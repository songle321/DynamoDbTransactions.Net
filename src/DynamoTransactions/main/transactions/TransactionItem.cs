using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
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
		private const string STATE_PENDING = "P";
		private const string STATE_COMMITTED = "C";
		private const string STATE_ROLLED_BACK = "R";

		protected internal readonly string txId;
		private readonly TransactionManager txManager;
		private IDictionary<string, AttributeValue> txItem;
		private int version;
		private readonly Dictionary<string, AttributeValue> txKey;
		private readonly IDictionary<string, Dictionary<ImmutableKey, Request>> requestsMap = new Dictionary<string, Dictionary<ImmutableKey, Request>>();

		/*
		 * Constructors and initializers
		 */

		/// <summary>
		/// Inserts or retrieves a transaction record. 
		/// </summary>
		/// <param name="txId"> the id of the transaction to insert or retrieve </param>
		/// <param name="txManager"> </param>
		/// <param name="insert"> whether to insert the transaction (it's a new transaction) or to load an existing one </param>
		/// <exception cref="TransactionNotFoundException"> If it is being retrieved and it is not found </exception>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public TransactionItem(String txId, TransactionManager txManager, boolean insert) throws com.amazonaws.services.dynamodbv2.transactions.exceptions.TransactionNotFoundException
		public TransactionItem(string txId, TransactionManager txManager, bool insert) : this(txId, txManager, insert, null)
		{
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public TransactionItem(java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> txItem, TransactionManager txManager) throws com.amazonaws.services.dynamodbv2.transactions.exceptions.TransactionNotFoundException
		public TransactionItem(IDictionary<string, AttributeValue> txItem, TransactionManager txManager) : this(null, txManager, false, txItem)
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
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: protected TransactionItem(String txId, TransactionManager txManager, boolean insert, java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> txItem) throws com.amazonaws.services.dynamodbv2.transactions.exceptions.TransactionNotFoundException
		protected internal TransactionItem(string txId, TransactionManager txManager, bool insert, IDictionary<string, AttributeValue> txItem)
		{
			this.txManager = txManager;

			// Initialize txId, txKey, and txItem
			if (!string.ReferenceEquals(txId, null))
			{
				// Validate mutual exclusivity of inputs
				if (txItem != null)
				{
					throw new TransactionException(txId, "When providing txId, txItem must be null");
				}
				this.txId = txId;
				IDictionary<string, AttributeValue> txKeyMap = new Dictionary<string, AttributeValue>(1);
				txKeyMap[Transaction.AttributeName.TXID.ToString()] = new AttributeValue(txId);
				this.txKey = new Dictionary<string, AttributeValue>(txKeyMap);
				if (insert)
				{
					this.txItem = this.insert();
				}
				else
				{
					this.txItem = get();
					if (this.txItem == null)
					{
						throw new TransactionNotFoundException(this.txId);
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
				this.txItem = txItem;
				if (!isTransactionItem(txItem))
				{
					throw new TransactionException(txId, "txItem is not a transaction item");
				}
				this.txId = txItem[Transaction.AttributeName.TXID.ToString()].S;
				IDictionary<string, AttributeValue> txKeyMap = new Dictionary<string, AttributeValue>(1);
				txKeyMap[Transaction.AttributeName.TXID.ToString()] = new AttributeValue(this.txId);
				this.txKey = new Dictionary<string, AttributeValue>(txKeyMap);
			}
			else
			{
				throw new TransactionException(null, "Either txId or txItem must be specified");
			}

			// Initialize the version
			AttributeValue txVersionVal = this.txItem[Transaction.AttributeName.VERSION.ToString()];
			if (txVersionVal == null || txVersionVal.N == null)
			{
				throw new TransactionException(this.txId, "Version number is not present in TX record");
			}
			version = int.Parse(txVersionVal.N);

			// Build the requests structure
			loadRequests();
		}

		/// <summary>
		/// Inserts a new transaction item into the table.  Assumes txKey is already initialized. </summary>
		/// <returns> the txItem </returns>
		/// <exception cref="TransactionException"> if the transaction already exists </exception>
		private IDictionary<string, AttributeValue> insert()
		{
			Dictionary<string, AttributeValue> item = new Dictionary<string, AttributeValue>();
			item[Transaction.AttributeName.STATE.ToString()] = new AttributeValue(STATE_PENDING);
			item[Transaction.AttributeName.VERSION.ToString()] = (new AttributeValue {N = Convert.ToString(1)});
			item[Transaction.AttributeName.DATE.ToString()] = txManager.CurrentTimeAttribute;
            foreach(var keyValue in txKey) item.Add(keyValue.Key, keyValue.Value);

			Dictionary<string, ExpectedAttributeValue> expectNotExists = new Dictionary<string, ExpectedAttributeValue>(2);
			expectNotExists[Transaction.AttributeName.TXID.ToString()] = new ExpectedAttributeValue(false);
			expectNotExists[Transaction.AttributeName.STATE.ToString()] = new ExpectedAttributeValue(false);

		    PutItemRequest request = new PutItemRequest
		    {
		        TableName = txManager.TransactionTableName,
		        Item = item,
		        Expected = expectNotExists
		    };

			try
			{
				txManager.Client.PutItemAsync(request).Wait();
				return item;
			}
			catch (ConditionalCheckFailedException e)
			{
				throw new TransactionException("Failed to create new transaction with id " + txId, e);
			}
		}

		/// <summary>
		/// Fetches this transaction item from the tx table.  Uses consistent read.
		/// </summary>
		/// <returns> the latest copy of the transaction item, or null if it has been completed (and deleted)  </returns>
		private IDictionary<string, AttributeValue> get()
		{
			GetItemRequest getRequest = new GetItemRequest
			{
			    TableName = txManager.TransactionTableName,
                Key = txKey.ToDictionary(x => x.Key, x => x.Value),
                ConsistentRead = true
			};
			return txManager.Client.GetItemAsync(getRequest).Result.Item;
		}

		/// <summary>
		/// Gets the version of the transaction image currently loaded.  Useful for determining if the item has changed when committing the transaction.
		/// </summary>
		public virtual int Version
		{
			get
			{
				return version;
			}
		}

		/// <summary>
		/// Determines whether the record is a valid transaction item.  Useful because backups of items in the transaction are 
		/// saved in the same table as the transaction item.
		/// </summary>
		/// <param name="txItem">
		/// @return </param>
		public static bool isTransactionItem(IDictionary<string, AttributeValue> txItem)
		{
			if (txItem == null)
			{
				throw new TransactionException(null, "txItem must not be null");
			}

			if (!txItem.ContainsKey(Transaction.AttributeName.TXID.ToString()))
			{
				return false;
			}

			if (txItem[Transaction.AttributeName.TXID.ToString()].S == null)
			{
				return false;
			}

			return true;
		}

		public virtual long LastUpdateTimeMillis
		{
			get
			{
				AttributeValue requestsVal = txItem[Transaction.AttributeName.DATE.ToString()];
				if (requestsVal == null || requestsVal.N == null)
				{
					throw new TransactionAssertionException(txId, "Expected date attribute to be defined");
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
				foreach (KeyValuePair<string, Dictionary<ImmutableKey, Request>> tableRequests in requestsMap.SetOfKeyValuePairs())
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
		public virtual Request getRequestForKey(string tableName, IDictionary<string, AttributeValue> key)
		{
			Dictionary<ImmutableKey, Request> tableRequests = requestsMap[tableName];

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
		/// <exception cref="ConditionalCheckFailedException"> if the tx item changed out from under us.  If you get this you must throw this TransactionItem away. </exception>
		/// <exception cref="DuplicateRequestException"> If you get this you do not need to throw away the item. </exception>
		/// <exception cref="InvalidRequestException"> If the request would add too much data to the transaction </exception>
		/// <returns> true if the request was added, false if it didn't need to be added (because it was a duplicate lock request) </returns>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public synchronized boolean addRequest(Request callerRequest) throws com.amazonaws.services.dynamodbv2.model.ConditionalCheckFailedException, com.amazonaws.services.dynamodbv2.transactions.exceptions.DuplicateRequestException
		public virtual bool addRequest(Request callerRequest)
		{
			lock (this)
			{
				// 1. Ensure the request is unique (modifies the internal data structure if it is unique)
				//    However, do not not short circuit.  If we're doing a read in a resumed transaction, it's important to ensure we're returning
				//    any writes that happened before. 
				addRequestToMap(callerRequest);
        
				callerRequest.Rid = version;
        
				// 2. Write request to transaction item
				MemoryStream requestBytes = Request.serialize(txId, callerRequest);
				AttributeValueUpdate txItemUpdate = new AttributeValueUpdate
				{
				    Action = AttributeAction.ADD,
                    Value = new AttributeValue
                    {
                        BS = new List<MemoryStream> {requestBytes}
                    }
				};
        
				Dictionary<string, AttributeValueUpdate> txItemUpdates = new Dictionary<string, AttributeValueUpdate>();
				txItemUpdates[Transaction.AttributeName.REQUESTS.ToString()] = txItemUpdate;
			    txItemUpdates[Transaction.AttributeName.VERSION.ToString()] = new AttributeValueUpdate
			    {
			        Action = AttributeAction.ADD,
			        Value = new AttributeValue
			        {
			            N = "1"
			        }
			    };
			    txItemUpdates[Transaction.AttributeName.DATE.ToString()] = new AttributeValueUpdate
			    {
			        Action = AttributeAction.PUT,
			        Value = txManager.CurrentTimeAttribute
			    };
        
				Dictionary<string, ExpectedAttributeValue> expected = new Dictionary<string, ExpectedAttributeValue>();
				expected[Transaction.AttributeName.STATE.ToString()] = new ExpectedAttributeValue(new AttributeValue(STATE_PENDING));
				expected[Transaction.AttributeName.VERSION.ToString()] = new ExpectedAttributeValue(new AttributeValue
				{
				    N = Convert.ToString(version)
                });

			    UpdateItemRequest txItemUpdateRequest = new UpdateItemRequest
			    {
			        TableName = txManager.TransactionTableName,
			        Key = txKey,
			        Expected = expected,
			        ReturnValues = ReturnValue.ALL_NEW,
			        AttributeUpdates = txItemUpdates
			    };
        
				try
				{
					txItem = txManager.Client.UpdateItemAsync(txItemUpdateRequest).Result.Attributes;
					int newVersion = int.Parse(txItem[Transaction.AttributeName.VERSION.ToString()].N);
					txAssert(newVersion == version + 1, txId, "Unexpected version number from update result");
					version = newVersion;
				}
				catch (AmazonServiceException e)
				{
					if ("ValidationException".Equals(e.ErrorCode))
					{
						removeRequestFromMap(callerRequest);
						throw new InvalidRequestException("The amount of data in the transaction cannot exceed the DynamoDB item size limit", txId, callerRequest.TableName, callerRequest.getKey(txManager), callerRequest);
					}
					else
					{
						throw e;
					}
				}
				return true;
			}
		}

		/// <summary>
		/// Reads the requests in the loaded txItem and adds them to the map of table -> key.
		/// </summary>
		private void loadRequests()
		{
			AttributeValue requestsVal = txItem[Transaction.AttributeName.REQUESTS.ToString()];
			IList<MemoryStream> rawRequests = (requestsVal != null && requestsVal.BS != null) ? requestsVal.BS : new List<MemoryStream>(0);

			foreach (MemoryStream rawRequest in rawRequests)
			{
				Request request = Request.deserialize(txId, rawRequest);
				// TODO don't make strings out of the PK all the time, also dangerous if behavior of toString changes!
				addRequestToMap(request);
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
		private bool addRequestToMap(Request request)
		{
			IDictionary<string, AttributeValue> key = request.getKey(txManager);
			ImmutableKey immutableKey = new ImmutableKey(key);

			Dictionary<ImmutableKey, Request> pkToRequestMap = requestsMap[request.TableName];

			if (pkToRequestMap == null)
			{
				pkToRequestMap = new Dictionary<ImmutableKey, Request>();
				requestsMap[request.TableName] = pkToRequestMap;
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
					throw new DuplicateRequestException(txId, request.TableName, key.ToString());
				}
			}

			pkToRequestMap[immutableKey] = request;
			return true;
		}

		/// <summary>
		/// Really should only be used in the catch of addRequest 
		/// </summary>
		/// <param name="request"> </param>
		private void removeRequestFromMap(Request request)
		{
			// It's okay to leave empty maps around
			ImmutableKey key = new ImmutableKey(request.getKey(txManager));
			requestsMap[request.TableName].Remove(key);
		}

		/*
		 * For saving and loading old item images 
		 */

		/// <summary>
		/// Saves the old copy of the item.  Does not mutate the item, unless an exception is thrown.
		/// </summary>
		/// <param name="item"> </param>
		/// <param name="rid"> </param>
		public virtual void saveItemImage(Dictionary<string, AttributeValue> item, int rid)
		{
			txAssert(!item.ContainsKey(Transaction.AttributeName.APPLIED.ToString()), txId, "The transaction has already applied this item image, it should not be saving over the item image with it");

			AttributeValue existingTxId = item[Transaction.AttributeName.TXID.ToString()] = new AttributeValue(txId);
			if (existingTxId != null && !txId.Equals(existingTxId.S))
			{
				throw new TransactionException(txId, "Items in transactions may not contain the attribute named " + Transaction.AttributeName.TXID.ToString());
			}

			// Don't save over the already saved item.  Prevents us from saving the applied image instead of the previous image in the case
			// of a re-drive.
			// If we want to be extremely paranoid, we could expect every attribute to be set exactly already in a second write step, and assert
			Dictionary<string, ExpectedAttributeValue> expected = new Dictionary<string, ExpectedAttributeValue>(1);
			expected[Transaction.AttributeName.IMAGE_ID.ToString()] = new ExpectedAttributeValue
			{
			    Exists = false
			};

			AttributeValue existingImageId = item[Transaction.AttributeName.IMAGE_ID.ToString()] = new AttributeValue(txId + "#" + rid);
			if (existingImageId != null)
			{
				throw new TransactionException(txId, "Items in transactions may not contain the attribute named " + Transaction.AttributeName.IMAGE_ID.ToString() + ", value was already " + existingImageId);
			}

			// TODO failures?  Size validation?
			try
			{
			    txManager.Client.PutItemAsync(new PutItemRequest
			    {
			        TableName = txManager.ItemImageTableName,
			        Expected = expected,
			        Item = item
			    });
			}
			catch (ConditionalCheckFailedException)
			{
				// Already was saved
			}

			// do not mutate the item for the customer unless if there aren't exceptions
			item.Remove(Transaction.AttributeName.IMAGE_ID.ToString());
		}

		/// <summary>
		/// Retrieves the old copy of the item, with any item image saving specific attributes removed
		/// </summary>
		/// <param name="rid">
		/// @return </param>
		public virtual Dictionary<string, AttributeValue> loadItemImage(int rid)
		{
			txAssert(rid > 0, txId, "Expected rid > 0");

			Dictionary<string, AttributeValue> key = new Dictionary<string, AttributeValue>(1);
			key[Transaction.AttributeName.IMAGE_ID.ToString()] = new AttributeValue(txId + "#" + rid);

			IDictionary<string, AttributeValue> item = txManager.Client.GetItemAsync(new GetItemRequest
			{
			    TableName = txManager.ItemImageTableName,
                Key = key,
                ConsistentRead = true
			}).Result.Item;

			if (item != null)
			{
				item.Remove(Transaction.AttributeName.IMAGE_ID.ToString());
			}

			return item;
		}

		/// <summary>
		/// Deletes the old version of the item.  Item images are immutable - it's just create + delete, so there is no need for
		/// concurrent modification checks.
		/// </summary>
		/// <param name="rid"> </param>
		public virtual void deleteItemImage(int rid)
		{
			txAssert(rid > 0, txId, "Expected rid > 0");

			Dictionary<string, AttributeValue> key = new Dictionary<string, AttributeValue>(1);
			key[Transaction.AttributeName.IMAGE_ID.ToString()] = new AttributeValue(txId + "#" + rid);

			txManager.Client.DeleteItemAsync(new DeleteItemRequest
			{
			    TableName = txManager.ItemImageTableName,
                Key = key
			}).Wait();
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
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void finish(final State targetState, final int expectedVersion) throws com.amazonaws.services.dynamodbv2.model.ConditionalCheckFailedException
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
		public virtual void finish(State targetState, int expectedVersion)
		{
			txAssert(State.COMMITTED.Equals(targetState) || State.ROLLED_BACK.Equals(targetState),"Illegal state in finish(): " + targetState, "txItem", txItem);
			Dictionary<string, ExpectedAttributeValue> expected = new Dictionary<string, ExpectedAttributeValue>(2);
		    expected[Transaction.AttributeName.STATE.ToString()] = new ExpectedAttributeValue
		    {
		        Value = new AttributeValue {S = STATE_PENDING}
		    };
			expected[Transaction.AttributeName.FINALIZED.ToString()] = new ExpectedAttributeValue { Exists = false };
		    expected[Transaction.AttributeName.VERSION.ToString()] = new ExpectedAttributeValue
		    {
		        Value = new AttributeValue {N = Convert.ToString(expectedVersion)}
		    };

			Dictionary<string, AttributeValueUpdate> updates = new Dictionary<string, AttributeValueUpdate>();
		    updates.Add(Transaction.AttributeName.STATE.ToString(), new AttributeValueUpdate
		    {
		        Action = AttributeAction.PUT,
		        Value = new AttributeValue(stateToString(targetState))
		    });
		    updates.Add(Transaction.AttributeName.DATE.ToString(), new AttributeValueUpdate
		    {
		        Action = AttributeAction.PUT,
		        Value = txManager.CurrentTimeAttribute
		    });

			UpdateItemRequest finishRequest = new UpdateItemRequest
			{
			    TableName = txManager.TransactionTableName,
                Key = txKey,
                AttributeUpdates = updates,
                ReturnValues = ReturnValue.ALL_NEW,
                Expected = expected
            };

			UpdateItemResponse finishResponse = txManager.Client.UpdateItemAsync(finishRequest).Result;
			txItem = finishResponse.Attributes;
			if (txItem == null)
			{
				throw new TransactionAssertionException(txId, "Unexpected null tx item after committing " + targetState);
			}
		}

		/// <summary>
		/// Completes a transaction by marking its "Finalized" attribute.  This leaves the completed transaction item around
		/// so that the party who created the transaction can see whether it was completed or rolled back.  They can then either 
		/// delete the transaction record when they're done, or they can run a sweeper process to go and delete the completed transactions
		/// later on. 
		/// </summary>
		/// <param name="expectedCurrentState"> </param>
		/// <exception cref="ConditionalCheckFailedException"> if the transaction is completed, doesn't exist anymore, or even if it isn't committed or rolled back   </exception>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void complete(final State expectedCurrentState) throws com.amazonaws.services.dynamodbv2.model.ConditionalCheckFailedException
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
		public virtual void complete(State expectedCurrentState)
		{
			Dictionary<string, ExpectedAttributeValue> expected = new Dictionary<string, ExpectedAttributeValue>(2);

			if (State.COMMITTED.Equals(expectedCurrentState))
			{
				expected[Transaction.AttributeName.STATE.ToString()] = new ExpectedAttributeValue(new AttributeValue(STATE_COMMITTED));
			}
			else if (State.ROLLED_BACK.Equals(expectedCurrentState))
			{
				expected[Transaction.AttributeName.STATE.ToString()] = new ExpectedAttributeValue(new AttributeValue(STATE_ROLLED_BACK));
			}
			else
			{
				throw new TransactionAssertionException(txId, "Illegal state in finish(): " + expectedCurrentState + " txItem " + txItem);
			}

			Dictionary<string, AttributeValueUpdate> updates = new Dictionary<string, AttributeValueUpdate>();
		    updates.Add(Transaction.AttributeName.FINALIZED.ToString(), new AttributeValueUpdate
		    {
		        Action = AttributeAction.PUT,
		        Value = new AttributeValue(Transaction.BOOLEAN_TRUE_ATTR_VAL)

		    });
		    updates.Add(Transaction.AttributeName.DATE.ToString(), new AttributeValueUpdate
		    {
		        Action = AttributeAction.PUT,
		        Value = txManager.CurrentTimeAttribute
		    });

			UpdateItemRequest completeRequest = new UpdateItemRequest
			{
			    TableName = txManager.TransactionTableName,
                AttributeUpdates = updates,
                ReturnValues = ReturnValue.ALL_NEW,
                Expected = expected
            };

			txItem = txManager.Client.UpdateItemAsync(completeRequest).Result.Attributes;
		}

		/// <summary>
		/// Deletes the tx item, only if it was in the "finalized" state.
		/// </summary>
		/// <exception cref="ConditionalCheckFailedException"> if the item does not exist or is not finalized </exception>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void delete() throws com.amazonaws.services.dynamodbv2.model.ConditionalCheckFailedException
		public virtual void delete()
		{
			Dictionary<string, ExpectedAttributeValue> expected = new Dictionary<string, ExpectedAttributeValue>(1);
		    expected[Transaction.AttributeName.FINALIZED.ToString()] = new ExpectedAttributeValue
		    {
		        Value = new AttributeValue(Transaction.BOOLEAN_TRUE_ATTR_VAL)
		    };

			DeleteItemRequest completeRequest = new DeleteItemRequest
			{
			    TableName = txManager.TransactionTableName,
                Key = txKey,
                Expected = expected
			};
			txManager.Client.DeleteItemAsync(completeRequest).Wait();
		}

		public virtual bool Completed
		{
			get
			{
				bool isCompleted = txItem.ContainsKey(Transaction.AttributeName.FINALIZED.ToString());
				if (isCompleted)
				{
					txAssert(State.COMMITTED.Equals(getState()) || State.ROLLED_BACK.Equals(getState()), txId, "Unexpected terminal state for completed transaction", "state", getState());
				}
				return isCompleted;
			}
		}

		/// <summary>
		/// For unit testing only
		/// @return
		/// </summary>
		protected internal virtual IDictionary<string, Dictionary<ImmutableKey, Request>> RequestMap
		{
			get
			{
				return requestsMap;
			}
		}

		public enum State
		{
			PENDING,
			COMMITTED,
			ROLLED_BACK
		}

		/// <summary>
		/// Returns the state of the transaction item.  Keep in mind that the current state is never truly known until you try to perform an action,
		/// so be careful with how you use this information.
		/// 
		/// @return
		/// </summary>
		public virtual State getState()
		{
			AttributeValue stateVal = txItem[Transaction.AttributeName.STATE.ToString()];
			string txState = (stateVal != null) ? stateVal.S : null;

			if (STATE_COMMITTED.Equals(txState))
			{
				return State.COMMITTED;
			}
			else if (STATE_ROLLED_BACK.Equals(txState))
			{
				return State.ROLLED_BACK;
			}
			else if (STATE_PENDING.Equals(txState))
			{
				return State.PENDING;
			}
			else
			{
				throw new TransactionAssertionException(txId, "Unrecognized transaction state: " + txState);
			}
		}

		public static string stateToString(State state)
		{
			switch (state)
			{
				case com.amazonaws.services.dynamodbv2.transactions.TransactionItem.State.PENDING:
					return STATE_PENDING;
				case com.amazonaws.services.dynamodbv2.transactions.TransactionItem.State.COMMITTED:
					return STATE_COMMITTED;
				case com.amazonaws.services.dynamodbv2.transactions.TransactionItem.State.ROLLED_BACK:
					return STATE_ROLLED_BACK;
				default:
					throw new TransactionAssertionException(null, "Unrecognized transaction state: " + state);
			}
		}

	}

 }