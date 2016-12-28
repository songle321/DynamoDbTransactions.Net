using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using com.amazonaws.services.dynamodbv2.transactions.exceptions;
using static com.amazonaws.services.dynamodbv2.transactions.Transaction;
using static com.amazonaws.services.dynamodbv2.transactions.exceptions.TransactionAssertionException;

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
    ///       transactions that attempt to lock any of the items in your transactions will finish your transaction before making progress in their own.
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
    ///       getItem in <seealso cref="TransactionManager"/> for read options that handle dealing with these "half written" items for you.</li>
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
        private static readonly Log LOG = LogFactory.getLog(typeof(Transaction));

        private const int ITEM_LOCK_ACQUIRE_ATTEMPTS = 3;
        private const int ITEM_COMMIT_ATTEMPTS = 2;
        private const int TX_LOCK_ACQUIRE_ATTEMPTS = 2;
        private const int TX_LOCK_CONTENTION_RESOLUTION_ATTEMPTS = 3;
        protected internal const string BOOLEAN_TRUE_ATTR_VAL = "1";

        /* Attribute name constants */
        protected internal const string TX_ATTR_PREFIX = "_Tx";
        public static readonly ISet<string> SPECIAL_ATTR_NAMES;

        private readonly TransactionManager txManager;
        private TransactionItem txItem;
        private readonly string txId;
        private readonly SortedSet<int?> fullyAppliedRequests = new SortedSet<int?>();

        private SemaphoreSlim semaphore = new SemaphoreSlim(1);

        static Transaction()
        {
            ISet<string> names = new HashSet<string>();
            foreach (AttributeName name in AttributeName.values())
            {
                names.Add(name.ToString());
            }
            SPECIAL_ATTR_NAMES = names;
        }

        /// <summary>
        /// default constructor to make cglib/spring able to proxy Transactions.
        /// </summary>
        protected internal Transaction()
        {
            this.txManager = null;
            this.txItem = null;
            this.txId = null;
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
            this.txManager = txManager;
            this.txItem = new TransactionItem(txId, txManager, insert);
            this.txId = txId;
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
            this.txManager = txManager;
            this.txItem = new TransactionItem(txItem, txManager);
            this.txId = this.txItem.txId;
        }

        public virtual string Id
        {
            get
            {
                return txId;
            }
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
        //ORIGINAL LINE: public com.amazonaws.services.dynamodbv2.model.PutItemResponse putItem(com.amazonaws.services.dynamodbv2.model.PutItemRequest request) throws com.amazonaws.services.dynamodbv2.transactions.exceptions.DuplicateRequestException, com.amazonaws.services.dynamodbv2.transactions.exceptions.ItemNotLockedException, com.amazonaws.services.dynamodbv2.transactions.exceptions.TransactionCompletedException, com.amazonaws.services.dynamodbv2.transactions.exceptions.TransactionNotFoundException, com.amazonaws.services.dynamodbv2.transactions.exceptions.TransactionException
        public virtual PutItemResponse putItem(PutItemRequest request)
        {
Request.PutItem wrappedRequest = new Request.PutItem();
            wrappedRequest.Request = request;
            Dictionary<string, AttributeValue> item = driveRequest(wrappedRequest);
            stripSpecialAttributes(item);
            return new PutItemResponse {
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
        //ORIGINAL LINE: public com.amazonaws.services.dynamodbv2.model.UpdateItemResponse updateItem(com.amazonaws.services.dynamodbv2.model.UpdateItemRequest request) throws com.amazonaws.services.dynamodbv2.transactions.exceptions.DuplicateRequestException, com.amazonaws.services.dynamodbv2.transactions.exceptions.ItemNotLockedException, com.amazonaws.services.dynamodbv2.transactions.exceptions.TransactionCompletedException, com.amazonaws.services.dynamodbv2.transactions.exceptions.TransactionNotFoundException, com.amazonaws.services.dynamodbv2.transactions.exceptions.TransactionException
        public virtual UpdateItemResponse updateItem(UpdateItemRequest request)
        {
Request.UpdateItem wrappedRequest = new Request.UpdateItem();
            wrappedRequest.Request = request;
            Dictionary<string, AttributeValue> item = driveRequest(wrappedRequest);
            stripSpecialAttributes(item);
            return new UpdateItemResponse {
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
        //ORIGINAL LINE: public com.amazonaws.services.dynamodbv2.model.DeleteItemResponse deleteItem(com.amazonaws.services.dynamodbv2.model.DeleteItemRequest request) throws com.amazonaws.services.dynamodbv2.transactions.exceptions.DuplicateRequestException, com.amazonaws.services.dynamodbv2.transactions.exceptions.ItemNotLockedException, com.amazonaws.services.dynamodbv2.transactions.exceptions.TransactionCompletedException, com.amazonaws.services.dynamodbv2.transactions.exceptions.TransactionNotFoundException, com.amazonaws.services.dynamodbv2.transactions.exceptions.TransactionException
        public virtual DeleteItemResponse deleteItem(DeleteItemRequest request)
        {
Request.DeleteItem wrappedRequest = new Request.DeleteItem();
            wrappedRequest.Request = request;
            Dictionary<string, AttributeValue> item = driveRequest(wrappedRequest);
            stripSpecialAttributes(item);
            return new DeleteItemResponse {
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
        //ORIGINAL LINE: public com.amazonaws.services.dynamodbv2.model.GetItemResponse getItem(com.amazonaws.services.dynamodbv2.model.GetItemRequest request) throws com.amazonaws.services.dynamodbv2.transactions.exceptions.DuplicateRequestException, com.amazonaws.services.dynamodbv2.transactions.exceptions.ItemNotLockedException, com.amazonaws.services.dynamodbv2.transactions.exceptions.TransactionCompletedException, com.amazonaws.services.dynamodbv2.transactions.exceptions.TransactionNotFoundException, com.amazonaws.services.dynamodbv2.transactions.exceptions.TransactionException
        public virtual GetItemResponse getItem(GetItemRequest request)
        {
Request.GetItem wrappedRequest = new Request.GetItem();
            wrappedRequest.Request = request;
            Dictionary<string, AttributeValue> item = driveRequest(wrappedRequest);
            stripSpecialAttributes(item);
            GetItemResponse result = new GetItemResponse {
Item = item
};
            return result;
        }

        public static void stripSpecialAttributes(Dictionary<string, AttributeValue> item)
        {
            if (item == null)
            {
                return;
            }
            foreach (string specialAttribute in SPECIAL_ATTR_NAMES)
            {
                item.Remove(specialAttribute);
            }
        }

        public static bool isLocked(Dictionary<string, AttributeValue> item)
        {
            if (item == null)
            {
                return false;
            }
            if (item.ContainsKey(AttributeName.TXID.ToString()))
            {
                return true;
            }
            return false;
        }

        public static bool isApplied(Dictionary<string, AttributeValue> item)
        {
            if (item == null)
            {
                return false;
            }
            if (item.ContainsKey(AttributeName.APPLIED.ToString()))
            {
                return true;
            }
            return false;
        }

        public static bool isTransient(Dictionary<string, AttributeValue> item)
        {
            if (item == null)
            {
                return false;
            }
            if (item.ContainsKey(AttributeName.TRANSIENT.ToString()))
            {
                return true;
            }
            return false;
        }

        public enum IsolationLevel
        {
            UNCOMMITTED,
            COMMITTED,
            READ_LOCK // what does it mean to read an item you wrote to in a transaction?
        }

        /// <summary>
        /// Deletes the transaction.  
        /// </summary>
        /// <returns> true if the transaction was deleted, false if it was not </returns>
        /// <exception cref="TransactionException"> if the transaction is not yet completed. </exception>
        public virtual async Task<bool> deleteAsync()
        {
            return await deleteIfAfterAsync(null);
        }
        /// <summary>
        /// Deletes the transaction, only if it has not been update since the specified duration.  A transaction's 
        /// "last updated date" is updated when:
        ///  - A request is added to the transaction
        ///  - The transaction switches to COMMITTED or ROLLED_BACK
        ///  - The transaction is marked as completed.  
        /// </summary>
        /// <param name="deleteIfAfterMillis"> the duration to ensure has passed before attempting to deleteAsync the record </param>
        /// <returns> true if the transaction was deleted, false if it was not old enough to deleteAsync yet. </returns>
        /// <exception cref="TransactionException"> if the transaction is not yet completed. </exception>
        public virtual async Task<bool> deleteAsync(long deleteIfAfterMillis)
        {
            return await deleteIfAfterAsync(deleteIfAfterMillis);
        }

        private async Task<bool> deleteIfAfterAsync(long? deleteIfAfterMillis)
        {
            await semaphore.WaitAsync();
            try
            {
                if (!txItem.Completed)
                {
                    // Ensure we have an up to date tx record
                    try
                    {
                        txItem = new TransactionItem(txId, txManager, false);
                    }
                    catch (TransactionNotFoundException)
                    {
                        return true; // expected, transaction already deleted
                    }
                    if (!txItem.Completed)
                    {
                        throw new TransactionException(txId, "You can only deleteAsync a transaction that is completed");
                    }
                }
                try
                {
                    if (deleteIfAfterMillis == null ||
                        (txItem.LastUpdateTimeMillis + deleteIfAfterMillis) <
                        DateTimeHelperClass.CurrentUnixTimeMillis())
                    {
                        txItem.delete();
                        return true;
                    }
                }
                catch (ConditionalCheckFailedException)
                {
                    // Can only happen if the tx isn't finalized or is already gone.
                    try
                    {
                        txItem = new TransactionItem(txId, txManager, false);
                        throw new TransactionException(txId,
                            "Transaction was completed but could not be deleted. " + txItem);
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
                semaphore.Release();
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
        public virtual void sweep(long rollbackAfterDurationMills, long deleteAfterDurationMillis)
        {
            // If the item has been completed for the specified threshold, deleteAsync it.
            if (txItem.Completed)
            {
                deleteAsync(deleteAfterDurationMillis);
            }
            else
            {
                // If the transaction has been PENDING for too long, roll it back.
                // If it's COMMITTED or PENDING, drive it to completion. 
                switch (txItem.getState())
                {
                    case TransactionItem.State.PENDING:
                        if ((txItem.LastUpdateTimeMillis + rollbackAfterDurationMills) < DateTimeHelperClass.CurrentUnixTimeMillis())
                        {
                            try
                            {
                                rollback();
                            }
                            catch (TransactionCompletedException)
                            {
                                // Transaction is already completed, ignore
                            }
                        }
                        break;
                    case TransactionItem.State.COMMITTED: // NOTE: falling through to ROLLED_BACK
                    case TransactionItem.State.ROLLED_BACK:
                        // This could call either commitAsync or rollback - they'll both do the right thing if it's already committed
                        try
                        {
                            rollback();
                        }
                        catch (TransactionCompletedException)
                        {
                            // Transaction is already completed, ignore
                        }
                        break;
                    default:
                        throw new TransactionAssertionException(txId, "Unexpected state in transaction: " + txItem.getState());
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
        //ORIGINAL LINE: protected synchronized java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> driveRequest(Request clientRequest) throws com.amazonaws.services.dynamodbv2.transactions.exceptions.DuplicateRequestException, com.amazonaws.services.dynamodbv2.transactions.exceptions.ItemNotLockedException, com.amazonaws.services.dynamodbv2.transactions.exceptions.TransactionException
        protected internal virtual Dictionary<string, AttributeValue> driveRequest(Request clientRequest)
        {
            lock (this)
            {
                /* 1. Validate the request (no conditions, required attributes, no conflicting attributes etc)
				 * 2. Copy the request (we change things in it, so don't mutate the caller's request
				 * 3. Acquire the lock, save image, apply via addRequest()
				 *    a) If that fails, go to 3) again.
				 */

                // basic validation
                clientRequest.validate(txId, txManager);

                // Don't mutate the caller's request.
                Request requestCopy = Request.deserialize(txId, Request.serialize(txId, clientRequest));

                ItemNotLockedException lastConflict = null;
                for (int i = 0; i < TX_LOCK_CONTENTION_RESOLUTION_ATTEMPTS; i++)
                {
                    try
                    {
                        Dictionary<string, AttributeValue> item = addRequest(requestCopy, (i != 0), TX_LOCK_ACQUIRE_ATTEMPTS);
                        return item;
                    }
                    catch (ItemNotLockedException e)
                    {
                        // Roll back or complete the other transaction
                        lastConflict = e;
                        Transaction conflictingTransaction = null;
                        try
                        {
                            conflictingTransaction = new Transaction(e.LockOwnerTxId, txManager, false);
                            conflictingTransaction.rollback();
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
        public virtual async Task commitAsync()
        {
            await semaphore.WaitAsync();
            try
            {
                // 1. Re-read transaction item
                //    a) If it doesn't exist, throw UnknownCompletedTransaction 

                // 2. Verify that we should continue
                //    a) If the transaction is closed, return or throw depending on COMPLETE or ROLLED_BACK.
                //    b) If the transaction is ROLLED_BACK, continue doRollback(), but at the end throw.
                //    c) If the transaction is COMMITTED, go to doCommitAsync(), and at the end return.

                // 3. Save the transaction's version number to detect if additional requests are added

                // 4. Verify that we have all of the locks and their saved images

                // 5. Change the state to COMMITTED conditioning on: 
                //         - it isn't closed
                //         - the version number hasn't changed
                //         - it's still PENDING
                //      a) If that fails, go to 1).

                // 6. Return success

                for (int i = 0; i < ITEM_COMMIT_ATTEMPTS + 1; i++)
                {
                    // Re-read state to ensure this isn't a resume that's going to come along and re-apply a completed transaction.
                    // This doesn't prevent a transaction from being applied multiple times, but it prevents a sweeper from applying
                    // a very old transaction.
                    try
                    {
                        txItem = new TransactionItem(txId, txManager, false);
                    }
                    catch (TransactionNotFoundException)
                    {
                        throw new UnknownCompletedTransactionException(txId,
                            "In transaction " + TransactionItem.State.COMMITTED +
                            " attempt, transaction either rolled back or committed");
                    }

                    if (txItem.Completed)
                    {
                        if (TransactionItem.State.COMMITTED.Equals(txItem.getState()))
                        {
                            return;
                        }
                        else if (TransactionItem.State.ROLLED_BACK.Equals(txItem.getState()))
                        {
                            throw new TransactionRolledBackException(txId, "Transaction was rolled back");
                        }
                        else
                        {
                            throw new TransactionAssertionException(txId,
                                "Unexpected state for transaction: " + txItem.getState());
                        }
                    }

                    if (TransactionItem.State.COMMITTED.Equals(txItem.getState()))
                    {
                        await doCommitAsync();
                        return;
                    }

                    if (TransactionItem.State.ROLLED_BACK.Equals(txItem.getState()))
                    {
                        doRollback();
                        throw new TransactionRolledBackException(txId, "Transaction was rolled back");
                    }

                    // Commit attempts is actually for the number of times we try to acquire all the locks
                    if (!(i < ITEM_COMMIT_ATTEMPTS))
                    {
                        throw new TransactionException(txId,
                            "Unable to commitAsync transaction after " + ITEM_COMMIT_ATTEMPTS + " attempts");
                    }

                    int version = txItem.Version;

                    verifyLocks();

                    try
                    {
                        txItem.finish(TransactionItem.State.COMMITTED, version);
                    }
                    catch (ConditionalCheckFailedException)
                    {
                        // Tx item version, changed out from under us, or was moved to committed, rolled back, deleted, etc by someone else.
                        // Retry in loop
                    }
                }

                throw new TransactionException(txId,
                    "Unable to commitAsync transaction after " + ITEM_COMMIT_ATTEMPTS + " attempts");
            }
            finally
            { 
                semaphore.Release();
            }
        }

        /// <summary>
        /// Rolls back the transaction.  You can only roll back a transaction that is in the PENDING state (not yet committed).
        /// <li>If you roll back a transaction in COMMITTED, this will continue committing the transaction if it isn't completed yet, 
        ///      but you will get back a TransactionCommittedException. </li>
        /// <li>If you roll back and already rolled back transaction, this will ensure the rollback completed, and return success</li>
        /// <li>If the transaction no longer exists, you'll get back an UnknownCompletedTransactionException</li>   
        /// </summary>
        /// <exception cref="TransactionCommittedException"> - the transaction was committed by a concurrent overlapping transaction </exception>
        /// <exception cref="UnknownCompletedTransactionException"> - the transaction completed, but it is not known whether it was rolled back or committed </exception>
        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public synchronized void rollback() throws com.amazonaws.services.dynamodbv2.transactions.exceptions.TransactionCompletedException, com.amazonaws.services.dynamodbv2.transactions.exceptions.UnknownCompletedTransactionException
        public virtual async Task rollback()
        {
            await semaphore.WaitAsync();
            try
            {
                TransactionItem.State state;
                bool alreadyRereadTxItem = false;
                try
                {
                    txItem.finish(TransactionItem.State.ROLLED_BACK, txItem.Version);
                    state = TransactionItem.State.ROLLED_BACK;
                }
                catch (ConditionalCheckFailedException)
                {
                    try
                    {
                        // Re-read state to see its actual state, since it wasn't in PENDING
                        txItem = new TransactionItem(txId, txManager, false);
                        alreadyRereadTxItem = true;
                        state = txItem.getState();
                    }
                    catch (TransactionNotFoundException)
                    {
                        throw new UnknownCompletedTransactionException(txId,
                            "In transaction " + TransactionItem.State.ROLLED_BACK +
                            " attempt, transaction either rolled back or committed");
                    }
                }

                if (TransactionItem.State.COMMITTED.Equals(state))
                {
                    if (!txItem.Completed)
                    {
                        doCommitAsync();
                    }
                    throw new TransactionCommittedException(txId, "Transaction was committed");
                }
                else if (TransactionItem.State.ROLLED_BACK.Equals(state))
                {
                    if (!txItem.Completed)
                    {
                        doRollback();
                    }
                    return;
                }
                else if (TransactionItem.State.PENDING.Equals(state))
                {
                    if (!alreadyRereadTxItem)
                    {
                        // The item was modified in the meantime (another request was added to it)
                        // so make sure we re-read it, and then try the rollback again
                        txItem = new TransactionItem(txId, txManager, false);
                    }
                    rollback();
                    return;
                }
                throw new TransactionAssertionException(txId, "Unexpected state in rollback(): " + state);
            }
            finally
            {
                semaphore.Release();
            }
        }

        /// <summary>
        /// Verifies that we actually hold all of the locks for the requests in the transaction, and that we have saved the 
        /// previous item images of every item involved in the requests (except for request types that we don't save images for).
        /// 
        /// The caller needs to wrap this with OCC on the tx version (request count) if it's going to commitAsync based on this decision.
        /// 
        /// This is optimized to consider the "version" numbers of the items that this Transaction object has fully applied so far
        /// to optimize the normal case that doesn't have failures.
        /// </summary>
        protected internal virtual void verifyLocks()
        {
            foreach (Request request in txItem.Requests)
            {
                // Optimization: If our transaction object (this) has first-hand fully applied a request, no need to do it again.
                if (!fullyAppliedRequests.Contains(request.Rid))
                {
                    addRequest(request, true, ITEM_LOCK_ACQUIRE_ATTEMPTS);
                }
            }
        }

        /// <summary>
        /// Deletes the transaction item from the database.
        /// 
        /// Does not throw if the item is gone, even if the conditional check to deleteAsync the item fails, and this method doesn't know what state
        /// it was in when deleted.  The caller is responsible for guaranteeing that it was actually in "currentState" immediately before calling
        /// this method.  
        /// </summary>
        //JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
        //ORIGINAL LINE: protected void complete(final State expectedCurrentState)
        protected internal virtual void complete(TransactionItem.State expectedCurrentState)
        {
            try
            {
                txItem.complete(expectedCurrentState);
            }
            catch (ConditionalCheckFailedException)
            {
                // Re-read state to ensure it was already completed
                try
                {
                    txItem = new TransactionItem(txId, txManager, false);
                    if (!txItem.Completed)
                    {
                        throw new TransactionAssertionException(txId, "Expected the transaction to be completed (no item), but there was one.");
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
        /// <param name="cancellationToken"></param>
        protected internal virtual async Task doCommitAsync()
        {
            // Defensively re-check the state to ensure it is COMMITTED
            txAssert(txItem != null && TransactionItem.State.COMMITTED.Equals(txItem.getState()), txId, "doCommitAsync() requires a non-null txItem with a state of " + TransactionItem.State.COMMITTED, "state", txItem.getState(), "txItem", txItem);

            // Note: Order is functionally unimportant, but we unlock all items first to try to reduce the need 
            // for other readers to read this transaction's information since it has already committed.
            foreach (Request request in txItem.Requests)
            {
                //Unlock the item, deleting it if it was inserted only to lock the item, or if it was a deleteAsync request
                await unlockItemAfterCommitAsync(request);
            }

            // Clean up the old item images
            foreach (Request request in txItem.Requests)
            {
                txItem.deleteItemImage(request.Rid.Value);
            }

            complete(TransactionItem.State.COMMITTED);
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
        /// <param name="cancellationToken"></param>
        protected internal virtual async Task unlockItemAfterCommitAsync(Request request)
        {
            try
            {
                Dictionary<string, ExpectedAttributeValue> expected = new Dictionary<string, ExpectedAttributeValue>();
                expected[AttributeName.TXID.ToString()] = new ExpectedAttributeValue {
Value = new AttributeValue(txId)
};

                if (request is Request.PutItem || request is Request.UpdateItem)
                {
                    Dictionary<string, AttributeValueUpdate> updates = new Dictionary<string, AttributeValueUpdate>();
                    updates[AttributeName.TXID.ToString()] = new AttributeValueUpdate {
Action = AttributeAction.DELETE
};
                    updates[AttributeName.TRANSIENT.ToString()] = new AttributeValueUpdate {
Action = AttributeAction.DELETE
};
                    updates[AttributeName.APPLIED.ToString()] = new AttributeValueUpdate {
Action = AttributeAction.DELETE
};
                    updates[AttributeName.DATE.ToString()] = new AttributeValueUpdate {
Action = AttributeAction.DELETE
};

                    UpdateItemRequest update = new UpdateItemRequest {
TableName = request.TableName,
.withKey(request.getKey(txManager))AttributeUpdates = updates,
Expected = expected
};
                    await txManager.Client.UpdateItemAsync(update);
                }
                else if (request is Request.DeleteItem)
                {
                    DeleteItemRequest delete = new DeleteItemRequest {
TableName = request.TableName,
.withKey(request.getKey(txManager))Expected = expected
};
                    await txManager.Client.DeleteItemAsync(delete);
                }
                else if (request is Request.GetItem)
                {
                    releaseReadLock(request.TableName, request.getKey(txManager));
                }
                else
                {
                    throw new TransactionAssertionException(txId, "Unknown request type: " + request.GetType());
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
        /// it gets deleted on rollback.
        /// </summary>
        protected internal virtual void doRollback()
        {
            txAssert(TransactionItem.State.ROLLED_BACK.Equals(txItem.getState()), txId, "Transaction state is not " + TransactionItem.State.ROLLED_BACK, "state", txItem.getState(), "txItem", txItem);

            foreach (Request request in txItem.Requests)
            {
                // Unlike unlockItems(), the order is important here.

                // 1. Apply the old item image over the one the request modified 
                rollbackItemAndReleaseLock(request);

                // 2. Delete the old item image, we don't need it anymore
                txItem.deleteItemImage(request.Rid);
            }

            complete(TransactionItem.State.ROLLED_BACK);
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
        protected internal virtual void rollbackItemAndReleaseLock(Request request)
        {
            rollbackItemAndReleaseLock(request.TableName, request.getKey(txManager), request is Request.GetItem, request.Rid);
        }

        protected internal virtual void rollbackItemAndReleaseLock(string tableName, Dictionary<string, AttributeValue> key, bool? isGet, int? rid)
        {
            // TODO there seems to be a race that leads to orphaned old item images (but is still correct in terms of the transaction)
            // A previous master could have stalled after writing the tx record, fall asleep, and then finally insert the old item image 
            // after this deleteAsync attempt goes through, and then the sleepy master crashes. There's no great way around this, 
            // so a sweeper needs to deal with it.

            // Possible outcomes:
            // 1) We know for sure from just the request (getItem) that we never back up the item. Release the lock (and deleteAsync if transient)
            // 2) We found a backup.  Apply the backup.
            // 3) We didn't find a backup. Try deleting the item with expected: 1) Transient, 2) Locked by us, return success
            // 4) Read the item. If we don't have the lock anymore, meaning it was already rolled back.  Return.
            // 5) We failed to take the backup, but should have.  
            //   a) If we've applied, assert.  
            //   b) Otherwise release the lock (okay to deleteAsync if transient to re-use logic)

            // 1. Read locks don't have a saved item image, so just unlock them and return
            if (isGet != null && isGet)
            {
                releaseReadLock(tableName, key);
                return;
            }

            // Read the old item image, if the rid is known.  Otherwise we treat it as if we don't have an item image.
            Dictionary<string, AttributeValue> itemImage = null;
            if (rid != null)
            {
                itemImage = txItem.loadItemImage(rid);
            }

            if (itemImage != null)
            {
                // 2. Found a backup.  Replace the current item with the pre-changes version of the item, at the same time removing the lock attributes
                txAssert(itemImage.Remove(AttributeName.TRANSIENT.ToString()) == null, txId, "Didn't expect to have saved an item image for a transient item", "itemImage", itemImage);

                itemImage.Remove(AttributeName.TXID.ToString());
                itemImage.Remove(AttributeName.DATE.ToString());

                txAssert(!itemImage.ContainsKey(AttributeName.APPLIED.ToString()), txId, "Old item image should not have contained the attribute " + AttributeName.APPLIED.ToString(), "itemImage", itemImage);

                try
                {
                    Dictionary<string, ExpectedAttributeValue> expected = new Dictionary<string, ExpectedAttributeValue>();
                    expected[AttributeName.TXID.ToString()] = new ExpectedAttributeValue {
Value = new AttributeValue(txId)
};

                    PutItemRequest put = new PutItemRequest {
TableName = tableName,
Item = itemImage,
Expected = expected
};
                    txManager.Client.putItem(put);
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
                    expected[AttributeName.TXID.ToString()] = new ExpectedAttributeValue {
Value = new AttributeValue(txId)
};
                    expected[AttributeName.TRANSIENT.ToString()] = new ExpectedAttributeValue {
Value = new AttributeValue(BOOLEAN_TRUE_ATTR_VAL)
};

                    DeleteItemRequest delete = new DeleteItemRequest {
TableName = tableName,
Key = key,
Expected = expected
};
                    txManager.Client.deleteItem(delete);
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
                //   b) Otherwise release the lock (okay to deleteAsync if transient to re-use logic)

                // 4) Read the item. If we don't have the lock anymore, meaning it was already rolled back.  Return.
                Dictionary<string, AttributeValue> item = getItem(tableName, key);

                if (item == null || !txId.Equals(getOwner(item)))
                {
                    // 3a) We don't have the lock anymore.  Return.
                    return;
                }

                // 5) We failed to take the backup, but should have.  
                //   a) If we've applied, assert.
                txAssert(!item.ContainsKey(AttributeName.APPLIED.ToString()), txId, "Applied change to item but didn't save a backup", "table", tableName, "key", key, "item" + item);

                //   b) Otherwise release the lock (okay to deleteAsync if transient to re-use logic)
                releaseReadLock(tableName, key);
            }
        }

        /// <summary>
        /// Unlocks an item without applying the previous item image on top of it.  This will deleteAsync the item if it 
        /// was marked as phantom.  
        /// 
        /// This is ONLY valid for releasing a read lock (either during rollback or post-commitAsync) 
        ///  OR releasing a lock where the change wasn't applied yet.
        /// </summary>
        /// <param name="tableName"> </param>
        /// <param name="key"> </param>
        protected internal virtual void releaseReadLock(string tableName, Dictionary<string, AttributeValue> key)
        {
            releaseReadLock(txId, txManager, tableName, key);
        }

        protected internal static void releaseReadLock(string txId, TransactionManager txManager, string tableName, Dictionary<string, AttributeValue> key)
        {
            Dictionary<string, ExpectedAttributeValue> expected = new Dictionary<string, ExpectedAttributeValue>();
            expected[AttributeName.TXID.ToString()] = new ExpectedAttributeValue {
Value = new AttributeValue(txId)
};
            expected[AttributeName.TRANSIENT.ToString()] = new ExpectedAttributeValue {
Exists = false
};
            expected[AttributeName.APPLIED.ToString()] = new ExpectedAttributeValue {
Exists = false
};

            try
            {
                Dictionary<string, AttributeValueUpdate> updates = new Dictionary<string, AttributeValueUpdate>(1);
                updates[AttributeName.TXID.ToString()] = new AttributeValueUpdate {
Action = AttributeAction.DELETE
};
                updates[AttributeName.DATE.ToString()] = new AttributeValueUpdate {
Action = AttributeAction.DELETE
};

                UpdateItemRequest update = new UpdateItemRequest {
TableName = tableName,
AttributeUpdates = updates,
Key = key,
Expected = expected
};
                txManager.Client.updateItem(update);
            }
            catch (ConditionalCheckFailedException)
            {
                try
                {
                    expected[AttributeName.TRANSIENT.ToString()] = (new ExpectedAttributeValue()).withValue(new AttributeValue {
S = BOOLEAN_TRUE_ATTR_VAL,

});

                    DeleteItemRequest delete = new DeleteItemRequest {
TableName = tableName,
Key = key,
Expected = expected
};
                    txManager.Client.deleteItem(delete);
                }
                catch (ConditionalCheckFailedException)
                {
                    // Ignore, means it was definitely rolled back
                    // Re-read to ensure that it wasn't applied
                    Dictionary<string, AttributeValue> item = getItem(txManager, tableName, key);
                    txAssert(!(item != null && txId.Equals(getOwner(item)) && item.ContainsKey(AttributeName.APPLIED.ToString())), "Item should not have been applied.  Unable to release lock", "item", item);
                }
            }
        }

        /// <summary>
        /// Unlocks an item and leaves it in an unknown state, as long as there is no associated transaction record
        /// </summary>
        /// <param name="txManager"> </param>
        /// <param name="tableName"> </param>
        /// <param name="item"> </param>
        protected internal static void unlockItemUnsafe(TransactionManager txManager, string tableName, Dictionary<string, AttributeValue> item, string txId)
        {
// 1) Ensure the transaction does not exist 
            try
            {
                Transaction tx = new Transaction(txId, txManager, false);
                throw new TransactionException(txId, "The transaction item should not have existed, but it did.  You can only unsafely unlock an item without a tx record. txItem: " + tx.txItem);
            }
            catch (TransactionNotFoundException)
            {
                // Expected to not exist
            }


            // 2) Remove all transaction attributes and condition on txId equality
            Dictionary<string, ExpectedAttributeValue> expected = new Dictionary<string, ExpectedAttributeValue>();
            expected[AttributeName.TXID.ToString()] = new ExpectedAttributeValue {
Value = new AttributeValue(txId)
};

            Dictionary<string, AttributeValueUpdate> updates = new Dictionary<string, AttributeValueUpdate>(1);
            foreach (string attrName in SPECIAL_ATTR_NAMES)
            {
                updates[attrName] = new AttributeValueUpdate {
Action = AttributeAction.DELETE
};
            }

            Dictionary<string, AttributeValue> key = Request.getKeyFromItem(tableName, item, txManager);

            UpdateItemRequest update = new UpdateItemRequest {
TableName = tableName,
AttributeUpdates = updates,
Key = key,
Expected = expected
};

            // Delete the item, and ignore conditional write failures
            try
            {
                txManager.Client.updateItem(update);
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
        /// <returns> the applied item image, or null if the apply was a deleteAsync. </returns>
        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: protected java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> addRequest(Request callerRequest, boolean isRedrive, int numAttempts) throws com.amazonaws.services.dynamodbv2.transactions.exceptions.DuplicateRequestException, com.amazonaws.services.dynamodbv2.transactions.exceptions.ItemNotLockedException, com.amazonaws.services.dynamodbv2.transactions.exceptions.TransactionCompletedException, com.amazonaws.services.dynamodbv2.transactions.exceptions.TransactionNotFoundException, com.amazonaws.services.dynamodbv2.transactions.exceptions.TransactionException
        protected internal virtual Dictionary<string, AttributeValue> addRequest(Request callerRequest, bool isRedrive, int numAttempts)
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
                    verifyLocks();
                    try
                    {
                        txItem.addRequest(callerRequest);
                        success = true;
                        break;
                    }
                    catch (ConditionalCheckFailedException)
                    {
                        // The transaction is either not in PENDING anymore, or the version number incremented from another thread/process
                        // registering a transaction (or we started cold on an existing transaction).

                        txItem = new TransactionItem(txId, txManager, false);

                        if (TransactionItem.State.COMMITTED.Equals(txItem.getState()))
                        {
                            throw new TransactionCommittedException(txId, "Attempted to add a request to a transaction that was not in state " + TransactionItem.State.PENDING + ", state is " + txItem.getState());
                        }
                        else if (TransactionItem.State.ROLLED_BACK.Equals(txItem.getState()))
                        {
                            throw new TransactionRolledBackException(txId, "Attempted to add a request to a transaction that was not in state " + TransactionItem.State.PENDING + ", state is " + txItem.getState());
                        }
                        else if (!TransactionItem.State.PENDING.Equals(txItem.getState()))
                        {
                            throw new UnknownCompletedTransactionException(txId, "Attempted to add a request to a transaction that was not in state " + TransactionItem.State.PENDING + ", state is " + txItem.getState());
                        }
                    }
                }

                if (!success)
                {
                    throw new TransactionException(txId, "Unable to add request to transaction - too much contention for the tx record");
                }
            }
            else
            {
                txAssert(TransactionItem.State.PENDING.Equals(txItem.getState()), txId, "Attempted to add a request to a transaction that was not in state " + TransactionItem.State.PENDING, "state", txItem.getState());
            }

            // 2. Write txId to item
            Dictionary<string, AttributeValue> item = lockItem(callerRequest, true, ITEM_LOCK_ACQUIRE_ATTEMPTS);

            //    As long as this wasn't a duplicate read request,
            // 3. Save the item image to a new item in case we need to roll back, unless:
            //    - it's a lock request,
            //    - we've already saved the item image
            //    - the item is transient (inserted for acquiring the lock)
            saveItemImage(callerRequest, item);

            // 3a. Re-read the transaction item to make sure it hasn't been rolled back or completed.
            //     Can be optimized if we know the transaction is already completed(
            try
            {
                txItem = new TransactionItem(txId, txManager, false);
            }
            catch (TransactionNotFoundException e)
            {
                releaseReadLock(callerRequest.TableName, callerRequest.getKey(txManager));
                throw e;
            }
            switch (txItem.getState())
            {
                case COMMITTED:
                    doCommitAsync();
                    throw new TransactionCommittedException(txId, "The transaction already committed");
                case ROLLED_BACK:
                    doRollback();
                    throw new TransactionRolledBackException(txId, "The transaction already rolled back");
                case PENDING:
                    break;
                default:
                    throw new TransactionException(txId, "Unexpected state " + txItem.getState());
            }

            // 4. Apply change to item, keeping lock on the item, returning the attributes according to RETURN_VALUE
            //    If we are a read request, and there is an applied deleteAsync request for the same item in the tx, return null.
            Dictionary<string, AttributeValue> returnItem = applyAndKeepLock(callerRequest, item);

            // 5. Optimization: Keep track of the requests that this transaction object has fully applied
            if (callerRequest.Rid != null)
            {
                fullyAppliedRequests.Add(callerRequest.Rid);
            }

            return returnItem;
        }

        protected internal virtual void saveItemImage(Request callerRequest, Dictionary<string, AttributeValue> item)
        {
            if (isRequestSaveable(callerRequest, item) && !item.ContainsKey(AttributeName.APPLIED.ToString()))
            {
                txItem.saveItemImage(item, callerRequest.Rid);
            }
        }

        protected internal virtual bool isRequestSaveable(Request callerRequest, Dictionary<string, AttributeValue> item)
        {
            if (!(callerRequest is Request.GetItem) && !item.ContainsKey(AttributeName.TRANSIENT.ToString()))
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
        //ORIGINAL LINE: protected java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> lockItem(Request callerRequest, boolean expectExists, int attempts) throws com.amazonaws.services.dynamodbv2.transactions.exceptions.ItemNotLockedException, com.amazonaws.services.dynamodbv2.transactions.exceptions.TransactionException
        protected internal virtual Dictionary<string, AttributeValue> lockItem(Request callerRequest, bool expectExists, int attempts)
        {
            Dictionary<string, AttributeValue> key = callerRequest.getKey(txManager);

            if (attempts <= 0)
            {
                throw new TransactionException(txId, "Unable to acquire item lock for item " + key); // This won't trigger a rollback, it's really just a case of contention and needs more redriving
            }

            // Create Expected and Updates maps.  
            //   - If we expect the item TO exist, we only update the lock
            //   - If we expect the item NOT to exist, we update both the transient attribute and the lock.
            // In both cases we expect the txid not to be set
            Dictionary<string, AttributeValueUpdate> updates = new Dictionary<string, AttributeValueUpdate>();
            updates[AttributeName.TXID.ToString()] = new AttributeValueUpdate {
Action = AttributeAction.PUT,
Value = new AttributeValue(txId)
};
            updates[AttributeName.DATE.ToString()] = new AttributeValueUpdate {
Action = AttributeAction.PUT,
Value = txManager.CurrentTimeAttribute
};

            Dictionary<string, ExpectedAttributeValue> expected;
            if (expectExists)
            {
                expected = callerRequest.getExpectExists(txManager);
                expected[AttributeName.TXID.ToString()] = new ExpectedAttributeValue {
Exists = false
};
            }
            else
            {
                expected = new Dictionary<string, ExpectedAttributeValue>(1);
                updates.put(AttributeName.TRANSIENT.ToString(), new AttributeValueUpdate {
Action = AttributeAction.PUT,

}.withValue(new AttributeValue {
S = BOOLEAN_TRUE_ATTR_VAL,

}));
            }
            expected[AttributeName.TXID.ToString()] = new ExpectedAttributeValue {
Exists = false
};

            // Do a conditional update on NO transaction id, and that the item DOES exist
            UpdateItemRequest updateRequest = new UpdateItemRequest {
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
                item = txManager.Client.updateItem(updateRequest).Attributes;
                owner = getOwner(item);
            }
            catch (ConditionalCheckFailedException)
            {
                // If the check failed, it means there is either:
                //   1) a different transaction currently locking the item
                //   2) this transaction already is attempting to lock the item.  
                //   3) the item does not exist
                // Get the item and see which is the case
                item = getItem(callerRequest.TableName, key);
                if (item == null)
                {
                    nextExpectExists = false;
                }
                else
                {
                    nextExpectExists = true;
                    owner = getOwner(item);
                }
            }

            // Try the write again if the item is unowned (but not if it is owned)
            if (!string.ReferenceEquals(owner, null))
            {
                if (txId.Equals(owner))
                {
                    return item;
                }
                // For now, always roll back / complete the other transaction in the case of a conflict.
                if (attempts > 1)
                {
                    try
                    {
                        Transaction otherTransaction = txManager.resumeTransaction(owner);
                        otherTransaction.rollback();
                    }
                    catch (TransactionCompletedException)
                    {
                        // no-op
                    }
                    catch (TransactionNotFoundException)
                    {
                        releaseReadLock(owner, txManager, callerRequest.TableName, key);
                    }
                }
                else
                {
                    throw new ItemNotLockedException(txId, owner, callerRequest.TableName, key);
                }
            }
            return lockItem(callerRequest, nextExpectExists, attempts - 1);
        }


        /// <summary>
        /// Writes the request to the user table and keeps the lock, as long as we still have the lock.
        /// Ensures that the write happens (at most) once, because the write atomically marks the item as applied.
        /// 
        /// This is a no-op for DeleteItem or LockItem requests, since for deleteAsync the item isn't removed until after
        /// the transaction commits, and lock doesn't mutate the item.
        /// 
        /// Note that this method mutates the item and the request.
        /// </summary>
        /// <param name="request"> </param>
        /// <param name="lockedItem"> </param>
        /// <returns> the copy of the item, as requested in ReturnValues of the request (or the new item in the case of a read), or null if this is a redrive  </returns>
        protected internal virtual Dictionary<string, AttributeValue> applyAndKeepLock(Request request, Dictionary<string, AttributeValue> lockedItem)
        {
            Dictionary<string, AttributeValue> returnItem = null;

            // 1. Remember what return values the caller wanted.
            string returnValues = request.ReturnValues; // save the returnValues because we will mutate it
            if (string.ReferenceEquals(returnValues, null))
            {
                returnValues = "NONE";
            }

            // 3. No-op if the locked item shows it was already applied.
            if (!lockedItem.ContainsKey(AttributeName.APPLIED.ToString()))
            {
                try
                {
                    Dictionary<string, ExpectedAttributeValue> expected = new Dictionary<string, ExpectedAttributeValue>();
                    expected[AttributeName.TXID.ToString()] = new ExpectedAttributeValue {
Value = new AttributeValue(txId)
};
                    expected[AttributeName.APPLIED.ToString()] = new ExpectedAttributeValue {
Exists = false
};

                    // TODO assert if the caller request contains any of our internally defined fields?
                    //      but we aren't copying the request object, so our retries might trigger the assertion.
                    //      at least could assert that they have the values that we want.
                    if (request is Request.PutItem)
                    {
                        PutItemRequest put = ((Request.PutItem)request).Request;
                        // Add the lock id and "is transient" flags to the put request (put replaces) 
                        put.Item.put(AttributeName.TXID.ToString(), new AttributeValue(txId));
                        put.Item.put(AttributeName.APPLIED.ToString(), new AttributeValue(BOOLEAN_TRUE_ATTR_VAL));
                        if (lockedItem.ContainsKey(AttributeName.TRANSIENT.ToString()))
                        {
                            put.Item.put(AttributeName.TRANSIENT.ToString(), lockedItem[AttributeName.TRANSIENT.ToString()]);
                        }
                        put.Item.put(AttributeName.DATE.ToString(), lockedItem[AttributeName.DATE.ToString()]);
                        put.Expected = expected;
                        put.ReturnValues = returnValues;
                        returnItem = txManager.Client.putItem(put).Attributes;
                    }
                    else if (request is Request.UpdateItem)
                    {
                        UpdateItemRequest update = ((Request.UpdateItem)request).Request;
                        update.Expected = expected;
                        update.ReturnValues = returnValues;

                        if (update.AttributeUpdates != null)
                        {
                            // Defensively deleteAsync the attributes in the request that could interfere with the transaction
                            update.AttributeUpdates.remove(AttributeName.TXID.ToString());
                            update.AttributeUpdates.remove(AttributeName.TRANSIENT.ToString());
                            update.AttributeUpdates.remove(AttributeName.DATE.ToString());
                        }
                        else
                        {
                            update.AttributeUpdates = new Dictionary<string, AttributeValueUpdate>(1);
                        }

                        update.AttributeUpdates.put(AttributeName.APPLIED.ToString(), new AttributeValueUpdate {
Action = AttributeAction.PUT,
Value = new AttributeValue(BOOLEAN_TRUE_ATTR_VAL),

});

                        returnItem = txManager.Client.updateItem(update).Attributes;
                    }
                    else if (request is Request.DeleteItem)
                    {
                        // no-op - deleteAsync doesn't change the item until unlock post-commitAsync
                    }
                    else if (request is Request.GetItem)
                    {
                        // no-op
                    }
                    else
                    {
                        throw new TransactionAssertionException(txId, "Request may not be null");
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
            if ("ALL_OLD".Equals(returnValues) && isTransient(lockedItem))
            {
                return null;
            }
            else if (request is Request.GetItem)
            {
                GetItemRequest getRequest = ((Request.GetItem)request).Request;
                Request lockingRequest = txItem.getRequestForKey(request.TableName, request.getKey(txManager));
                if (lockingRequest is Request.DeleteItem)
                {
                    return null; // If the item we're getting is deleted in this transaction
                }
                else if (lockingRequest is Request.GetItem && isTransient(lockedItem))
                {
                    return null; // If the item has only a read lock and is transient
                }
                else if (getRequest.AttributesToGet != null)
                {
                    // Remove attributes that weren't asked for in the request
                    ISet<string> attributesToGet = new HashSet<string>(getRequest.AttributesToGet);
                    IEnumerator<KeyValuePair<string, AttributeValue>> it = lockedItem.SetOfKeyValuePairs().GetEnumerator();
                    while (it.MoveNext())
                    {
                        KeyValuePair<string, AttributeValue> attr = it.Current;
                        if (!attributesToGet.Contains(attr.Key))
                        {
                            //JAVA TO C# CONVERTER TODO TASK: .NET enumerators are read-only:
                            it.remove(); // TODO does this need to keep the tx attributes?
                        }
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
                returnItem = txItem.loadItemImage(request.Rid);
                if (returnItem == null)
                {
                    throw new UnknownCompletedTransactionException(txId, "Transaction must have completed since the old copy of the image is missing");
                }
                return returnItem;
            }
            else if ("ALL_NEW".Equals(returnValues))
            {
                if (returnItem != null)
                {
                    return returnItem; // If the apply write succeeded, we have the ALL_NEW from the request
                }
                returnItem = getItem(request.TableName, request.getKey(txManager));
                if (returnItem == null)
                {
                    throw new UnknownCompletedTransactionException(txId, "Transaction must have completed since the item no longer exists");
                }
                string owner = getOwner(returnItem);
                if (!txId.Equals(owner))
                {
                    throw new ItemNotLockedException(txId, owner, request.TableName, returnItem);
                }
                return returnItem;
            }
            else if ("NONE".Equals(returnValues))
            {
                return null;
            }
            else
            {
                throw new TransactionAssertionException(txId, "Unsupported return values: " + returnValues);
            }
        }

        /// <summary>
        /// Returns a copy of the requested item all attributes retrieved.  Performs a consistent read.
        /// </summary>
        /// <param name="tableName"> </param>
        /// <param name="key"> </param>
        /// <returns> the item map, with all attributes fetched </returns>
        protected internal virtual Dictionary<string, AttributeValue> getItem(string tableName, Dictionary<string, AttributeValue> key)
        {
            return getItem(txManager, tableName, key);
        }

        protected internal static Dictionary<string, AttributeValue> getItem(TransactionManager txManager, string tableName, Dictionary<string, AttributeValue> key)
        {
            GetItemRequest getRequest = new GetItemRequest {
TableName = tableName,
ConsistentRead = true,
Key = key
};
            GetItemResponse getResult = txManager.Client.getItem(getRequest);
            return getResult.Item;
        }

        /// <summary>
        /// Determines the current lock holder for the given item
        /// </summary>
        /// <param name="item"> must not be null </param>
        /// <returns> the owning transaction id, or null if the item isn't locked </returns>
        protected internal static string getOwner(Dictionary<string, AttributeValue> item)
        {
            if (item == null)
            {
                throw new System.ArgumentException();
            }
            AttributeValue itemTxId = item[AttributeName.TXID.ToString()];
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
                return txItem;
            }
        }

        public sealed class AttributeName
        {
public static readonly AttributeName TXID = new AttributeName("TXID", InnerEnum.TXID, TX_ATTR_PREFIX + "Id");
            public static readonly AttributeName TRANSIENT = new AttributeName("TRANSIENT", InnerEnum.TRANSIENT, TX_ATTR_PREFIX + "T");
            public static readonly AttributeName DATE = new AttributeName("DATE", InnerEnum.DATE, TX_ATTR_PREFIX + "D");
            public static readonly AttributeName APPLIED = new AttributeName("APPLIED", InnerEnum.APPLIED, TX_ATTR_PREFIX + "A");
            public static readonly AttributeName REQUESTS = new AttributeName("REQUESTS", InnerEnum.REQUESTS, TX_ATTR_PREFIX + "R");
            public static readonly AttributeName STATE = new AttributeName("STATE", InnerEnum.STATE, TX_ATTR_PREFIX + "S");
            public static readonly AttributeName VERSION = new AttributeName("VERSION", InnerEnum.VERSION, TX_ATTR_PREFIX + "V");
            public static readonly AttributeName FINALIZED = new AttributeName("FINALIZED", InnerEnum.FINALIZED, TX_ATTR_PREFIX + "F");
            public static readonly AttributeName IMAGE_ID = new AttributeName("IMAGE_ID", InnerEnum.IMAGE_ID, TX_ATTR_PREFIX + "I");

            private static readonly List<AttributeName> valueList = new List<AttributeName>();

            static AttributeName()
            {
                valueList.Add(TXID);
                valueList.Add(TRANSIENT);
                valueList.Add(DATE);
                valueList.Add(APPLIED);
                valueList.Add(REQUESTS);
                valueList.Add(STATE);
                valueList.Add(VERSION);
                valueList.Add(FINALIZED);
                valueList.Add(IMAGE_ID);
            }

            public enum InnerEnum
            {
                TXID,
                TRANSIENT,
                DATE,
                APPLIED,
                REQUESTS,
                STATE,
                VERSION,
                FINALIZED,
                IMAGE_ID
            }

            private readonly string nameValue;
            private readonly int ordinalValue;
            private readonly InnerEnum innerEnumValue;
            private static int nextOrdinal = 0;

            internal AttributeName(string name, InnerEnum innerEnum, string value)
            {
                this.value = value;

                nameValue = name;
                ordinalValue = nextOrdinal++;
                innerEnumValue = innerEnum;
            }

            internal readonly string value;

            public override string ToString()
            {
                return value;
            }

            public static List<AttributeName> values()
            {
                return valueList;
            }

            public InnerEnum InnerEnumValue()
            {
                return innerEnumValue;
            }

            public int ordinal()
            {
                return ordinalValue;
            }

            public static AttributeName valueOf(string name)
            {
                foreach (AttributeName enumInstance in AttributeName.values())
                {
                    if (enumInstance.nameValue == name)
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
        //JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
        //ORIGINAL LINE: public <T> void deleteAsync(final T item)
        public virtual void deleteAsync<T>(T item)
        {
            dMapper = new CallableAnonymousInnerClass(this, item),
;
        }

        private class CallableAnonymousInnerClass : Callable<Void>
        {
            private readonly Transaction outerInstance;

            private T item;

            public CallableAnonymousInnerClass(Transaction outerInstance, T item)
            {
                this.outerInstance = outerInstance;
                this.item = item;
            }

            //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
            //ORIGINAL LINE: @Override public Void call() throws Exception
            public override Void call()
            {
                outerInstance.txManager.ClientMapper.delete(item);
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
        //JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
        //ORIGINAL LINE: public <T> T load(final T item)
        public virtual T load<T>(T item)
        {
            return dMapper = new CallableAnonymousInnerClass2(this, item),
;
        }

        private class CallableAnonymousInnerClass2 : Callable<T>
        {
            private readonly Transaction outerInstance;

            private T item;

            public CallableAnonymousInnerClass2(Transaction outerInstance, T item)
            {
                this.outerInstance = outerInstance;
                this.item = item;
            }

            //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
            //ORIGINAL LINE: @Override public T call() throws Exception
            public override T call()
            {
                return outerInstance.txManager.ClientMapper.load(item);
            }
        }

        /// <summary>
        /// Save an item using the mapper.
        /// </summary>
        /// <param name="item">
        ///            An item object with key attributes populated. </param>
        //JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
        //ORIGINAL LINE: public <T> void save(final T item)
        public virtual void save<T>(T item)
        {
            dMapper = new CallableAnonymousInnerClass3(this, item),
;
        }

        private class CallableAnonymousInnerClass3 : Callable<Void>
        {
            private readonly Transaction outerInstance;

            private T item;

            public CallableAnonymousInnerClass3(Transaction outerInstance, T item)
            {
                this.outerInstance = outerInstance;
                this.item = item;
            }

            //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
            //ORIGINAL LINE: @Override public Void call() throws Exception
            public override Void call()
            {
                outerInstance.txManager.ClientMapper.save(item);
                return null;
            }
        }

        private T doWithMapper<T>(Callable<T> callable)
        {
            try
            {
                txManager.FacadeProxy.Backend = new TransactionDynamoDBFacade(this, txManager);
                return callable.call();
            }
            catch (Exception e)
            {
                // have to do this here in order to avoid having to declare a checked exception type
                throw e;
            }
            catch (Exception e)
            {
                // none of the callers of this method need to throw a checked exception
                throw new Exception(e);
            }
            finally
            {
                txManager.FacadeProxy.Backend = null;
            }
        }

    }

}