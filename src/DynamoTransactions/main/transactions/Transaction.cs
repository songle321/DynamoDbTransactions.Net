using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using com.amazonaws.services.dynamodbv2.transactions.exceptions;
using static com.amazonaws.services.dynamodbv2.transactions.Transaction;
using static com.amazonaws.services.dynamodbv2.transactions.exceptions.TransactionAssertionException;
using System.Linq;

// <summary>
// Copyright 2013-2014 Amazon.com, Inc. or its affiliates. All Rights Reserved.
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
    /// A transaction that can span multiple items or tables in DynamoDB.  Thread-safe.  
    /// 
    /// If you are using transactions on items in a table, you should perform write operations only using this transaction library. 
    /// Failing to do so (performing raw writes to the could cause transactions to become stuck permanently, lost writes (either 
    /// from transactions or from your writes), or other undefined behavior. 
    /// 
    /// These transactions are atomic and can provide read isolation.
    /// <ul> 
    ///   <li>Atomicity: The transaction guarantees that if you successfully commitAsync your transaction, all of the requests in your transactions 
    ///       will eventually be applied without interference from other transactions.  If your application dies while committing, other 
    ///       transactions that attempt to lock any of the items in your transactions will finishAsync your transaction before making progress in their own.
    ///       It is recommended that you periodically scan the Transactions table for stuck transactions, or look for stale locks when you read items, 
    ///       to ensure that transactions are eventually either re-driven or rolled back.</li>
    ///   <li>Isolation: This library offers 3 forms of read isolation.  The strongest form involves acquiring read locks within the scope of a transaction.
    ///       Once you commitAsync the transaction, you know that those reads were performed in isolation.  While this is the strongest form, it is also the most
    ///       expensive.  For other forms of read isolation, see <seealso cref="TransactionManager"/>.</li>
    /// </ul>
    /// 
    /// Usage notes:
    /// <ul>
    ///   <li>When involving an item in a transaction that does not yet exist in the table, this library will insert the item without any other attributes 
    ///       except for the primary key and some transaction metadata attributes.  If you perform reads on your table outside of the transaction library,
    ///       you need to be prepared to deal with these "half written" items. These are identifiable by the presence of a "_TxT" attribute.  See 
    ///       getItemAsync in <seealso cref="TransactionManager"/> for read options that handle dealing with these "half written" items for you.</li>
    ///   <li>You can't perform multiple write operations on the same item within the transaction</li>
    ///   <li>If you read in a transaction after a write, the read will return the item as if the write has committed.</li>
    ///   <li>Read locks may be upgraded to write locks, and you can read items if the have write locks.</li>
    ///   <li>ReturnValues in write operations are supported.<li>
    ///   <li>You are recommended to periodically scan your transactions table for stuck transactions so that they are eventually redriven or rolled back</li>
    /// </ul>
    /// 
    /// Current caveats:
    /// <ul>
    ///   <li>The total amount of request data in a transaction may not exceed 64 KB.</li>
    ///   <li>Conditions in write operations are not supported.</li>
    ///   <li>This library cannot operate on items which are larger than 63 KB (TODO come up with exact value)</li>
    ///   <li>Attributes beginning with "_Tx" are not allowed in your items involved in transactions.</li> 
    /// </li> 
    /// </summary>
    public class Transaction
    {
        private static readonly Log Log = LogFactory.GetLog(typeof(Transaction));

        private const int ItemLockAcquireAttempts = 3;
        private const int ItemCommitAttempts = 2;
        private const int TxLockAcquireAttempts = 2;
        private const int TxLockContentionResolutionAttempts = 3;
        protected internal const string BooleanTrueAttrVal = "1";

        /* Attribute name constants */
        protected internal const string TxAttrPrefix = "_Tx";
        public static readonly ISet<string> SpecialAttrNames;

        private readonly TransactionManager _txManager;
        private TransactionItem _txItem;
        private readonly string _txId;
        private readonly SortedSet<int?> _fullyAppliedRequests = new SortedSet<int?>();

        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);

        static Transaction()
        {
            ISet<string> names = new HashSet<string>();
            foreach (AttributeName name in AttributeName.Values())
            {
                names.Add(name.ToString());
            }
            SpecialAttrNames = names;
        }

        /// <summary>
        /// default constructor to make cglib/spring able to proxy Transactions.
        /// </summary>
        protected internal Transaction()
        {
            this._txManager = null;
            this._txItem = null;
            this._txId = null;
        }

        /// <summary>
        /// Opens a new transaction inserts it into the database, or resumes an existing transaction.
        /// </summary>
        /// <param name="txId"> </param>
        /// <param name="txManager"> </param>
        /// <param name="insert"> - whether or not this is a new transaction, or one being resumed. </param>
        /// <exception cref="TransactionNotFoundException"> </exception>
        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: protected Transaction(String txId, TransactionManager txManager, boolean insert) throws com.amazonaws.services.dynamodbv2.transactions.exceptions.TransactionNotFoundException
        protected internal Transaction(string txId, TransactionManager txManager, bool insert)
        {
            this._txManager = txManager;
            this._txItem = new TransactionItem(txId, txManager, insert);
            this._txId = txId;
        }

        /// <summary>
        /// Resumes an existing transaction.  The caller must provide all of the attributes of the item.   
        /// </summary>
        /// <param name="txItem"> </param>
        /// <param name="txManager"> </param>
        /// <exception cref="TransactionNotFoundException"> </exception>
        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: protected Transaction(java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> txItem, TransactionManager txManager) throws com.amazonaws.services.dynamodbv2.transactions.exceptions.TransactionNotFoundException
        protected internal Transaction(Dictionary<string, AttributeValue> txItem, TransactionManager txManager)
        {
            this._txManager = txManager;
            this._txItem = new TransactionItem(txItem, txManager);
            this._txId = this._txItem.TxId;
        }

        public virtual string Id
        {
            get { return _txId; }
        }

        /// <summary>
        /// Adds a PutItem request to the transaction
        /// </summary>
        /// <param name="request"> </param>
        /// <exception cref="DuplicateRequestException"> if the item in the request is already involved in this transaction </exception>
        /// <exception cref="ItemNotLockedException"> when another transaction is confirmed to have the lock on the item in the request </exception>
        /// <exception cref="TransactionCompletedException"> when the transaction has already completed </exception>
        /// <exception cref="TransactionNotFoundException"> if the transaction does not exist </exception>
        /// <exception cref="TransactionException"> on unexpected errors or unresolvable OCC contention </exception>
        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public com.amazonaws.services.dynamodbv2.model.PutItemResponse putItemAsync(com.amazonaws.services.dynamodbv2.model.PutItemRequest request) throws com.amazonaws.services.dynamodbv2.transactions.exceptions.DuplicateRequestException, com.amazonaws.services.dynamodbv2.transactions.exceptions.ItemNotLockedException, com.amazonaws.services.dynamodbv2.transactions.exceptions.TransactionCompletedException, com.amazonaws.services.dynamodbv2.transactions.exceptions.TransactionNotFoundException, com.amazonaws.services.dynamodbv2.transactions.exceptions.TransactionException
        public virtual async Task<PutItemResponse> PutItemAsync(PutItemRequest request)
        {
            Request.PutItem wrappedRequest = new Request.PutItem();
            wrappedRequest.Request = request;
            Dictionary<string, AttributeValue> item = await DriveRequestAsync(wrappedRequest);
            StripSpecialAttributes(item);
            return new PutItemResponse
            {
                Attributes = item
            };
        }

        /// <summary>
        /// Adds an UpdateItem request to the transaction
        /// </summary>
        /// <param name="request"> </param>
        /// <exception cref="DuplicateRequestException"> if the item in the request is already involved in this transaction </exception>
        /// <exception cref="ItemNotLockedException"> when another transaction is confirmed to have the lock on the item in the request </exception>
        /// <exception cref="TransactionCompletedException"> when the transaction has already completed </exception>
        /// <exception cref="TransactionNotFoundException"> if the transaction does not exist </exception>
        /// <exception cref="TransactionException"> on unexpected errors or unresolvable OCC contention </exception>
        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public com.amazonaws.services.dynamodbv2.model.UpdateItemResponse updateItemAsync(com.amazonaws.services.dynamodbv2.model.UpdateItemRequest request) throws com.amazonaws.services.dynamodbv2.transactions.exceptions.DuplicateRequestException, com.amazonaws.services.dynamodbv2.transactions.exceptions.ItemNotLockedException, com.amazonaws.services.dynamodbv2.transactions.exceptions.TransactionCompletedException, com.amazonaws.services.dynamodbv2.transactions.exceptions.TransactionNotFoundException, com.amazonaws.services.dynamodbv2.transactions.exceptions.TransactionException
        public virtual async Task<UpdateItemResponse> UpdateItemAsync(UpdateItemRequest request)
        {
            Request.UpdateItem wrappedRequest = new Request.UpdateItem();
            wrappedRequest.Request = request;
            Dictionary<string, AttributeValue> item = await DriveRequestAsync(wrappedRequest);
            StripSpecialAttributes(item);
            return new UpdateItemResponse
            {
                Attributes = item
            };
        }

        /// <summary>
        /// Adds a DeleteItem request to the transaction
        /// </summary>
        /// <param name="request"> </param>
        /// <exception cref="DuplicateRequestException"> if the item in the request is already involved in this transaction </exception>
        /// <exception cref="ItemNotLockedException"> when another transaction is confirmed to have the lock on the item in the request </exception>
        /// <exception cref="TransactionCompletedException"> when the transaction has already completed </exception>
        /// <exception cref="TransactionNotFoundException"> if the transaction does not exist </exception>
        /// <exception cref="TransactionException"> on unexpected errors or unresolvable OCC contention </exception>
        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public com.amazonaws.services.dynamodbv2.model.DeleteItemResponse deleteItemAsync(com.amazonaws.services.dynamodbv2.model.DeleteItemRequest request) throws com.amazonaws.services.dynamodbv2.transactions.exceptions.DuplicateRequestException, com.amazonaws.services.dynamodbv2.transactions.exceptions.ItemNotLockedException, com.amazonaws.services.dynamodbv2.transactions.exceptions.TransactionCompletedException, com.amazonaws.services.dynamodbv2.transactions.exceptions.TransactionNotFoundException, com.amazonaws.services.dynamodbv2.transactions.exceptions.TransactionException
        public virtual async Task<DeleteItemResponse> DeleteItemAsync(DeleteItemRequest request)
        {
            Request.DeleteItem wrappedRequest = new Request.DeleteItem();
            wrappedRequest.Request = request;
            Dictionary<string, AttributeValue> item = await DriveRequestAsync(wrappedRequest);
            StripSpecialAttributes(item);
            return new DeleteItemResponse
            {
                Attributes = item
            };
        }

        /// <summary>
        /// Locks an item for the duration of the transaction, unless it is already locked. Useful for isolated reads.  
        /// Returns the copy of the item as it exists so far in the transaction (if reading after a write in the same transaction)
        /// </summary>
        /// <param name="request"> </param>
        /// <exception cref="DuplicateRequestException"> if the item in the request is already involved in this transaction </exception>
        /// <exception cref="ItemNotLockedException"> when another transaction is confirmed to have the lock on the item in the request </exception>
        /// <exception cref="TransactionCompletedException"> when the transaction has already completed </exception>
        /// <exception cref="TransactionNotFoundException"> if the transaction does not exist </exception>
        /// <exception cref="TransactionException"> on unexpected errors or unresolvable OCC contention </exception>
        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public com.amazonaws.services.dynamodbv2.model.GetItemResponse getItemAsync(com.amazonaws.services.dynamodbv2.model.GetItemRequest request) throws com.amazonaws.services.dynamodbv2.transactions.exceptions.DuplicateRequestException, com.amazonaws.services.dynamodbv2.transactions.exceptions.ItemNotLockedException, com.amazonaws.services.dynamodbv2.transactions.exceptions.TransactionCompletedException, com.amazonaws.services.dynamodbv2.transactions.exceptions.TransactionNotFoundException, com.amazonaws.services.dynamodbv2.transactions.exceptions.TransactionException
        public virtual async Task<GetItemResponse> GetItemAsync(GetItemRequest request)
        {
            Request.GetItem wrappedRequest = new Request.GetItem();
            wrappedRequest.Request = request;
            Dictionary<string, AttributeValue> item = await DriveRequestAsync(wrappedRequest);
            StripSpecialAttributes(item);
            GetItemResponse result = new GetItemResponse
            {
                Item = item
            };
            return result;
        }

        public static void StripSpecialAttributes(Dictionary<string, AttributeValue> item)
        {
            if (item == null)
            {
                return;
            }
            foreach (string specialAttribute in SpecialAttrNames)
            {
                item.Remove(specialAttribute);
            }
        }

        public static bool IsLocked(Dictionary<string, AttributeValue> item)
        {
            if (item == null)
            {
                return false;
            }
            if (item.ContainsKey(AttributeName.Txid.ToString()))
            {
                return true;
            }
            return false;
        }

        public static bool IsApplied(Dictionary<string, AttributeValue> item)
        {
            if (item == null)
            {
                return false;
            }
            if (item.ContainsKey(AttributeName.Applied.ToString()))
            {
                return true;
            }
            return false;
        }

        public static bool IsTransient(Dictionary<string, AttributeValue> item)
        {
            if (item == null)
            {
                return false;
            }
            if (item.ContainsKey(AttributeName.Transient.ToString()))
            {
                return true;
            }
            return false;
        }

        public enum IsolationLevel
        {
            Uncommitted,
            Committed,
            ReadLock // what does it mean to read an item you wrote to in a transaction?
        }

        /// <summary>
        /// Deletes the transaction.  
        /// </summary>
        /// <returns> true if the transaction was deleted, false if it was not </returns>
        /// <exception cref="TransactionException"> if the transaction is not yet completed. </exception>
        public virtual async Task<bool> DeleteAsync()
        {
            return await DeleteIfAfterAsync(null);
        }

        /// <summary>
        /// Deletes the transaction, only if it has not been update since the specified duration.  A transaction's 
        /// "last updated date" is updated when:
        ///  - A request is added to the transaction
        ///  - The transaction switches to COMMITTED or ROLLED_BACK
        ///  - The transaction is marked as completed.  
        /// </summary>
        /// <param name="deleteIfAfterMillis"> the duration to ensure has passed before attempting to delete the record </param>
        /// <returns> true if the transaction was deleted, false if it was not old enough to delete yet. </returns>
        /// <exception cref="TransactionException"> if the transaction is not yet completed. </exception>
        public virtual async Task<bool> DeleteAsync(long deleteIfAfterMillis)
        {
            return await DeleteIfAfterAsync(deleteIfAfterMillis);
        }

        private async Task<bool> DeleteIfAfterAsync(long? deleteIfAfterMillis)
        {
            await _semaphore.WaitAsync();
            try
            {
                if (!_txItem.Completed)
                {
                    // Ensure we have an up to date tx record
                    try
                    {
                        _txItem = new TransactionItem(_txId, _txManager, false);
                    }
                    catch (TransactionNotFoundException)
                    {
                        return true; // expected, transaction already deleted
                    }
                    if (!_txItem.Completed)
                    {
                        throw new TransactionException(_txId, "You can only delete a transaction that is completed");
                    }
                }
                try
                {
                    if (deleteIfAfterMillis == null ||
                        (_txItem.LastUpdateTimeMillis + deleteIfAfterMillis) <
                        DateTimeHelperClass.CurrentUnixTimeMillis())
                    {
                        _txItem.Delete();
                        return true;
                    }
                }
                catch (ConditionalCheckFailedException)
                {
                    // Can only happen if the tx isn't finalized or is already gone.
                    try
                    {
                        _txItem = new TransactionItem(_txId, _txManager, false);
                        throw new TransactionException(_txId,
                            "Transaction was completed but could not be deleted. " + _txItem);
                    }
                    catch (TransactionNotFoundException)
                    {
                        return true; // expected, transaction already deleted
                    }
                }
                return false;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Finishes a a transaction if it is already COMMITTED or PENDING but not yet COMPLETED
        /// 
        /// If it is PENDING and hasn't been active for rollbackAfterDurationMills, the transaction is rolled back.
        /// 
        /// If it is completed and hasn't been active for deleteAfterDurationMillis, the transaction is deleted.
        /// </summary>
        /// <param name="rollbackAfterDurationMills"> </param>
        /// <param name="deleteAfterDurationMillis"> </param>
        public virtual async Task SweepAsync(long rollbackAfterDurationMills, long deleteAfterDurationMillis)
        {
            // If the item has been completed for the specified threshold, delete it.
            if (_txItem.Completed)
            {
                await DeleteAsync(deleteAfterDurationMillis);
            }
            else
            {
                // If the transaction has been PENDING for too long, roll it back.
                // If it's COMMITTED or PENDING, drive it to completion. 
                switch (_txItem.GetState())
                {
                    case TransactionItem.State.Pending:
                        if ((_txItem.LastUpdateTimeMillis + rollbackAfterDurationMills) <
                            DateTimeHelperClass.CurrentUnixTimeMillis())
                        {
                            try
                            {
                                await RollbackAsync();
                            }
                            catch (TransactionCompletedException)
                            {
                                // Transaction is already completed, ignore
                            }
                        }
                        break;
                    case TransactionItem.State.Committed: // NOTE: falling through to ROLLED_BACK
                    case TransactionItem.State.RolledBack:
                        // This could callAsync either commitAsync or rollbackAsync - they'll both do the right thing if it's already committed
                        try
                        {
                            await RollbackAsync();
                        }
                        catch (TransactionCompletedException)
                        {
                            // Transaction is already completed, ignore
                        }
                        break;
                    default:
                        throw new TransactionAssertionException(_txId,
                            "Unexpected state in transaction: " + _txItem.GetState());
                }
            }
        }

        /// <summary>
        /// Adds a request to the transaction
        /// </summary>
        /// <param name="clientRequest"> </param>
        /// <exception cref="DuplicateRequestException"> if the item in the request is already used in this transaction </exception>
        /// <exception cref="ItemNotLockedException"> if we were unable to acquire the lock because of contention with other transactions </exception>
        /// <exception cref="TransactionException"> if another unresolvable error occurs, including too much contention on this transaction record </exception>
        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: protected synchronized java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> driveRequestAsync(Request clientRequest) throws com.amazonaws.services.dynamodbv2.transactions.exceptions.DuplicateRequestException, com.amazonaws.services.dynamodbv2.transactions.exceptions.ItemNotLockedException, com.amazonaws.services.dynamodbv2.transactions.exceptions.TransactionException
        protected internal virtual async Task<Dictionary<string, AttributeValue>> DriveRequestAsync(Request clientRequest)
        {
            await _semaphore.WaitAsync();
            try
            {
                /* 1. Validate the request (no conditions, required attributes, no conflicting attributes etc)
				 * 2. Copy the request (we change things in it, so don't mutate the caller's request
				 * 3. Acquire the lock, saveAsync image, apply via addRequestAsync()
				 *    a) If that fails, go to 3) again.
				 */

                // basic validation
                await clientRequest.ValidateAsync(_txId, _txManager);

                // Don't mutate the caller's request.
                Request requestCopy = Request.Deserialize(_txId, Request.Serialize(_txId, clientRequest));

                ItemNotLockedException lastConflict = null;
                for (int i = 0; i < TxLockContentionResolutionAttempts; i++)
                {
                    try
                    {
                        Dictionary<string, AttributeValue> item =
                            await AddRequestAsync(requestCopy, (i != 0), TxLockAcquireAttempts);
                        return item;
                    }
                    catch (ItemNotLockedException e)
                    {
                        // Roll back or completeAsync the other transaction
                        lastConflict = e;
                        Transaction conflictingTransaction = null;
                        try
                        {
                            conflictingTransaction = new Transaction(e.LockOwnerTxId, _txManager, false);
                            await conflictingTransaction.RollbackAsync();
                        }
                        catch (TransactionNotFoundException)
                        {
                            // ignore, and try again on the next iteration, previous lock should be gone now
                        }
                        catch (TransactionCompletedException)
                        {
                            // ignore, doesn't matter if it committed or rolled back
                        }
                    }
                }
                throw lastConflict;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Commits the transaction
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <exception cref="TransactionRolledBackException"> - the transaction was rolled back by a concurrent overlapping transaction </exception>
        /// <exception cref="UnknownCompletedTransactionException"> - the transaction completed, but it is not known whether it committed or rolled back
        /// TODO throw a specific exception for encountering too much contention  </exception>
        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public synchronized void commitAsync() throws com.amazonaws.services.dynamodbv2.transactions.exceptions.TransactionRolledBackException, com.amazonaws.services.dynamodbv2.transactions.exceptions.UnknownCompletedTransactionException
        public virtual async Task CommitAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                // 1. Re-read transaction item
                //    a) If it doesn't exist, throw UnknownCompletedTransaction 

                // 2. Verify that we should continue
                //    a) If the transaction is closed, return or throw depending on COMPLETE or ROLLED_BACK.
                //    b) If the transaction is ROLLED_BACK, continue doRollbackAsync(), but at the end throw.
                //    c) If the transaction is COMMITTED, go to doCommitAsync(), and at the end return.

                // 3. Save the transaction's version number to detect if additional requests are added

                // 4. Verify that we have all of the locks and their saved images

                // 5. Change the state to COMMITTED conditioning on: 
                //         - it isn't closed
                //         - the version number hasn't changed
                //         - it's still PENDING
                //      a) If that fails, go to 1).

                // 6. Return success

                for (int i = 0; i < ItemCommitAttempts + 1; i++)
                {
                    // Re-read state to ensure this isn't a resume that's going to come along and re-apply a completed transaction.
                    // This doesn't prevent a transaction from being applied multiple times, but it prevents a sweeper from applying
                    // a very old transaction.
                    try
                    {
                        _txItem = new TransactionItem(_txId, _txManager, false);
                    }
                    catch (TransactionNotFoundException)
                    {
                        throw new UnknownCompletedTransactionException(_txId,
                            "In transaction " + TransactionItem.State.Committed +
                            " attempt, transaction either rolled back or committed");
                    }

                    if (_txItem.Completed)
                    {
                        if (TransactionItem.State.Committed.Equals(_txItem.GetState()))
                        {
                            return;
                        }
                        else if (TransactionItem.State.RolledBack.Equals(_txItem.GetState()))
                        {
                            throw new TransactionRolledBackException(_txId, "Transaction was rolled back");
                        }
                        else
                        {
                            throw new TransactionAssertionException(_txId,
                                "Unexpected state for transaction: " + _txItem.GetState());
                        }
                    }

                    if (TransactionItem.State.Committed.Equals(_txItem.GetState()))
                    {
                        await DoCommitAsync();
                        return;
                    }

                    if (TransactionItem.State.RolledBack.Equals(_txItem.GetState()))
                    {
                        await DoRollbackAsync();
                        throw new TransactionRolledBackException(_txId, "Transaction was rolled back");
                    }

                    // Commit attempts is actually for the number of times we try to acquire all the locks
                    if (!(i < ItemCommitAttempts))
                    {
                        throw new TransactionException(_txId,
                            "Unable to commitAsync transaction after " + ItemCommitAttempts + " attempts");
                    }

                    int version = _txItem.Version;

                    await VerifyLocksAsync();

                    try
                    {
                        await _txItem.FinishAsync(TransactionItem.State.Committed, version);
                    }
                    catch (ConditionalCheckFailedException)
                    {
                        // Tx item version, changed out from under us, or was moved to committed, rolled back, deleted, etc by someone else.
                        // Retry in loop
                    }
                }

                throw new TransactionException(_txId,
                    "Unable to commitAsync transaction after " + ItemCommitAttempts + " attempts");
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Rolls back the transaction.  You can only roll back a transaction that is in the PENDING state (not yet committed).
        /// <li>If you roll back a transaction in COMMITTED, this will continue committing the transaction if it isn't completed yet, 
        ///      but you will get back a TransactionCommittedException. </li>
        /// <li>If you roll back and already rolled back transaction, this will ensure the rollbackAsync completed, and return success</li>
        /// <li>If the transaction no longer exists, you'll get back an UnknownCompletedTransactionException</li>   
        /// </summary>
        /// <exception cref="TransactionCommittedException"> - the transaction was committed by a concurrent overlapping transaction </exception>
        /// <exception cref="UnknownCompletedTransactionException"> - the transaction completed, but it is not known whether it was rolled back or committed </exception>
        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public synchronized void rollbackAsync() throws com.amazonaws.services.dynamodbv2.transactions.exceptions.TransactionCompletedException, com.amazonaws.services.dynamodbv2.transactions.exceptions.UnknownCompletedTransactionException
        public virtual async Task RollbackAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                TransactionItem.State state;
                bool alreadyRereadTxItem = false;
                try
                {
                    await _txItem.FinishAsync(TransactionItem.State.RolledBack, _txItem.Version);
                    state = TransactionItem.State.RolledBack;
                }
                catch (ConditionalCheckFailedException)
                {
                    try
                    {
                        // Re-read state to see its actual state, since it wasn't in PENDING
                        _txItem = new TransactionItem(_txId, _txManager, false);
                        alreadyRereadTxItem = true;
                        state = _txItem.GetState();
                    }
                    catch (TransactionNotFoundException)
                    {
                        throw new UnknownCompletedTransactionException(_txId,
                            "In transaction " + TransactionItem.State.RolledBack +
                            " attempt, transaction either rolled back or committed");
                    }
                }

                if (TransactionItem.State.Committed.Equals(state))
                {
                    if (!_txItem.Completed)
                    {
                        await DoCommitAsync();
                    }
                    throw new TransactionCommittedException(_txId, "Transaction was committed");
                }
                else if (TransactionItem.State.RolledBack.Equals(state))
                {
                    if (!_txItem.Completed)
                    {
                        await DoRollbackAsync();
                    }
                    return;
                }
                else if (TransactionItem.State.Pending.Equals(state))
                {
                    if (!alreadyRereadTxItem)
                    {
                        // The item was modified in the meantime (another request was added to it)
                        // so make sure we re-read it, and then try the rollbackAsync again
                        _txItem = new TransactionItem(_txId, _txManager, false);
                    }
                    await RollbackAsync();
                    return;
                }
                throw new TransactionAssertionException(_txId, "Unexpected state in rollbackAsync(): " + state);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Verifies that we actually hold all of the locks for the requests in the transaction, and that we have saved the 
        /// previous item images of every item involved in the requests (except for request types that we don't saveAsync images for).
        /// 
        /// The caller needs to wrap this with OCC on the tx version (request count) if it's going to commitAsync based on this decision.
        /// 
        /// This is optimized to consider the "version" numbers of the items that this Transaction object has fully applied so far
        /// to optimize the normal case that doesn't have failures.
        /// </summary>
        protected internal virtual async Task VerifyLocksAsync()
        {
            foreach (Request request in _txItem.Requests)
            {
                // Optimization: If our transaction object (this) has first-hand fully applied a request, no need to do it again.
                if (!_fullyAppliedRequests.Contains(request.Rid))
                {
                    await AddRequestAsync(request, true, ItemLockAcquireAttempts);
                }
            }
        }

        /// <summary>
        /// Deletes the transaction item from the database.
        /// 
        /// Does not throw if the item is gone, even if the conditional check to delete the item fails, and this method doesn't know what state
        /// it was in when deleted.  The caller is responsible for guaranteeing that it was actually in "currentState" immediately before calling
        /// this method.  
        /// </summary>
        protected internal virtual async Task CompleteAsync(TransactionItem.State expectedCurrentState)
        {
            try
            {
                await _txItem.CompleteAsync(expectedCurrentState);
            }
            catch (ConditionalCheckFailedException)
            {
                // Re-read state to ensure it was already completed
                try
                {
                    _txItem = new TransactionItem(_txId, _txManager, false);
                    if (!_txItem.Completed)
                    {
                        throw new TransactionAssertionException(_txId, "Expected the transaction to be completed (no item), but there was one.");
                    }
                }
                catch (TransactionNotFoundException)
                {
                    // expected - transaction record no longer exists
                }
            }
        }

        /// <summary>
        /// Deletes the old item images and unlocks each item, deleting the item themselves if they inserted only to lock the item.  
        /// 
        /// This is to be used post-commitAsync only.
        /// </summary>
        protected internal virtual async Task DoCommitAsync()
        {
            // Defensively re-check the state to ensure it is COMMITTED
            TxAssert(_txItem != null && TransactionItem.State.Committed.Equals(_txItem.GetState()), _txId, "doCommitAsync() requires a non-null txItem with a state of " + TransactionItem.State.Committed, "state", _txItem.GetState(), "txItem", _txItem);

            // Note: Order is functionally unimportant, but we unlock all items first to try to reduce the need 
            // for other readers to read this transaction's information since it has already committed.
            foreach (Request request in _txItem.Requests)
            {
                //Unlock the item, deleting it if it was inserted only to lock the item, or if it was a delete request
                await UnlockItemAfterCommitAsync(request);
            }

            // Clean up the old item images
            foreach (Request request in _txItem.Requests)
            {
                _txItem.DeleteItemImage(request.Rid);
            }

            await CompleteAsync(TransactionItem.State.Committed);
        }

        /// <summary>
        /// Releases the lock for the item.  If the item was inserted only to acquire the lock (if the item didn't exist before 
        /// for a DeleteItem or LockItem), it will be deleted now.
        /// 
        /// Otherwise, all of the attributes uses for the transaction (tx id, transient flag, applied flag) will be removed.
        /// 
        /// Conditions on our transaction id owning the item
        /// 
        /// To be used once the transaction has committed only. </summary>
        /// <param name="request"> </param>
        protected internal virtual async Task UnlockItemAfterCommitAsync(Request request)
        {
            try
            {
                Dictionary<string, ExpectedAttributeValue> expected = new Dictionary<string, ExpectedAttributeValue>();
                expected[AttributeName.Txid.ToString()] = new ExpectedAttributeValue
                {
                    Value = new AttributeValue(_txId)
                };

                if (request is Request.PutItem || request is Request.UpdateItem)
                {
                    Dictionary<string, AttributeValueUpdate> updates = new Dictionary<string, AttributeValueUpdate>();
                    updates[AttributeName.Txid.ToString()] = new AttributeValueUpdate
                    {
                        Action = AttributeAction.DELETE
                    };
                    updates[AttributeName.Transient.ToString()] = new AttributeValueUpdate
                    {
                        Action = AttributeAction.DELETE
                    };
                    updates[AttributeName.Applied.ToString()] = new AttributeValueUpdate
                    {
                        Action = AttributeAction.DELETE
                    };
                    updates[AttributeName.Date.ToString()] = new AttributeValueUpdate
                    {
                        Action = AttributeAction.DELETE
                    };

                    UpdateItemRequest update = new UpdateItemRequest
                    {
                        TableName = request.TableName,
                        Key = await request.GetKeyAsync(_txManager),
                        AttributeUpdates = updates,
                        Expected = expected
                    };
                    await _txManager.Client.UpdateItemAsync(update);
                }
                else if (request is Request.DeleteItem)
                {
                    DeleteItemRequest delete = new DeleteItemRequest
                    {
                        TableName = request.TableName,
                        Key = await request.GetKeyAsync(_txManager),
                        Expected = expected
                    };
                    await _txManager.Client.DeleteItemAsync(delete);
                }
                else if (request is Request.GetItem)
                {
                    await ReleaseReadLockAsync(request.TableName, await request.GetKeyAsync(_txManager));
                }
                else
                {
                    throw new TransactionAssertionException(_txId, "Unknown request type: " + request.GetType());
                }
            }
            catch (ConditionalCheckFailedException)
            {
                // ignore, unlock already happened
                // TODO if we really want to be paranoid we could condition on applied = 1, and then here
                //      we would have to read the item again and make sure that applied was 1 if we owned the lock (and assert otherwise) 
            }
        }



        /// <summary>
        /// Rolls back the transaction, only if the transaction is in the ROLLED_BACK state.
        /// 
        /// This handles using the AttributeName.TRANSIENT to ensure that if an item was "phantom" (inserted during the transaction when acquiring the lock),
        /// it gets deleted on rollbackAsync.
        /// </summary>
        protected internal virtual async Task DoRollbackAsync()
        {
            TxAssert(TransactionItem.State.RolledBack.Equals(_txItem.GetState()), _txId, "Transaction state is not " + TransactionItem.State.RolledBack, "state", _txItem.GetState(), "txItem", _txItem);

            foreach (Request request in _txItem.Requests)
            {
                // Unlike unlockItems(), the order is important here.

                // 1. Apply the old item image over the one the request modified 
                await RollbackItemAndReleaseLockAsync(request);

                // 2. Delete the old item image, we don't need it anymore
                _txItem.DeleteItemImage(request.Rid);
            }

            await CompleteAsync(TransactionItem.State.RolledBack);
        }

        /// <summary>
        /// Rolls back the apply of the request by reading the previous item image and overwriting the item with the old image.
        /// If there was no old item image, determines whether the item was transient (and there shouldn't be an item image), 
        /// or if   
        /// 
        /// In the case of lock requests, the lock is simply removed.
        /// 
        /// In either case, if the item did not exist before the lock was acquired, it is deleted.
        /// </summary>
        /// <param name="request"> </param>
        protected internal virtual async Task RollbackItemAndReleaseLockAsync(Request request)
        {
            await RollbackItemAndReleaseLockAsync(request.TableName, await request.GetKeyAsync(_txManager), request is Request.GetItem, request.Rid);
        }

        protected internal virtual async Task RollbackItemAndReleaseLockAsync(string tableName, Dictionary<string, AttributeValue> key, bool? isGet, int? rid)
        {
            // TODO there seems to be a race that leads to orphaned old item images (but is still correct in terms of the transaction)
            // A previous master could have stalled after writing the tx record, fall asleep, and then finally insert the old item image 
            // after this delete attempt goes through, and then the sleepy master crashes. There's no great way around this, 
            // so a sweeper needs to deal with it.

            // Possible outcomes:
            // 1) We know for sure from just the request (getItemAsync) that we never back up the item. Release the lock (and delete if transient)
            // 2) We found a backup.  Apply the backup.
            // 3) We didn't find a backup. Try deleting the item with expected: 1) Transient, 2) Locked by us, return success
            // 4) Read the item. If we don't have the lock anymore, meaning it was already rolled back.  Return.
            // 5) We failed to take the backup, but should have.  
            //   a) If we've applied, assert.  
            //   b) Otherwise release the lock (okay to delete if transient to re-use logic)

            // 1. Read locks don't have a saved item image, so just unlock them and return
            if (isGet.HasValue && isGet.Value)
            {
                await ReleaseReadLockAsync(tableName, key);
                return;
            }

            // Read the old item image, if the rid is known.  Otherwise we treat it as if we don't have an item image.
            Dictionary<string, AttributeValue> itemImage = null;
            if (rid != null)
            {
                itemImage = await _txItem.LoadItemImageAsync(rid.Value);
            }

            if (itemImage != null)
            {
                // 2. Found a backup.  Replace the current item with the pre-changes version of the item, at the same time removing the lock attributes
                TxAssert(itemImage.Remove(AttributeName.Transient.ToString()) == false, _txId, "Didn't expect to have saved an item image for a transient item", "itemImage", itemImage);

                itemImage.Remove(AttributeName.Txid.ToString());
                itemImage.Remove(AttributeName.Date.ToString());

                TxAssert(!itemImage.ContainsKey(AttributeName.Applied.ToString()), _txId, "Old item image should not have contained the attribute " + AttributeName.Applied.ToString(), "itemImage", itemImage);

                try
                {
                    Dictionary<string, ExpectedAttributeValue> expected = new Dictionary<string, ExpectedAttributeValue>();
                    expected[AttributeName.Txid.ToString()] = new ExpectedAttributeValue
                    {
                        Value = new AttributeValue(_txId)
                    };

                    PutItemRequest put = new PutItemRequest
                    {
                        TableName = tableName,
                        Item = itemImage,
                        Expected = expected
                    };
                    await _txManager.Client.PutItemAsync(put);
                }
                catch (ConditionalCheckFailedException)
                {
                    // Only conditioning on "locked by us", so if that fails, it means it already happened (and may have advanced forward)
                }
            }
            else
            {
                // 3) We didn't find a backup. Try deleting the item with expected: 1) Transient, 2) Locked by us, return success
                try
                {
                    Dictionary<string, ExpectedAttributeValue> expected = new Dictionary<string, ExpectedAttributeValue>();
                    expected[AttributeName.Txid.ToString()] = new ExpectedAttributeValue
                    {
                        Value = new AttributeValue(_txId)
                    };
                    expected[AttributeName.Transient.ToString()] = new ExpectedAttributeValue
                    {
                        Value = new AttributeValue(BooleanTrueAttrVal)
                    };

                    DeleteItemRequest delete = new DeleteItemRequest
                    {
                        TableName = tableName,
                        Key = key,
                        Expected = expected
                    };
                    await _txManager.Client.DeleteItemAsync(delete);
                    return;
                }
                catch (ConditionalCheckFailedException)
                {
                    // This means it already happened (and may have advanced forward)
                    // Technically there could be a bug if it is locked by us but not marked as transient.
                }
                // 4) Read the item. If we don't have the lock anymore, meaning it was already rolled back.  Return.
                // 5) We failed to take the backup, but should have.  
                //   a) If we've applied, assert.  
                //   b) Otherwise release the lock (okay to delete if transient to re-use logic)

                // 4) Read the item. If we don't have the lock anymore, meaning it was already rolled back.  Return.
                Dictionary<string, AttributeValue> item = await GetItemAsync(tableName, key);

                if (item == null || !_txId.Equals(GetOwner(item)))
                {
                    // 3a) We don't have the lock anymore.  Return.
                    return;
                }

                // 5) We failed to take the backup, but should have.  
                //   a) If we've applied, assert.
                TxAssert(!item.ContainsKey(AttributeName.Applied.ToString()), _txId, "Applied change to item but didn't saveAsync a backup", "table", tableName, "key", key, "item" + item);

                //   b) Otherwise release the lock (okay to delete if transient to re-use logic)
                await ReleaseReadLockAsync(tableName, key);
            }
        }

        /// <summary>
        /// Unlocks an item without applying the previous item image on top of it.  This will delete the item if it 
        /// was marked as phantom.  
        /// 
        /// This is ONLY valid for releasing a read lock (either during rollbackAsync or post-commitAsync) 
        ///  OR releasing a lock where the change wasn't applied yet.
        /// </summary>
        /// <param name="tableName"> </param>
        /// <param name="key"> </param>
        protected internal virtual async Task ReleaseReadLockAsync(string tableName, Dictionary<string, AttributeValue> key)
        {
            await ReleaseReadLockAsync(_txId, _txManager, tableName, key);
        }

        protected internal static async Task ReleaseReadLockAsync(string txId, TransactionManager txManager, string tableName, Dictionary<string, AttributeValue> key)
        {
            Dictionary<string, ExpectedAttributeValue> expected = new Dictionary<string, ExpectedAttributeValue>();
            expected[AttributeName.Txid.ToString()] = new ExpectedAttributeValue
            {
                Value = new AttributeValue(txId)
            };
            expected[AttributeName.Transient.ToString()] = new ExpectedAttributeValue
            {
                Exists = false
            };
            expected[AttributeName.Applied.ToString()] = new ExpectedAttributeValue
            {
                Exists = false
            };

            try
            {
                Dictionary<string, AttributeValueUpdate> updates = new Dictionary<string, AttributeValueUpdate>(1);
                updates[AttributeName.Txid.ToString()] = new AttributeValueUpdate
                {
                    Action = AttributeAction.DELETE
                };
                updates[AttributeName.Date.ToString()] = new AttributeValueUpdate
                {
                    Action = AttributeAction.DELETE
                };

                UpdateItemRequest update = new UpdateItemRequest
                {
                    TableName = tableName,
                    AttributeUpdates = updates,
                    Key = key,
                    Expected = expected
                };
                await txManager.Client.UpdateItemAsync(update);
            }
            catch (ConditionalCheckFailedException)
            {
                try
                {
                    expected[AttributeName.Transient.ToString()] = new ExpectedAttributeValue
                    {
                        Value = new AttributeValue {S = BooleanTrueAttrVal}
                    };

                    DeleteItemRequest delete = new DeleteItemRequest
                    {
                        TableName = tableName,
                        Key = key,
                        Expected = expected
                    };
                    await txManager.Client.DeleteItemAsync(delete);
                }
                catch (ConditionalCheckFailedException)
                {
                    // Ignore, means it was definitely rolled back
                    // Re-read to ensure that it wasn't applied
                    Dictionary<string, AttributeValue> item = await GetItemAsync(txManager, tableName, key);
                    TxAssert(!(item != null && txId.Equals(GetOwner(item)) && item.ContainsKey(AttributeName.Applied.ToString())), "Item should not have been applied.  Unable to release lock", "item", item);
                }
            }
        }

        /// <summary>
        /// Unlocks an item and leaves it in an unknown state, as long as there is no associated transaction record
        /// </summary>
        /// <param name="txManager"> </param>
        /// <param name="tableName"> </param>
        /// <param name="item"> </param>
        protected internal static async Task UnlockItemUnsafeAsync(TransactionManager txManager, string tableName, Dictionary<string, AttributeValue> item, string txId)
        {
            // 1) Ensure the transaction does not exist 
            try
            {
                Transaction tx = new Transaction(txId, txManager, false);
                throw new TransactionException(txId, "The transaction item should not have existed, but it did.  You can only unsafely unlock an item without a tx record. txItem: " + tx._txItem);
            }
            catch (TransactionNotFoundException)
            {
                // Expected to not exist
            }


            // 2) Remove all transaction attributes and condition on txId equality
            Dictionary<string, ExpectedAttributeValue> expected = new Dictionary<string, ExpectedAttributeValue>();
            expected[AttributeName.Txid.ToString()] = new ExpectedAttributeValue
            {
                Value = new AttributeValue(txId)
            };

            Dictionary<string, AttributeValueUpdate> updates = new Dictionary<string, AttributeValueUpdate>(1);
            foreach (string attrName in SpecialAttrNames)
            {
                updates[attrName] = new AttributeValueUpdate
                {
                    Action = AttributeAction.DELETE
                };
            }

            Dictionary<string, AttributeValue> key = await Request.GetKeyFromItemAsync(tableName, item, txManager);

            UpdateItemRequest update = new UpdateItemRequest
            {
                TableName = tableName,
                AttributeUpdates = updates,
                Key = key,
                Expected = expected
            };

            // Delete the item, and ignore conditional write failures
            try
            {
                await txManager.Client.UpdateItemAsync(update);
            }
            catch (ConditionalCheckFailedException)
            {
                // already unlocked
            }
        }

        /// <summary>
        /// Adds a request to the transaction, preserving order of requests via the version field in the tx record  
        /// </summary>
        /// <param name="callerRequest"> </param>
        /// <param name="isRedrive"> - true if the request was already saved to the tx item, and this is redriving the attempt to write the tx to the item (fighting for a lock with other transactions) </param>
        /// <param name="numAttempts"> </param>
        /// <exception cref="DuplicateRequestException"> if the item in the request is already involved in this transaction </exception>
        /// <exception cref="ItemNotLockedException"> when another transaction is confirmed to have the lock on the item in the request </exception>
        /// <exception cref="TransactionCompletedException"> when the transaction has already completed </exception>
        /// <exception cref="TransactionNotFoundException"> if the transaction does not exist </exception>
        /// <exception cref="TransactionException"> on unexpected errors or unresolvable OCC contention </exception>
        /// <returns> the applied item image, or null if the apply was a delete. </returns>
        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: protected java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> addRequestAsync(Request callerRequest, boolean isRedrive, int numAttempts) throws com.amazonaws.services.dynamodbv2.transactions.exceptions.DuplicateRequestException, com.amazonaws.services.dynamodbv2.transactions.exceptions.ItemNotLockedException, com.amazonaws.services.dynamodbv2.transactions.exceptions.TransactionCompletedException, com.amazonaws.services.dynamodbv2.transactions.exceptions.TransactionNotFoundException, com.amazonaws.services.dynamodbv2.transactions.exceptions.TransactionException
        protected internal virtual async Task<Dictionary<string, AttributeValue>> AddRequestAsync(Request callerRequest, bool isRedrive, int numAttempts)
        {
            // 1. Write the full caller request to the transaction item, but not if it's being re-driven.
            //    (In order to re-drive, the request must already be in the transaction item) 
            if (!isRedrive)
            {
                bool success = false;
                for (int i = 0; i < numAttempts; i++)
                {
                    // 1a. Verify the locks up to ensure that if we are adding a "read" request for an item that has been written to in this transaction,
                    //     that we return the write.
                    await VerifyLocksAsync();
                    try
                    {
                        await _txItem.AddRequestAsync(callerRequest);
                        success = true;
                        break;
                    }
                    catch (ConditionalCheckFailedException)
                    {
                        // The transaction is either not in PENDING anymore, or the version number incremented from another thread/process
                        // registering a transaction (or we started cold on an existing transaction).

                        _txItem = new TransactionItem(_txId, _txManager, false);

                        if (TransactionItem.State.Committed.Equals(_txItem.GetState()))
                        {
                            throw new TransactionCommittedException(_txId, "Attempted to add a request to a transaction that was not in state " + TransactionItem.State.Pending + ", state is " + _txItem.GetState());
                        }
                        else if (TransactionItem.State.RolledBack.Equals(_txItem.GetState()))
                        {
                            throw new TransactionRolledBackException(_txId, "Attempted to add a request to a transaction that was not in state " + TransactionItem.State.Pending + ", state is " + _txItem.GetState());
                        }
                        else if (!TransactionItem.State.Pending.Equals(_txItem.GetState()))
                        {
                            throw new UnknownCompletedTransactionException(_txId, "Attempted to add a request to a transaction that was not in state " + TransactionItem.State.Pending + ", state is " + _txItem.GetState());
                        }
                    }
                }

                if (!success)
                {
                    throw new TransactionException(_txId, "Unable to add request to transaction - too much contention for the tx record");
                }
            }
            else
            {
                TxAssert(TransactionItem.State.Pending.Equals(_txItem.GetState()), _txId, "Attempted to add a request to a transaction that was not in state " + TransactionItem.State.Pending, "state", _txItem.GetState());
            }

            // 2. Write txId to item
            Dictionary<string, AttributeValue> item = await LockItemAsync(callerRequest, true, ItemLockAcquireAttempts);

            //    As long as this wasn't a duplicate read request,
            // 3. Save the item image to a new item in case we need to roll back, unless:
            //    - it's a lock request,
            //    - we've already saved the item image
            //    - the item is transient (inserted for acquiring the lock)
            SaveItemImage(callerRequest, item);

            // 3a. Re-read the transaction item to make sure it hasn't been rolled back or completed.
            //     Can be optimized if we know the transaction is already completed(
            try
            {
                _txItem = new TransactionItem(_txId, _txManager, false);
            }
            catch (TransactionNotFoundException)
            {
                await ReleaseReadLockAsync(callerRequest.TableName, await callerRequest.GetKeyAsync(_txManager));
                throw;
            }
            switch (_txItem.GetState())
            {
                case TransactionItem.State.Committed:
                    await DoCommitAsync();
                    throw new TransactionCommittedException(_txId, "The transaction already committed");
                case TransactionItem.State.RolledBack:
                    await DoRollbackAsync();
                    throw new TransactionRolledBackException(_txId, "The transaction already rolled back");
                case TransactionItem.State.Pending:
                    break;
                default:
                    throw new TransactionException(_txId, "Unexpected state " + _txItem.GetState());
            }

            // 4. Apply change to item, keeping lock on the item, returning the attributes according to RETURN_VALUE
            //    If we are a read request, and there is an applied delete request for the same item in the tx, return null.
            Dictionary<string, AttributeValue> returnItem = await ApplyAndKeepLockAsync(callerRequest, item);

            // 5. Optimization: Keep track of the requests that this transaction object has fully applied
            if (callerRequest.Rid != null)
            {
                _fullyAppliedRequests.Add(callerRequest.Rid);
            }

            return returnItem;
        }

        protected internal virtual void SaveItemImage(Request callerRequest, Dictionary<string, AttributeValue> item)
        {
            if (IsRequestSaveable(callerRequest, item) && !item.ContainsKey(AttributeName.Applied.ToString()))
            {
                _txItem.SaveItemImage(item, callerRequest.Rid);
            }
        }

        protected internal virtual bool IsRequestSaveable(Request callerRequest, Dictionary<string, AttributeValue> item)
        {
            if (!(callerRequest is Request.GetItem) && !item.ContainsKey(AttributeName.Transient.ToString()))
            {
                return true;
            }
            return false;
        }


        /// <summary>
        /// Attempts to lock an item.  If the conditional write fails, we read the item to see if we already hold the lock.
        /// If that read reveals no lock owner, then we attempt again to acquire the lock, for a total of "attempts" times.  
        /// </summary>
        /// <param name="callerRequest"> </param>
        /// <param name="attempts"> </param>
        /// <returns> the locked item image </returns>
        /// <exception cref="ItemNotLockedException"> when the item is locked by another transaction </exception>
        /// <exception cref="TransactionException"> when we ran out of attempts to write the item, but it did not appear to be owned </exception>
        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: protected java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> lockItemAsync(Request callerRequest, boolean expectExists, int attempts) throws com.amazonaws.services.dynamodbv2.transactions.exceptions.ItemNotLockedException, com.amazonaws.services.dynamodbv2.transactions.exceptions.TransactionException
        protected internal virtual async Task<Dictionary<string, AttributeValue>> LockItemAsync(Request callerRequest, bool expectExists, int attempts)
        {
            Dictionary<string, AttributeValue> key = await callerRequest.GetKeyAsync(_txManager);

            if (attempts <= 0)
            {
                throw new TransactionException(_txId, "Unable to acquire item lock for item " + key); // This won't trigger a rollbackAsync, it's really just a case of contention and needs more redriving
            }

            // Create Expected and Updates maps.  
            //   - If we expect the item TO exist, we only update the lock
            //   - If we expect the item NOT to exist, we update both the transient attribute and the lock.
            // In both cases we expect the txid not to be set
            Dictionary<string, AttributeValueUpdate> updates = new Dictionary<string, AttributeValueUpdate>();
            updates[AttributeName.Txid.ToString()] = new AttributeValueUpdate
            {
                Action = AttributeAction.PUT,
                Value = new AttributeValue(_txId)
            };
            updates[AttributeName.Date.ToString()] = new AttributeValueUpdate
            {
                Action = AttributeAction.PUT,
                Value = _txManager.CurrentTimeAttribute
            };

            Dictionary<string, ExpectedAttributeValue> expected;
            if (expectExists)
            {
                expected = await callerRequest.GetExpectExists(_txManager);
                expected[AttributeName.Txid.ToString()] = new ExpectedAttributeValue
                {
                    Exists = false
                };
            }
            else
            {
                expected = new Dictionary<string, ExpectedAttributeValue>(1);
                updates.Add(AttributeName.Transient.ToString(), new AttributeValueUpdate
                {
                    Action = AttributeAction.PUT,
                    Value = new AttributeValue {S = BooleanTrueAttrVal}
                });
            }
            expected[AttributeName.Txid.ToString()] = new ExpectedAttributeValue
            {
                Exists = false
            };

            // Do a conditional update on NO transaction id, and that the item DOES exist
            UpdateItemRequest updateRequest = new UpdateItemRequest
            {
                TableName = callerRequest.TableName,
                Expected = expected,
                Key = key,
                ReturnValues = ReturnValue.ALL_NEW,
                AttributeUpdates = updates
            };

            string owner = null;
            bool nextExpectExists = false;
            Dictionary<string, AttributeValue> item = null;
            try
            {
                item = (await _txManager.Client.UpdateItemAsync(updateRequest)).Attributes;
                owner = GetOwner(item);
            }
            catch (ConditionalCheckFailedException)
            {
                // If the check failed, it means there is either:
                //   1) a different transaction currently locking the item
                //   2) this transaction already is attempting to lock the item.  
                //   3) the item does not exist
                // Get the item and see which is the case
                item = await GetItemAsync(callerRequest.TableName, key);
                if (item == null)
                {
                    nextExpectExists = false;
                }
                else
                {
                    nextExpectExists = true;
                    owner = GetOwner(item);
                }
            }

            // Try the write again if the item is unowned (but not if it is owned)
            if (!string.ReferenceEquals(owner, null))
            {
                if (_txId.Equals(owner))
                {
                    return item;
                }
                // For now, always roll back / completeAsync the other transaction in the case of a conflict.
                if (attempts > 1)
                {
                    try
                    {
                        Transaction otherTransaction = _txManager.ResumeTransaction(owner);
                        await otherTransaction.RollbackAsync();
                    }
                    catch (TransactionCompletedException)
                    {
                        // no-op
                    }
                    catch (TransactionNotFoundException)
                    {
                        await ReleaseReadLockAsync(owner, _txManager, callerRequest.TableName, key);
                    }
                }
                else
                {
                    throw new ItemNotLockedException(_txId, owner, callerRequest.TableName, key);
                }
            }
            return await LockItemAsync(callerRequest, nextExpectExists, attempts - 1);
        }


        /// <summary>
        /// Writes the request to the user table and keeps the lock, as long as we still have the lock.
        /// Ensures that the write happens (at most) once, because the write atomically marks the item as applied.
        /// 
        /// This is a no-op for DeleteItem or LockItem requests, since for delete the item isn't removed until after
        /// the transaction commits, and lock doesn't mutate the item.
        /// 
        /// Note that this method mutates the item and the request.
        /// </summary>
        /// <param name="request"> </param>
        /// <param name="lockedItem"> </param>
        /// <returns> the copy of the item, as requested in ReturnValues of the request (or the new item in the case of a read), or null if this is a redrive  </returns>
        protected internal virtual async Task<Dictionary<string, AttributeValue>> ApplyAndKeepLockAsync(Request request, Dictionary<string, AttributeValue> lockedItem)
        {
            Dictionary<string, AttributeValue> returnItem = null;

            // 1. Remember what return values the caller wanted.
            string returnValues = request.ReturnValues; // saveAsync the returnValues because we will mutate it
            if (string.ReferenceEquals(returnValues, null))
            {
                returnValues = "NONE";
            }

            // 3. No-op if the locked item shows it was already applied.
            if (!lockedItem.ContainsKey(AttributeName.Applied.ToString()))
            {
                try
                {
                    Dictionary<string, ExpectedAttributeValue> expected = new Dictionary<string, ExpectedAttributeValue>();
                    expected[AttributeName.Txid.ToString()] = new ExpectedAttributeValue
                    {
                        Value = new AttributeValue(_txId)
                    };
                    expected[AttributeName.Applied.ToString()] = new ExpectedAttributeValue
                    {
                        Exists = false
                    };

                    // TODO assert if the caller request contains any of our internally defined fields?
                    //      but we aren't copying the request object, so our retries might trigger the assertion.
                    //      at least could assert that they have the values that we want.
                    if (request is Request.PutItem)
                    {
                        PutItemRequest put = ((Request.PutItem)request).Request;
                        // Add the lock id and "is transient" flags to the put request (put replaces) 
                        put.Item.Add(AttributeName.Txid.ToString(), new AttributeValue(_txId));
                        put.Item.Add(AttributeName.Applied.ToString(), new AttributeValue(BooleanTrueAttrVal));
                        if (lockedItem.ContainsKey(AttributeName.Transient.ToString()))
                        {
                            put.Item.Add(AttributeName.Transient.ToString(), lockedItem[AttributeName.Transient.ToString()]);
                        }
                        put.Item.Add(AttributeName.Date.ToString(), lockedItem[AttributeName.Date.ToString()]);
                        put.Expected = expected;
                        put.ReturnValues = returnValues;
                        returnItem = (await _txManager.Client.PutItemAsync(put)).Attributes;
                    }
                    else if (request is Request.UpdateItem)
                    {
                        UpdateItemRequest update = ((Request.UpdateItem)request).Request;
                        update.Expected = expected;
                        update.ReturnValues = returnValues;

                        if (update.AttributeUpdates != null)
                        {
                            // Defensively delete the attributes in the request that could interfere with the transaction
                            update.AttributeUpdates.Remove(AttributeName.Txid.ToString());
                            update.AttributeUpdates.Remove(AttributeName.Transient.ToString());
                            update.AttributeUpdates.Remove(AttributeName.Date.ToString());
                        }
                        else
                        {
                            update.AttributeUpdates = new Dictionary<string, AttributeValueUpdate>(1);
                        }

                        update.AttributeUpdates.Add(AttributeName.Applied.ToString(), new AttributeValueUpdate
                        {
                            Action = AttributeAction.PUT,
                            Value = new AttributeValue(BooleanTrueAttrVal),

                        });

                        returnItem = (await _txManager.Client.UpdateItemAsync(update)).Attributes;
                    }
                    else if (request is Request.DeleteItem)
                    {
                        // no-op - delete doesn't change the item until unlock post-commitAsync
                    }
                    else if (request is Request.GetItem)
                    {
                        // no-op
                    }
                    else
                    {
                        throw new TransactionAssertionException(_txId, "Request may not be null");
                    }
                }
                catch (ConditionalCheckFailedException)
                {
                    // ignore - apply already happened
                }
            }

            // If it is a redrive, don't return an item.
            // TODO propagate a flag for whether this is a caller request or if it's being redriven by another transaction manager picking it up.
            //      In that case it doesn't matter what we do here.
            //      Also change the returnValues in the write requests based on this.
            if ("ALL_OLD".Equals(returnValues) && IsTransient(lockedItem))
            {
                return null;
            }
            else if (request is Request.GetItem)
            {
                GetItemRequest getRequest = ((Request.GetItem)request).Request;
                Request lockingRequest = _txItem.GetRequestForKey(request.TableName, await request.GetKeyAsync(_txManager));
                if (lockingRequest is Request.DeleteItem)
                {
                    return null; // If the item we're getting is deleted in this transaction
                }
                else if (lockingRequest is Request.GetItem && IsTransient(lockedItem))
                {
                    return null; // If the item has only a read lock and is transient
                }
                else if (getRequest.AttributesToGet != null)
                {
                    // Remove attributes that weren't asked for in the request
                    ISet<string> attributesToGet = new HashSet<string>(getRequest.AttributesToGet);
                    var toRemove = lockedItem.Where(x => !attributesToGet.Contains(x.Key));
                    foreach (var item in toRemove)
                    {
                        lockedItem.Remove(item.Key); // TODO does this need to keep the tx attributes
                    }
                }
                return lockedItem;
            }
            else if (request is Request.DeleteItem)
            {
                if ("ALL_OLD".Equals(returnValues))
                {
                    return lockedItem; // Deletes are left alone in apply, so return the locked item
                }
                return null; // In the case of NONE or ALL_NEW, it doesn't matter - item is (being) deleted.
            }
            else if ("ALL_OLD".Equals(returnValues))
            {
                if (returnItem != null)
                {
                    return returnItem; // If the apply write succeeded, we have the ALL_OLD from the request
                }
                returnItem = await _txItem.LoadItemImageAsync(request.Rid);
                if (returnItem == null)
                {
                    throw new UnknownCompletedTransactionException(_txId, "Transaction must have completed since the old copy of the image is missing");
                }
                return returnItem;
            }
            else if ("ALL_NEW".Equals(returnValues))
            {
                if (returnItem != null)
                {
                    return returnItem; // If the apply write succeeded, we have the ALL_NEW from the request
                }
                returnItem = await GetItemAsync(request.TableName, await request.GetKeyAsync(_txManager));
                if (returnItem == null)
                {
                    throw new UnknownCompletedTransactionException(_txId, "Transaction must have completed since the item no longer exists");
                }
                string owner = GetOwner(returnItem);
                if (!_txId.Equals(owner))
                {
                    throw new ItemNotLockedException(_txId, owner, request.TableName, returnItem);
                }
                return returnItem;
            }
            else if ("NONE".Equals(returnValues))
            {
                return null;
            }
            else
            {
                throw new TransactionAssertionException(_txId, "Unsupported return values: " + returnValues);
            }
        }

        /// <summary>
        /// Returns a copy of the requested item all attributes retrieved.  Performs a consistent read.
        /// </summary>
        /// <param name="tableName"> </param>
        /// <param name="key"> </param>
        /// <returns> the item map, with all attributes fetched </returns>
        protected internal virtual async Task<Dictionary<string, AttributeValue>> GetItemAsync(string tableName, Dictionary<string, AttributeValue> key)
        {
            return await GetItemAsync(_txManager, tableName, key);
        }

        protected internal static async Task<Dictionary<string, AttributeValue>> GetItemAsync(TransactionManager txManager, string tableName, Dictionary<string, AttributeValue> key)
        {
            GetItemRequest getRequest = new GetItemRequest
            {
                TableName = tableName,
                ConsistentRead = true,
                Key = key
            };
            GetItemResponse getResult = await txManager.Client.GetItemAsync(getRequest);
            return getResult.Item;
        }

        /// <summary>
        /// Determines the current lock holder for the given item
        /// </summary>
        /// <param name="item"> must not be null </param>
        /// <returns> the owning transaction id, or null if the item isn't locked </returns>
        protected internal static string GetOwner(Dictionary<string, AttributeValue> item)
        {
            if (item == null)
            {
                throw new System.ArgumentException();
            }
            AttributeValue itemTxId = item[AttributeName.Txid.ToString()];
            if (itemTxId != null && itemTxId.S != null)
            {
                return itemTxId.S;
            }
            return null;
        }

        /// <summary>
        /// For unit tests </summary>
        /// <returns> the current transaction item </returns>
        protected internal virtual TransactionItem TxItem
        {
            get
            {
                return _txItem;
            }
        }

        public sealed class AttributeName
        {
            public static readonly AttributeName Txid = new AttributeName("TXID", InnerEnum.Txid, TxAttrPrefix + "Id");
            public static readonly AttributeName Transient = new AttributeName("TRANSIENT", InnerEnum.Transient, TxAttrPrefix + "T");
            public static readonly AttributeName Date = new AttributeName("DATE", InnerEnum.Date, TxAttrPrefix + "D");
            public static readonly AttributeName Applied = new AttributeName("APPLIED", InnerEnum.Applied, TxAttrPrefix + "A");
            public static readonly AttributeName Requests = new AttributeName("REQUESTS", InnerEnum.Requests, TxAttrPrefix + "R");
            public static readonly AttributeName State = new AttributeName("STATE", InnerEnum.State, TxAttrPrefix + "S");
            public static readonly AttributeName Version = new AttributeName("VERSION", InnerEnum.Version, TxAttrPrefix + "V");
            public static readonly AttributeName Finalized = new AttributeName("FINALIZED", InnerEnum.Finalized, TxAttrPrefix + "F");
            public static readonly AttributeName ImageId = new AttributeName("IMAGE_ID", InnerEnum.ImageId, TxAttrPrefix + "I");

            private static readonly List<AttributeName> ValueList = new List<AttributeName>();

            static AttributeName()
            {
                ValueList.Add(Txid);
                ValueList.Add(Transient);
                ValueList.Add(Date);
                ValueList.Add(Applied);
                ValueList.Add(Requests);
                ValueList.Add(State);
                ValueList.Add(Version);
                ValueList.Add(Finalized);
                ValueList.Add(ImageId);
            }

            public enum InnerEnum
            {
                Txid,
                Transient,
                Date,
                Applied,
                Requests,
                State,
                Version,
                Finalized,
                ImageId
            }

            private readonly string _nameValue;
            private readonly int _ordinalValue;
            private readonly InnerEnum _innerEnumValue;
            private static int _nextOrdinal = 0;

            internal AttributeName(string name, InnerEnum innerEnum, string value)
            {
                this.Value = value;

                _nameValue = name;
                _ordinalValue = _nextOrdinal++;
                _innerEnumValue = innerEnum;
            }

            internal readonly string Value;

            public override string ToString()
            {
                return Value;
            }

            public static List<AttributeName> Values()
            {
                return ValueList;
            }

            public InnerEnum InnerEnumValue()
            {
                return _innerEnumValue;
            }

            public int Ordinal()
            {
                return _ordinalValue;
            }

            public static AttributeName ValueOf(string name)
            {
                foreach (AttributeName enumInstance in AttributeName.Values())
                {
                    if (enumInstance._nameValue == name)
                    {
                        return enumInstance;
                    }
                }
                throw new System.ArgumentException(name);
            }
        }

        /// <summary>
        /// Delete an item using the mapper.
        /// </summary>
        /// <param name="item">
        ///            An item object with key attributes populated. </param>
        public virtual async Task DeleteAsync<T>(T item)
        {
            await DoWithMapperAsync(new CallableAnonymousInnerClass<T>(this, item));
        }

        private class CallableAnonymousInnerClass<T> : Callable<object>
        {
            private readonly Transaction _outerInstance;

            private T _item;

            public CallableAnonymousInnerClass(Transaction outerInstance, T item)
            {
                this._outerInstance = outerInstance;
                this._item = item;
            }

            public override async Task<object> CallAsync()
            {
                await _outerInstance._txManager.ClientMapper.DeleteAsync(_item);
                return null;
            }
        }

        /// <summary>
        /// Load an item using the mapper.
        /// </summary>
        /// <param name="item">
        ///            An item object with key attributes populated. </param>
        /// <returns> An instance of the item class with all attributes populated from
        ///         the table, or null if the item does not exist as of the start of
        ///         this transaction. </returns>
        public virtual async Task<T> LoadAsync<T>(T item)
        {
            return await DoWithMapperAsync(new CallableAnonymousInnerClass2<T>(this, item));
        }

        private class CallableAnonymousInnerClass2<T> : Callable<T>
        {
            private readonly Transaction _outerInstance;

            private T _item;

            public CallableAnonymousInnerClass2(Transaction outerInstance, T item)
            {
                this._outerInstance = outerInstance;
                this._item = item;
            }

            public override async Task<T> CallAsync()
            {
                return await _outerInstance._txManager.ClientMapper.LoadAsync<T>(_item);
            }
        }

        /// <summary>
        /// Save an item using the mapper.
        /// </summary>
        /// <param name="item">
        ///            An item object with key attributes populated. </param>
        public virtual async Task SaveAsync<T>(T item)
        {
            await DoWithMapperAsync(new CallableAnonymousInnerClass3<T>(this, item));
        }

        private class CallableAnonymousInnerClass3<T> : Callable<object>
        {
            private readonly Transaction _outerInstance;

            private T _item;

            public CallableAnonymousInnerClass3(Transaction outerInstance, T item)
            {
                this._outerInstance = outerInstance;
                this._item = item;
            }

            public override async Task<object> CallAsync()
            {
                await _outerInstance._txManager.ClientMapper.SaveAsync(_item);
                return null;
            }
        }

        private async Task<T> DoWithMapperAsync<T>(Callable<T> callable)
        {
            try
            {
                _txManager.FacadeProxy.Backend = new TransactionDynamoDbFacade(this, _txManager);
                return await callable.CallAsync();
            }
            finally
            {
                _txManager.FacadeProxy.Backend = null;
            }
        }

    }

}