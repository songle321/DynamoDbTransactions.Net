﻿using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.Model;
using com.amazonaws.services.dynamodbv2.transactions.exceptions;

using static com.amazonaws.services.dynamodbv2.transactions.Transaction;
using static com.amazonaws.services.dynamodbv2.transactions.exceptions.TransactionAssertionException;

// <summary>
// Copyright 2013-2016 Amazon.com, Inc. or its affiliates. All Rights Reserved.
// 
// Licensed under the Amazon Software License (the "License").
// You may not use this file except in compliance with the License.
// A copy of the License is located at
// 
//  http://aws.amazon.com/asl/
// 
// or in the "license" file accompanying this file. This file is distributed
// on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, express
// or implied. See the License for the specific language governing permissions
// and limitations under the License.
// </summary>
namespace com.amazonaws.services.dynamodbv2.transactions
{
    /// <summary>
    /// An isolation handler for reading items at the committed
    /// (Transaction.IsolationLevel.COMMITTED) level. It will
    /// filter out transient items. If there is an applied, but
    /// uncommitted item, the isolation handler will attempt to
    /// get the last committed version of the item.
    /// </summary>
    public class ReadCommittedIsolationHandlerImpl : IReadIsolationHandler
    {
        private const int DefaultNumRetries = 2;
        private static readonly Log Log = LogFactory.GetLog(typeof(ReadCommittedIsolationHandlerImpl));

        private readonly TransactionManager _txManager;
        private readonly int _numRetries;

        public ReadCommittedIsolationHandlerImpl(TransactionManager txManager) : this(txManager, DefaultNumRetries)
        {
        }

        public ReadCommittedIsolationHandlerImpl(TransactionManager txManager, int numRetries)
        {
            this._txManager = txManager;
            this._numRetries = numRetries;
        }

        /// <summary>
        /// Return the item that's passed in if it's not locked. Otherwise, throw a TransactionException. </summary>
        /// <param name="item"> The item to check </param>
        /// <returns> The item if it's locked (or if it's locked, but not yet applied) </returns>
        protected internal virtual Dictionary<string, AttributeValue> CheckItemCommitted(Dictionary<string, AttributeValue> item)
        {
            // If the item doesn't exist, it's not locked
            if (item == null)
            {
                return null;
            }
            // If the item is transient, return null
            if (IsTransient(item))
            {
                return null;
            }
            // If the item isn't applied, it doesn't matter if it's locked
            if (!IsApplied(item))
            {
                return item;
            }
            // If the item isn't locked, return it
            string lockingTxId = GetOwner(item);
            if (string.ReferenceEquals(lockingTxId, null))
            {
                return item;
            }

            throw new TransactionException(lockingTxId, "Item has been modified in an uncommitted transaction.");
        }

        /// <summary>
        /// Get an old committed version of an item from the images table. </summary>
        /// <param name="lockingTx"> The transaction that is currently locking the item. </param>
        /// <param name="tableName"> The table that contains the item </param>
        /// <param name="key"> The item's key </param>
        /// <returns> a previously committed version of the item </returns>
        protected internal virtual async Task<Dictionary<string, AttributeValue>> GetOldCommittedItemAsync(Transaction lockingTx, string tableName, Dictionary<string, AttributeValue> key)
        {
            Request lockingRequest = lockingTx.TxItem.GetRequestForKey(tableName, key);
            TxAssert(lockingRequest != null, null, "Expected transaction to be locking request, but no request found for tx", lockingTx.Id, "table", tableName, "key ", key);
            Dictionary<string, AttributeValue> oldItem = await lockingTx.TxItem.LoadItemImageAsync(lockingRequest.Rid);
            if (oldItem == null)
            {
                if (Log.DebugEnabled)
                {
                    Log.Debug("Item image " + lockingRequest.Rid + " missing for transaction " + lockingTx.Id);
                }
                throw new UnknownCompletedTransactionException(lockingTx.Id, "Transaction must have completed since the old copy of the image is missing");
            }
            return oldItem;
        }

        /// <summary>
        /// Create a GetItemRequest for an item (in the event that you need to get the item again). </summary>
        /// <param name="tableName"> The table that holds the item </param>
        /// <param name="item"> The item to get </param>
        /// <param name="cancellationToken"></param>
        /// <returns> the request </returns>
        protected internal virtual async Task<GetItemRequest> CreateGetItemRequestAsync(string tableName, Dictionary<string, AttributeValue> item, CancellationToken cancellationToken)
        {
            Dictionary<string, AttributeValue> key = await _txManager.CreateKeyMapAsync(tableName, item, cancellationToken);

            /*
			 * Set the request to consistent read the next time around, since we may have read while locking tx
			 * was cleaning up or read a stale item that is no longer locked
			 */
            GetItemRequest request = new GetItemRequest
            {
                TableName = tableName,
                Key = key,
                ConsistentRead = true
            };
            return request;
        }

        protected internal virtual Transaction LoadTransaction(string txId)
        {
            return new Transaction(txId, _txManager, false);
        }

        /// <summary>
        /// Returns the item that's passed in if it's not locked. Otherwise, tries to get an old
        /// committed version of the item. If that's not possible, it retries. </summary>
        /// <param name="item"> The item to check. </param>
        /// <param name="tableName"> The table that contains the item </param>
        /// <param name="numRetries"></param>
        /// <param name="cancellationToken"></param>
        /// <returns> A committed version of the item (not necessarily the latest committed version). </returns>
        protected internal virtual async Task<Dictionary<string, AttributeValue>> HandleItemAsync(Dictionary<string, AttributeValue> item, string tableName, int numRetries, CancellationToken cancellationToken)
        {
            GetItemRequest request = null; // only create if necessary
            for (int i = 0; i <= numRetries; i++)
            {
                Dictionary<string, AttributeValue> currentItem;
                if (i == 0)
                {
                    currentItem = item;
                }
                else
                {
                    if (request == null)
                    {
                        request = await CreateGetItemRequestAsync(tableName, item, cancellationToken);
                    }
                    currentItem = (await _txManager.Client.GetItemAsync(request, cancellationToken)).Item;
                }

                // 1. Return the item if it isn't locked (or if it's locked, but not applied yet)
                try
                {
                    return CheckItemCommitted(currentItem);
                }
                catch (TransactionException e1)
                {
                    try
                    {
                        // 2. Load the locking transaction
                        Transaction lockingTx = LoadTransaction(e1.TxId);

                        /*
						 * 3. See if the locking transaction has been committed. If so, return the item. This is valid because you cannot
						 * write to an item multiple times in the same transaction. Otherwise it would expose intermediate state.
						 */
                        if (TransactionItem.State.Committed.Equals(lockingTx.TxItem.GetState()))
                        {
                            return currentItem;
                        }

                        // 4. Try to get a previously committed version of the item
                        if (request == null)
                        {
                            request = await CreateGetItemRequestAsync(tableName, item, cancellationToken);
                        }
                        return await GetOldCommittedItemAsync(lockingTx, tableName, request.Key);
                    }
                    catch (UnknownCompletedTransactionException e2)
                    {
                        Log.Debug("Could not find item image. Transaction must have already completed.", e2);
                    }
                    catch (TransactionNotFoundException e2)
                    {
                        Log.Debug("Unable to find locking transaction. Transaction must have already completed.", e2);
                    }
                }
            }
            throw new TransactionException(null, "Ran out of attempts to get a committed image of the item");
        }

        protected internal virtual Dictionary<string, AttributeValue> FilterAttributesToGet(Dictionary<string, AttributeValue> item, List<string> attributesToGet)
        {
            if (item == null)
            {
                return null;
            }
            if (attributesToGet == null || attributesToGet.Count == 0)
            {
                return item;
            }
            Dictionary<string, AttributeValue> result = new Dictionary<string, AttributeValue>();
            foreach (string attributeName in attributesToGet)
            {
                AttributeValue value = item[attributeName];
                if (value != null)
                {
                    result[attributeName] = value;
                }
            }
            return result;
        }

        public virtual async Task<Dictionary<string, AttributeValue>> HandleItemAsync(Dictionary<string, AttributeValue> item, List<string> attributesToGet, string tableName, CancellationToken cancellationToken)
        {
            return FilterAttributesToGet(await HandleItemAsync(item, tableName, _numRetries, cancellationToken), attributesToGet);
        }

    }

}