using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using com.amazonaws.services.dynamodbv2.transactions.exceptions;
using com.amazonaws.services.dynamodbv2.util;

using static com.amazonaws.services.dynamodbv2.transactions.exceptions.TransactionAssertionException;

// <summary>
// Copyright 2013-2013 Amazon.com, Inc. or its affiliates. All Rights Reserved.
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
//JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to C#:
//	import static com.amazonaws.services.dynamodbv2.transactions.Transaction.AttributeName;
//JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to C#:
//	import static com.amazonaws.services.dynamodbv2.transactions.exceptions.TransactionAssertionException.txAssert;

	/// <summary>
	/// Contains an image of the transaction item in DynamoDB, and methods to change that item.
	/// 
	/// If any of those attempts to change the transaction fail, the item needs to be thrown away, re-fetched,
	/// and the change applied via the new item.
	/// </summary>
	public class TransactionItem
	{
/* Transaction states */
		private const string StatePending = "P";
		private const string StateCommitted = "C";
		private const string StateRolledBack = "R";

		protected internal readonly string TxId;
		private readonly TransactionManager _txManager;
		private Dictionary<string, AttributeValue> _txItem;
		private int _version;
		private readonly Dictionary<string, AttributeValue> _txKey;
		private readonly Dictionary<string, Dictionary<ImmutableKey, Request>> _requestsMap = new Dictionary<string, Dictionary<ImmutableKey, Request>>();

        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);

        /*
		 * Constructors and initializers
		 */

        /// <summary>
        /// Inserts or retrieves a transaction record. 
        /// </summary>
        /// <param name="txId"> the id of the transaction to insert or retrieve </param>
        /// <param name="txManager"> </param>
        /// <param name="insert"> whether to insert the transaction (it's a new transaction) or to loadAsync an existing one </param>
        /// <exception cref="TransactionNotFoundException"> If it is being retrieved and it is not found </exception>
        public TransactionItem(string txId, TransactionManager txManager, bool insert) : this(txId, txManager, insert, null)
        {
        }

        public TransactionItem(Dictionary<string, AttributeValue> txItem, TransactionManager txManager) : this(null, txManager, false, txItem)
        {
        }

        //
        // This constructor does too much work! Fix by moving into factory method (part done) TODO
        //

        /// <summary>
        /// Either inserts a new transaction, reads it from the database, or initializes from a previously read transaction item.
        /// </summary>
        /// <param name="txId"> </param>
        /// <param name="txManager"> </param>
        /// <param name="insert"> </param>
        /// <param name="txItem"> A previously read transaction item (must include all of the attributes from the item). May not be specified with txId. </param>
        /// <exception cref="TransactionNotFoundException"> </exception>
        protected internal TransactionItem(string txId, TransactionManager txManager, bool insert, Dictionary<string, AttributeValue> txItem)
        {
            this._txManager = txManager;

            // Initialize txId, txKey, and txItem
            if (!string.ReferenceEquals(txId, null))
            {
                // Validate mutual exclusivity of inputs
                if (txItem != null)
                {
                    throw new TransactionException(txId, "When providing txId, txItem must be null");
                }
                this.TxId = txId;
                Dictionary<string, AttributeValue> txKeyMap = new Dictionary<string, AttributeValue>(1);
                txKeyMap[Transaction.AttributeName.Txid.ToString()] = new AttributeValue(txId);
                this._txKey = new Dictionary<string, AttributeValue>(txKeyMap);
                if (insert)
                {
                    this._txItem = this.Insert();
                }
                else
                {
                    this._txItem = GetAsync().Result;
                    if (this._txItem == null)
                    {
                        throw new TransactionNotFoundException(this.TxId);
                    }
                }
            }
            else if (txItem != null)
            {
                // Validate mutual exclusivity of inputs
                if (insert)
                {
                    throw new TransactionException(txId, "When providing a txItem, insert must be false");
                }
                this._txItem = txItem;
                if (!IsTransactionItem(txItem))
                {
                    throw new TransactionException(txId, "txItem is not a transaction item");
                }
                this.TxId = txItem[Transaction.AttributeName.Txid.ToString()].S;
                Dictionary<string, AttributeValue> txKeyMap = new Dictionary<string, AttributeValue>(1);
                txKeyMap[Transaction.AttributeName.Txid.ToString()] = new AttributeValue(this.TxId);
                this._txKey = new Dictionary<string, AttributeValue>(txKeyMap);
            }
            else
            {
                throw new TransactionException(null, "Either txId or txItem must be specified");
            }

            // Initialize the version
            AttributeValue txVersionVal = this._txItem[Transaction.AttributeName.Version.ToString()];
            if (txVersionVal == null || txVersionVal.N == null)
            {
                throw new TransactionException(this.TxId, "Version number is not present in TX record");
            }
            _version = int.Parse(txVersionVal.N);

            // Build the requests structure
            LoadRequestsAsync().Wait();
        }



        private TransactionItem(TransactionManager txManager, string txId, Dictionary<string, AttributeValue> txKey)
        {
            this._txManager = txManager;
            this.TxId = txId;
            this._txKey = txKey;
        }

	    public TransactionItem() // for tests
	    {
	        
	    }

        /// <summary>
        /// Either inserts a new transaction, reads it from the database, or initializes from a previously read transaction item.
        /// </summary>
        /// <param name="txId"> </param>
        /// <param name="txManager"> </param>
        /// <param name="insert"> </param>
        /// <param name="txItem"> A previously read transaction item (must include all of the attributes from the item). May not be specified with txId. </param>
        /// <exception cref="TransactionNotFoundException"> </exception>
        protected internal static async Task<TransactionItem> CreateAsync(string txId, TransactionManager txManager, bool insert, Dictionary<string, AttributeValue> txItem)
        {
            TransactionItem transactionItem;

            // Initialize txId, txKey, and txItem
            if (!string.ReferenceEquals(txId, null))
            {
                // Validate mutual exclusivity of inputs
                if (txItem != null)
                {
                    throw new TransactionException(txId, "When providing txId, txItem must be null");
                }
                Dictionary<string, AttributeValue> txKeyMap = new Dictionary<string, AttributeValue>(1);
                txKeyMap[Transaction.AttributeName.Txid.ToString()] = new AttributeValue(txId);
                var newTxKey = new Dictionary<string, AttributeValue>(txKeyMap);

                transactionItem = new TransactionItem(txManager, txId, newTxKey);

                if (insert)
                {
                    transactionItem._txItem = transactionItem.Insert();
                }
                else
                {
                    transactionItem._txItem = await GetTransactionItemAsync(txManager, newTxKey);
                    if (transactionItem._txItem == null)
                    {
                        throw new TransactionNotFoundException(transactionItem.TxId);
                    }
                }
            }
            else if (txItem != null)
            {
                // Validate mutual exclusivity of inputs
                if (insert)
                {
                    throw new TransactionException(txId, "When providing a txItem, insert must be false");
                }
                if (!IsTransactionItem(txItem))
                {
                    throw new TransactionException(txId, "txItem is not a transaction item");
                }

                var newTxId = txItem[Transaction.AttributeName.Txid.ToString()].S;
                Dictionary<string, AttributeValue> txKeyMap = new Dictionary<string, AttributeValue>(1);
                txKeyMap[Transaction.AttributeName.Txid.ToString()] = new AttributeValue(newTxId);
                var newTxKey = new Dictionary<string, AttributeValue>(txKeyMap);
                transactionItem = new TransactionItem(txManager, newTxId, newTxKey);
                transactionItem._txItem = txItem;
            }
            else
            {
                throw new TransactionException(null, "Either txId or txItem must be specified");
            }

            // Initialize the version
            AttributeValue txVersionVal = transactionItem._txItem[Transaction.AttributeName.Version.ToString()];
            if (txVersionVal == null || txVersionVal.N == null)
            {
                throw new TransactionException(transactionItem.TxId, "Version number is not present in TX record");
            }
            transactionItem._version = int.Parse(txVersionVal.N);

            // Build the requests structure
            await transactionItem.LoadRequestsAsync();
            return transactionItem;
        }

        /// <summary>
        /// Inserts a new transaction item into the table.  Assumes txKey is already initialized. </summary>
        /// <returns> the txItem </returns>
        /// <exception cref="TransactionException"> if the transaction already exists </exception>
        private Dictionary<string, AttributeValue> Insert()
		{
			Dictionary<string, AttributeValue> item = new Dictionary<string, AttributeValue>();
			item[Transaction.AttributeName.State.ToString()] = new AttributeValue(StatePending);
			item[Transaction.AttributeName.Version.ToString()] = (new AttributeValue {N = Convert.ToString(1)});
			item[Transaction.AttributeName.Date.ToString()] = _txManager.CurrentTimeAttribute;
            foreach(var keyValue in _txKey) item.Add(keyValue.Key, keyValue.Value);

			Dictionary<string, ExpectedAttributeValue> expectNotExists = new Dictionary<string, ExpectedAttributeValue>(2);
			expectNotExists[Transaction.AttributeName.Txid.ToString()] = new ExpectedAttributeValue(false);
			expectNotExists[Transaction.AttributeName.State.ToString()] = new ExpectedAttributeValue(false);

		    PutItemRequest request = new PutItemRequest
		    {
		        TableName = _txManager.TransactionTableName,
		        Item = item,
		        Expected = expectNotExists
		    };

			try
			{
				_txManager.Client.PutItemAsync(request);
				return item;
			}
			catch (ConditionalCheckFailedException e)
			{
				throw new TransactionException("Failed to create new transaction with id " + TxId, e);
			}
        }

        /// <summary>
        /// Fetches this transaction item from the tx table.  Uses consistent read.
        /// </summary>
        /// <returns> the latest copy of the transaction item, or null if it has been completed (and deleted)  </returns>
        private async Task<Dictionary<string, AttributeValue>> GetAsync()
        {
            return await GetTransactionItemAsync(_txManager, _txKey);
        }

        /// <summary>
        /// Fetches this transaction item from the tx table.  Uses consistent read.
        /// </summary>
        /// <returns> the latest copy of the transaction item, or null if it has been completed (and deleted)  </returns>
        private static async Task<Dictionary<string, AttributeValue>> GetTransactionItemAsync(TransactionManager txManager, Dictionary<string, AttributeValue> txKey)
        {
            GetItemRequest getRequest = new GetItemRequest
            {
                TableName = txManager.TransactionTableName,
                Key = txKey.ToDictionary(x => x.Key, x => x.Value),
                ConsistentRead = true
            };
            return (await txManager.Client.GetItemAsync(getRequest)).Item;
        }

        /// <summary>
        /// Gets the version of the transaction image currently loaded.  Useful for determining if the item has changed when committing the transaction.
        /// </summary>
        public virtual int Version
		{
			get
			{
				return _version;
			}
		}

		/// <summary>
		/// Determines whether the record is a valid transaction item.  Useful because backups of items in the transaction are 
		/// saved in the same table as the transaction item.
		/// </summary>
		/// <param name="txItem">
		/// @return </param>
		public static bool IsTransactionItem(Dictionary<string, AttributeValue> txItem)
		{
			if (txItem == null)
			{
				throw new TransactionException(null, "txItem must not be null");
			}

			if (!txItem.ContainsKey(Transaction.AttributeName.Txid.ToString()))
			{
				return false;
			}

			if (txItem[Transaction.AttributeName.Txid.ToString()].S == null)
			{
				return false;
			}

			return true;
		}

		public virtual long LastUpdateTimeMillis
		{
			get
			{
				AttributeValue requestsVal = _txItem[Transaction.AttributeName.Date.ToString()];
				if (requestsVal == null || requestsVal.N == null)
				{
					throw new TransactionAssertionException(TxId, "Expected date attribute to be defined");
				}
    
				try
				{
					double date = double.Parse(requestsVal.N);
					return (long)(date * 1000.0);
				}
				catch (System.FormatException e)
				{
					throw new TransactionException("Excpected valid date attribute, was: " + requestsVal.N, e);
				}
			}
		}

		/*
		 * For adding to and maintaining the requests within the transactions
		 */

		/// <summary>
		/// Returns the requests in the tx item, sorted by table, then item primary key.  If a lock request was overwritten by 
		/// a write, or a lock happened after a write, that lock will not be returned in this list.  
		/// </summary>
		/// <param name="txItem">
		/// @return </param>
		public virtual List<Request> Requests
		{
			get
			{
				List<Request> requests = new List<Request>();
				foreach (KeyValuePair<string, Dictionary<ImmutableKey, Request>> tableRequests in _requestsMap.SetOfKeyValuePairs())
				{
					foreach (KeyValuePair<ImmutableKey, Request> keyRequests in tableRequests.Value)
					{
						requests.Add(keyRequests.Value);
					}
				}
				return requests;
			}
		}

		/// <summary>
		/// Returns the Request for this table and key, or null if that item is not in this transaction.
		/// </summary>
		/// <param name="tableName"> </param>
		/// <param name="key">
		/// @return </param>
		public virtual Request GetRequestForKey(string tableName, Dictionary<string, AttributeValue> key)
		{
			Dictionary<ImmutableKey, Request> tableRequests = _requestsMap[tableName];

			if (tableRequests != null)
			{
				Request request = tableRequests[new ImmutableKey(key)];
				if (request != null)
				{
					return request;
				}
			}

			return null;
		}

		/// <summary>
		/// Adds a request object (input param) to the transaction item.  Enforces that request are unique for a given table name and primary key.
		/// Doesn't let you do more than one write per item.  However you can upgrade a read lock to a write. </summary>
		/// <param name="callerRequest"> </param>
		/// <exception cref="ConditionalCheckFailedException"> if the tx item changed out from under us.  If you getAsync this you must throw this TransactionItem away. </exception>
		/// <exception cref="DuplicateRequestException"> If you getAsync this you do not need to throw away the item. </exception>
		/// <exception cref="InvalidRequestException"> If the request would add too much data to the transaction </exception>
		/// <returns> true if the request was added, false if it didn't need to be added (because it was a duplicate lock request) </returns>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public synchronized boolean addRequestAsync(Request callerRequest) throws com.amazonaws.services.dynamodbv2.model.ConditionalCheckFailedException, com.amazonaws.services.dynamodbv2.transactions.exceptions.DuplicateRequestException
		public virtual async Task<bool> AddRequestAsync(Request callerRequest)
		{
		    await _semaphore.WaitAsync();
		    try
		    {
		        // 1. Ensure the request is unique (modifies the internal data structure if it is unique)
		        //    However, do not not short circuit.  If we're doing a read in a resumed transaction, it's important to ensure we're returning
		        //    any writes that happened before. 
		        await AddRequestToMapAsync(callerRequest);

		        callerRequest.Rid = _version;

		        // 2. Write request to transaction item
		        MemoryStream requestBytes = Request.Serialize(TxId, callerRequest);
		        AttributeValueUpdate txItemUpdate = new AttributeValueUpdate
		        {
		            Action = AttributeAction.ADD,
		            Value = new AttributeValue
		            {
		                BS = new List<MemoryStream> {requestBytes}
		            }
		        };

		        Dictionary<string, AttributeValueUpdate> txItemUpdates = new Dictionary<string, AttributeValueUpdate>();
		        txItemUpdates[Transaction.AttributeName.Requests.ToString()] = txItemUpdate;
		        txItemUpdates[Transaction.AttributeName.Version.ToString()] = new AttributeValueUpdate
		        {
		            Action = AttributeAction.ADD,
		            Value = new AttributeValue
		            {
		                N = "1"
		            }
		        };
		        txItemUpdates[Transaction.AttributeName.Date.ToString()] = new AttributeValueUpdate
		        {
		            Action = AttributeAction.PUT,
		            Value = _txManager.CurrentTimeAttribute
		        };

		        Dictionary<string, ExpectedAttributeValue> expected = new Dictionary<string, ExpectedAttributeValue>();
		        expected[Transaction.AttributeName.State.ToString()] =
		            new ExpectedAttributeValue(new AttributeValue(StatePending));
		        expected[Transaction.AttributeName.Version.ToString()] = new ExpectedAttributeValue(new AttributeValue
		        {
		            N = Convert.ToString(_version)
		        });

		        UpdateItemRequest txItemUpdateRequest = new UpdateItemRequest
		        {
		            TableName = _txManager.TransactionTableName,
		            Key = _txKey,
		            Expected = expected,
		            ReturnValues = ReturnValue.ALL_NEW,
		            AttributeUpdates = txItemUpdates
		        };

		        try
		        {
		            _txItem = (await _txManager.Client.UpdateItemAsync(txItemUpdateRequest)).Attributes;
		            int newVersion = int.Parse(_txItem[Transaction.AttributeName.Version.ToString()].N);
		            TxAssert(newVersion == _version + 1, TxId, "Unexpected version number from update result");
		            _version = newVersion;
		        }
		        catch (AmazonServiceException e)
		        {
		            if ("ValidationException".Equals(e.ErrorCode))
		            {
		                await RemoveRequestFromMapAsync(callerRequest);
		                throw new InvalidRequestException(
		                    "The amount of data in the transaction cannot exceed the DynamoDB item size limit", TxId,
		                    callerRequest.TableName, await callerRequest.GetKeyAsync(_txManager), callerRequest);
		            }
		            else
		            {
		                throw;
		            }
		        }
		        return true;
		    }
		    finally
		    {
		        _semaphore.Release();
		    }
		}

		/// <summary>
		/// Reads the requests in the loaded txItem and adds them to the map of table -> key.
		/// </summary>
		private async Task LoadRequestsAsync()
		{
			AttributeValue requestsVal = _txItem[Transaction.AttributeName.Requests.ToString()];
			List<MemoryStream> rawRequests = (requestsVal != null && requestsVal.BS != null) ? requestsVal.BS : new List<MemoryStream>(0);

			foreach (MemoryStream rawRequest in rawRequests)
			{
				Request request = Request.Deserialize(TxId, rawRequest);
				// TODO don't make strings out of the PK all the time, also dangerous if behavior of toString changes!
				await AddRequestToMapAsync(request);
			}
		}

		/// <summary>
		/// Adds the request to the internal map structure if it doesn't already exist. If there is a write and a read to the same item,
		/// only the write will appear in this map. 
		/// </summary>
		/// <param name="request"> </param>
		/// <exception cref="DuplicateRequestException"> if there are multiple write operations to the same item. </exception>
		/// <returns> true if the request was added, false if not (isn't added if it's a read where there is already a write) </returns>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private boolean addRequestToMap(Request request) throws com.amazonaws.services.dynamodbv2.transactions.exceptions.DuplicateRequestException
		private async Task<bool> AddRequestToMapAsync(Request request)
		{
			Dictionary<string, AttributeValue> key = await request.GetKeyAsync(_txManager);
			ImmutableKey immutableKey = new ImmutableKey(key);

			Dictionary<ImmutableKey, Request> pkToRequestMap = _requestsMap[request.TableName];

			if (pkToRequestMap == null)
			{
				pkToRequestMap = new Dictionary<ImmutableKey, Request>();
				_requestsMap[request.TableName] = pkToRequestMap;
			}

			Request existingRequest = pkToRequestMap[immutableKey];
			if (existingRequest != null)
			{
				if (request is Request.GetItem)
				{
					return false;
				}

				if (existingRequest is Request.GetItem)
				{
					// ok to overwrite a lock with a write
				}
				else
				{
					throw new DuplicateRequestException(TxId, request.TableName, key.ToString());
				}
			}

			pkToRequestMap[immutableKey] = request;
			return true;
		}

		/// <summary>
		/// Really should only be used in the catch of addRequestAsync 
		/// </summary>
		/// <param name="request"> </param>
		private async Task RemoveRequestFromMapAsync(Request request)
		{
			// It's okay to leave empty maps around
			ImmutableKey key = new ImmutableKey(await request.GetKeyAsync(_txManager));
			_requestsMap[request.TableName].Remove(key);
		}

		/*
		 * For saving and loading old item images 
		 */

		/// <summary>
		/// Saves the old copy of the item.  Does not mutate the item, unless an exception is thrown.
		/// </summary>
		/// <param name="item"> </param>
		/// <param name="rid"> </param>
		public virtual void SaveItemImage(Dictionary<string, AttributeValue> item, int rid)
		{
			TxAssert(!item.ContainsKey(Transaction.AttributeName.Applied.ToString()), TxId, "The transaction has already applied this item image, it should not be saving over the item image with it");

			AttributeValue existingTxId = item[Transaction.AttributeName.Txid.ToString()] = new AttributeValue(TxId);
			if (existingTxId != null && !TxId.Equals(existingTxId.S))
			{
				throw new TransactionException(TxId, "Items in transactions may not contain the attribute named " + Transaction.AttributeName.Txid.ToString());
			}

			// Don't saveAsync over the already saved item.  Prevents us from saving the applied image instead of the previous image in the case
			// of a re-drive.
			// If we want to be extremely paranoid, we could expect every attribute to be set exactly already in a second write step, and assert
			Dictionary<string, ExpectedAttributeValue> expected = new Dictionary<string, ExpectedAttributeValue>(1);
			expected[Transaction.AttributeName.ImageId.ToString()] = new ExpectedAttributeValue
			{
			    Exists = false
			};

			AttributeValue existingImageId = item[Transaction.AttributeName.ImageId.ToString()] = new AttributeValue(TxId + "#" + rid);
			if (existingImageId != null)
			{
				throw new TransactionException(TxId, "Items in transactions may not contain the attribute named " + Transaction.AttributeName.ImageId.ToString() + ", value was already " + existingImageId);
			}

			// TODO failures?  Size validation?
			try
			{
			    _txManager.Client.PutItemAsync(new PutItemRequest
			    {
			        TableName = _txManager.ItemImageTableName,
			        Expected = expected,
			        Item = item
			    });
			}
			catch (ConditionalCheckFailedException)
			{
				// Already was saved
			}

			// do not mutate the item for the customer unless if there aren't exceptions
			item.Remove(Transaction.AttributeName.ImageId.ToString());
		}

		/// <summary>
		/// Retrieves the old copy of the item, with any item image saving specific attributes removed
		/// </summary>
		/// <param name="rid">
		/// @return </param>
		public virtual async Task<Dictionary<string, AttributeValue>> LoadItemImageAsync(int rid)
		{
			TxAssert(rid > 0, TxId, "Expected rid > 0");

			Dictionary<string, AttributeValue> key = new Dictionary<string, AttributeValue>(1);
			key[Transaction.AttributeName.ImageId.ToString()] = new AttributeValue(TxId + "#" + rid);

			Dictionary<string, AttributeValue> item = (await _txManager.Client.GetItemAsync(new GetItemRequest
			{
			    TableName = _txManager.ItemImageTableName,
                Key = key,
                ConsistentRead = true
			})).Item;

			if (item != null)
			{
				item.Remove(Transaction.AttributeName.ImageId.ToString());
			}

			return item;
		}

		/// <summary>
		/// Deletes the old version of the item.  Item images are immutable - it's just create + deleteAsync, so there is no need for
		/// concurrent modification checks.
		/// </summary>
		/// <param name="rid"> </param>
		public virtual void DeleteItemImage(int rid)
		{
			TxAssert(rid > 0, TxId, "Expected rid > 0");

			Dictionary<string, AttributeValue> key = new Dictionary<string, AttributeValue>(1);
			key[Transaction.AttributeName.ImageId.ToString()] = new AttributeValue(TxId + "#" + rid);

			_txManager.Client.DeleteItemAsync(new DeleteItemRequest
			{
			    TableName = _txManager.ItemImageTableName,
                Key = key
			});
		}

		/*
		 * For changing the state of the transaction
		 */

		/// <summary>
		/// Marks the transaction item as either COMMITTED or ROLLED_BACK, but only if it was in the PENDING state.
		/// It will also condition on the expected version. 
		/// </summary>
		/// <param name="targetState"> </param>
		/// <param name="expectedVersion"> </param>
		/// <exception cref="ConditionalCheckFailedException"> if the transaction doesn't exist, isn't PENDING, is finalized, 
		///         or the expected version doesn't match (if specified)   </exception>
		public virtual async Task FinishAsync(State targetState, int expectedVersion)
		{
			TxAssert(State.Committed.Equals(targetState) || State.RolledBack.Equals(targetState),"Illegal state in finish(): " + targetState, "txItem", _txItem);
			Dictionary<string, ExpectedAttributeValue> expected = new Dictionary<string, ExpectedAttributeValue>(2);
		    expected[Transaction.AttributeName.State.ToString()] = new ExpectedAttributeValue
		    {
		        Value = new AttributeValue {S = StatePending}
		    };
			expected[Transaction.AttributeName.Finalized.ToString()] = new ExpectedAttributeValue { Exists = false };
		    expected[Transaction.AttributeName.Version.ToString()] = new ExpectedAttributeValue
		    {
		        Value = new AttributeValue {N = Convert.ToString(expectedVersion)}
		    };

			Dictionary<string, AttributeValueUpdate> updates = new Dictionary<string, AttributeValueUpdate>();
		    updates.Add(Transaction.AttributeName.State.ToString(), new AttributeValueUpdate
		    {
		        Action = AttributeAction.PUT,
		        Value = new AttributeValue(StateToString(targetState))
		    });
		    updates.Add(Transaction.AttributeName.Date.ToString(), new AttributeValueUpdate
		    {
		        Action = AttributeAction.PUT,
		        Value = _txManager.CurrentTimeAttribute
		    });

			UpdateItemRequest finishRequest = new UpdateItemRequest
			{
			    TableName = _txManager.TransactionTableName,
                Key = _txKey,
                AttributeUpdates = updates,
                ReturnValues = ReturnValue.ALL_NEW,
                Expected = expected
            };

			UpdateItemResponse finishResponse = await _txManager.Client.UpdateItemAsync(finishRequest);
			_txItem = finishResponse.Attributes;
			if (_txItem == null)
			{
				throw new TransactionAssertionException(TxId, "Unexpected null tx item after committing " + targetState);
			}
		}

		/// <summary>
		/// Completes a transaction by marking its "Finalized" attribute.  This leaves the completed transaction item around
		/// so that the party who created the transaction can see whether it was completed or rolled back.  They can then either 
		/// deleteAsync the transaction record when they're done, or they can run a sweeper process to go and deleteAsync the completed transactions
		/// later on. 
		/// </summary>
		/// <param name="expectedCurrentState"> </param>
		/// <exception cref="ConditionalCheckFailedException"> if the transaction is completed, doesn't exist anymore, or even if it isn't committed or rolled back   </exception>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void completeAsync(final State expectedCurrentState) throws com.amazonaws.services.dynamodbv2.model.ConditionalCheckFailedException
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
		public virtual async Task CompleteAsync(State expectedCurrentState)
		{
			Dictionary<string, ExpectedAttributeValue> expected = new Dictionary<string, ExpectedAttributeValue>(2);

			if (State.Committed.Equals(expectedCurrentState))
			{
				expected[Transaction.AttributeName.State.ToString()] = new ExpectedAttributeValue(new AttributeValue(StateCommitted));
			}
			else if (State.RolledBack.Equals(expectedCurrentState))
			{
				expected[Transaction.AttributeName.State.ToString()] = new ExpectedAttributeValue(new AttributeValue(StateRolledBack));
			}
			else
			{
				throw new TransactionAssertionException(TxId, "Illegal state in finish(): " + expectedCurrentState + " txItem " + _txItem);
			}

			Dictionary<string, AttributeValueUpdate> updates = new Dictionary<string, AttributeValueUpdate>();
		    updates.Add(Transaction.AttributeName.Finalized.ToString(), new AttributeValueUpdate
		    {
		        Action = AttributeAction.PUT,
		        Value = new AttributeValue(Transaction.BooleanTrueAttrVal)

		    });
		    updates.Add(Transaction.AttributeName.Date.ToString(), new AttributeValueUpdate
		    {
		        Action = AttributeAction.PUT,
		        Value = _txManager.CurrentTimeAttribute
		    });

			UpdateItemRequest completeRequest = new UpdateItemRequest
			{
			    TableName = _txManager.TransactionTableName,
                AttributeUpdates = updates,
                ReturnValues = ReturnValue.ALL_NEW,
                Expected = expected
            };

			_txItem = (await _txManager.Client.UpdateItemAsync(completeRequest)).Attributes;
		}

		/// <summary>
		/// Deletes the tx item, only if it was in the "finalized" state.
		/// </summary>
		/// <exception cref="ConditionalCheckFailedException"> if the item does not exist or is not finalized </exception>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void deleteAsync() throws com.amazonaws.services.dynamodbv2.model.ConditionalCheckFailedException
		public virtual void Delete()
		{
			Dictionary<string, ExpectedAttributeValue> expected = new Dictionary<string, ExpectedAttributeValue>(1);
		    expected[Transaction.AttributeName.Finalized.ToString()] = new ExpectedAttributeValue
		    {
		        Value = new AttributeValue(Transaction.BooleanTrueAttrVal)
		    };

			DeleteItemRequest completeRequest = new DeleteItemRequest
			{
			    TableName = _txManager.TransactionTableName,
                Key = _txKey,
                Expected = expected
			};
			_txManager.Client.DeleteItemAsync(completeRequest);
		}

		public virtual bool Completed
		{
			get
			{
				bool isCompleted = _txItem.ContainsKey(Transaction.AttributeName.Finalized.ToString());
				if (isCompleted)
				{
					TxAssert(State.Committed.Equals(GetState()) || State.RolledBack.Equals(GetState()), TxId, "Unexpected terminal state for completed transaction", "state", GetState());
				}
				return isCompleted;
			}
		}

		/// <summary>
		/// For unit testing only
		/// @return
		/// </summary>
		protected internal virtual Dictionary<string, Dictionary<ImmutableKey, Request>> RequestMap
		{
			get
			{
				return _requestsMap;
			}
		}

		public enum State
		{
			Pending,
			Committed,
			RolledBack
		}

		/// <summary>
		/// Returns the state of the transaction item.  Keep in mind that the current state is never truly known until you try to perform an action,
		/// so be careful with how you use this information.
		/// 
		/// @return
		/// </summary>
		public virtual State GetState()
		{
			AttributeValue stateVal = _txItem[Transaction.AttributeName.State.ToString()];
			string txState = (stateVal != null) ? stateVal.S : null;

			if (StateCommitted.Equals(txState))
			{
				return State.Committed;
			}
			else if (StateRolledBack.Equals(txState))
			{
				return State.RolledBack;
			}
			else if (StatePending.Equals(txState))
			{
				return State.Pending;
			}
			else
			{
				throw new TransactionAssertionException(TxId, "Unrecognized transaction state: " + txState);
			}
		}

		public static string StateToString(State state)
		{
			switch (state)
			{
				case com.amazonaws.services.dynamodbv2.transactions.TransactionItem.State.Pending:
					return StatePending;
				case com.amazonaws.services.dynamodbv2.transactions.TransactionItem.State.Committed:
					return StateCommitted;
				case com.amazonaws.services.dynamodbv2.transactions.TransactionItem.State.RolledBack:
					return StateRolledBack;
				default:
					throw new TransactionAssertionException(null, "Unrecognized transaction state: " + state);
			}
		}

	}

 }