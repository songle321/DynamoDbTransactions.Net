using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using com.amazonaws.services.dynamodbv2.util;

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
	/// A factory for client-side transactions on DynamoDB.  Thread-safe. 
	/// </summary>
	public class TransactionManager
	{

		private static readonly Log log = LogFactory.getLog(typeof(TransactionManager));
		private static readonly IList<AttributeDefinition> TRANSACTIONS_TABLE_ATTRIBUTES;

	    private static readonly IList<KeySchemaElement> TRANSACTIONS_TABLE_KEY_SCHEMA =new []
	    {
	        new KeySchemaElement
	        {
	            AttributeName = Transaction.AttributeName.TXID.ToString(),
	            KeyType = KeyType.HASH
	        }
	    };

		private static readonly IList<AttributeDefinition> TRANSACTION_IMAGES_TABLE_ATTRIBUTES;

	    private static readonly IList<KeySchemaElement> TRANSACTION_IMAGES_TABLE_KEY_SCHEMA = new[]
	    {
	        new KeySchemaElement
	        {
	            AttributeName = Transaction.AttributeName.IMAGE_ID.ToString(),
	            KeyType = KeyType.HASH
	        }
	    };


		static TransactionManager()
		{
			IList<AttributeDefinition> definition = Arrays.asList((new AttributeDefinition()).withAttributeName(Transaction.AttributeName.TXID.ToString()).withAttributeType(ScalarAttributeType.S));
			definition.Sort(new AttributeDefinitionComparator());
			TRANSACTIONS_TABLE_ATTRIBUTES = Collections.unmodifiableList(definition);

			definition = Arrays.asList((new AttributeDefinition()).withAttributeName(Transaction.AttributeName.IMAGE_ID.ToString()).withAttributeType(ScalarAttributeType.S));
			definition.Sort(new AttributeDefinitionComparator());
			TRANSACTION_IMAGES_TABLE_ATTRIBUTES = Collections.unmodifiableList(definition);
		}

		private readonly AmazonDynamoDBClient client;
		private readonly string transactionTableName;
		private readonly string itemImageTableName;
		private readonly ConcurrentDictionary<string, IList<KeySchemaElement>> tableSchemaCache = new ConcurrentDictionary<string, IList<KeySchemaElement>>();
		private readonly DynamoDBMapper clientMapper;
		private readonly ThreadLocalDynamoDBFacade facadeProxy;
		private readonly ReadUncommittedIsolationHandlerImpl readUncommittedIsolationHandler;
		private readonly ReadCommittedIsolationHandlerImpl readCommittedIsolationHandler;

		public TransactionManager(AmazonDynamoDBClient client, string transactionTableName, string itemImageTableName) : this(client, transactionTableName, itemImageTableName, DynamoDBMapperConfig.DEFAULT)
		{
		}

		public TransactionManager(AmazonDynamoDBClient client, string transactionTableName, string itemImageTableName, DynamoDBMapperConfig config) : this(client, transactionTableName, itemImageTableName, config, null)
		{
		}

		public TransactionManager(AmazonDynamoDBClient client, string transactionTableName, string itemImageTableName, DynamoDBMapperConfig config, AttributeTransformer transformer)
		{
			if (client == null)
			{
				throw new System.ArgumentException("client must not be null");
			}
			if (string.ReferenceEquals(transactionTableName, null))
			{
				throw new System.ArgumentException("transactionTableName must not be null");
			}
			if (string.ReferenceEquals(itemImageTableName, null))
			{
				throw new System.ArgumentException("itemImageTableName must not be null");
			}
			this.client = client;
			this.transactionTableName = transactionTableName;
			this.itemImageTableName = itemImageTableName;
			this.facadeProxy = new ThreadLocalDynamoDBFacade();
			this.clientMapper = new DynamoDBMapper(facadeProxy, config, transformer);
			this.readUncommittedIsolationHandler = new ReadUncommittedIsolationHandlerImpl();
			this.readCommittedIsolationHandler = new ReadCommittedIsolationHandlerImpl(this);
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: protected java.util.List<com.amazonaws.services.dynamodbv2.model.KeySchemaElement> getTableSchema(String tableName) throws com.amazonaws.services.dynamodbv2.model.ResourceNotFoundException
		protected internal virtual IList<KeySchemaElement> getTableSchema(string tableName)
		{
			IList<KeySchemaElement> schema = tableSchemaCache[tableName];
			if (schema == null)
			{
				DescribeTableResponse result = client.describeTable((new DescribeTableRequest()).withTableName(tableName));
				schema = Collections.unmodifiableList(result.Table.KeySchema);
				tableSchemaCache[tableName] = schema;
			}
			return schema;
		}

//JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
//ORIGINAL LINE: protected java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> createKeyMap(final String tableName, final java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> item)
		protected internal virtual Dictionary<string, AttributeValue> createKeyMap(string tableName, IDictionary<string, AttributeValue> item)
		{
			if (string.ReferenceEquals(tableName, null))
			{
				throw new System.ArgumentException("must specify a tableName");
			}
			if (item == null)
			{
				throw new System.ArgumentException("must specify an item");
			}
			IList<KeySchemaElement> schema = getTableSchema(tableName);
			IDictionary<string, AttributeValue> key = new Dictionary<string, AttributeValue>(schema.Count);
			foreach (KeySchemaElement element in schema)
			{
				key[element.AttributeName] = item[element.AttributeName];
			}
			return key;
		}

		public virtual Transaction newTransaction()
		{
			Transaction transaction = new Transaction(UUID.randomUUID().ToString(), this, true);
			log.info("Started transaction " + transaction.Id);
			return transaction;
		}

		public virtual Transaction resumeTransaction(string txId)
		{
			Transaction transaction = new Transaction(txId, this, false);
			log.info("Resuming transaction from id " + transaction.Id);
			return transaction;
		}

		public virtual Transaction resumeTransaction(IDictionary<string, AttributeValue> txItem)
		{
			Transaction transaction = new Transaction(txItem, this);
			log.info("Resuming transaction from item " + transaction.Id);
			return transaction;
		}

		public static bool isTransactionItem(IDictionary<string, AttributeValue> txItem)
		{
			return TransactionItem.isTransactionItem(txItem);
		}

		public virtual AmazonDynamoDBClient Client
		{
			get
			{
				return client;
			}
		}

		public virtual DynamoDBMapper ClientMapper
		{
			get
			{
				return clientMapper;
			}
		}

		protected internal virtual ThreadLocalDynamoDBFacade FacadeProxy
		{
			get
			{
				return facadeProxy;
			}
		}

		protected internal virtual ReadIsolationHandler getReadIsolationHandler(Transaction.IsolationLevel isolationLevel)
		{
			if (isolationLevel == null)
			{
				throw new System.ArgumentException("isolation level is required");
			}
			switch (isolationLevel)
			{
				case UNCOMMITTED:
					return readUncommittedIsolationHandler;
				case COMMITTED:
					return readCommittedIsolationHandler;
				case READ_LOCK:
					throw new System.ArgumentException("Cannot call GetItemAsync at the READ_LOCK isolation level outside of a transaction. Call GetItemAsync on a transaction directly instead.");
				default:
					throw new System.ArgumentException("Unrecognized isolation level: " + isolationLevel);
			}
		}

		public virtual async Task<GetItemResponse> GetItemAsync(GetItemRequest request, Transaction.IsolationLevel isolationLevel, CancellationToken cancellationToken)
		{
			if (request.AttributesToGet != null)
			{
				ISet<string> attributesToGet = new HashSet<string>(request.AttributesToGet);
				attributesToGet.UnionWith(Transaction.SPECIAL_ATTR_NAMES);
				request.AttributesToGet = attributesToGet.ToList();
			}
			GetItemResponse result = await Client.GetItemAsync(request, cancellationToken);
			Dictionary<string, AttributeValue> item = getReadIsolationHandler(isolationLevel).handleItem(result.Item, request.AttributesToGet, request.TableName);
			Transaction.stripSpecialAttributes(item);
			result.Item = item;
			return result;
		}

		public virtual string TransactionTableName
		{
			get
			{
				return transactionTableName;
			}
		}

		public virtual string ItemImageTableName
		{
			get
			{
				return itemImageTableName;
			}
		}

		/// <summary>
		/// Breaks an item lock and leaves the item intact, leaving an item in an unknown state.  Only works if the owning transaction
		/// does not exist. 
		/// 
		///   1) It could leave an item that should not exist (was inserted only for obtaining the lock)
		///   2) It could replace the item with an old copy of the item from an unknown previous transaction
		///   3) A request from an earlier transaction could be applied a second time
		///   4) Other conditions of this nature 
		/// </summary>
		/// <param name="tableName"> </param>
		/// <param name="item"> </param>
		/// <param name="txId"> </param>
		public virtual void breakLock(string tableName, IDictionary<string, AttributeValue> item, string txId)
		{
			if (log.WarnEnabled)
			{
				log.warn("Breaking a lock on table " + tableName + " for transaction " + txId + " for item " + item + ".  This will leave the item in an unknown state");
			}
			Transaction.unlockItemUnsafe(this, tableName, item, txId);
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public static void verifyOrCreateTransactionTable(com.amazonaws.services.dynamodbv2.AmazonDynamoDBClient client, String tableName, long readCapacityUnits, long writeCapacityUnits, Nullable<long> waitTimeSeconds) throws InterruptedException
		public static void verifyOrCreateTransactionTable(AmazonDynamoDBClient client, string tableName, long readCapacityUnits, long writeCapacityUnits, long? waitTimeSeconds)
		{
			(new TableHelper(client)).verifyOrCreateTable(tableName, TRANSACTIONS_TABLE_ATTRIBUTES, TRANSACTIONS_TABLE_KEY_SCHEMA, null, new ProvisionedThroughput()
					.withReadCapacityUnits(readCapacityUnits).withWriteCapacityUnits(writeCapacityUnits), waitTimeSeconds);
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public static void verifyOrCreateTransactionImagesTable(com.amazonaws.services.dynamodbv2.AmazonDynamoDBClient client, String tableName, long readCapacityUnits, long writeCapacityUnits, Nullable<long> waitTimeSeconds) throws InterruptedException
		public static void verifyOrCreateTransactionImagesTable(AmazonDynamoDBClient client, string tableName, long readCapacityUnits, long writeCapacityUnits, long? waitTimeSeconds)
		{
			(new TableHelper(client)).verifyOrCreateTable(tableName, TRANSACTION_IMAGES_TABLE_ATTRIBUTES, TRANSACTION_IMAGES_TABLE_KEY_SCHEMA, null, new ProvisionedThroughput()
					.withReadCapacityUnits(readCapacityUnits).withWriteCapacityUnits(writeCapacityUnits), waitTimeSeconds);
		}

		/// <summary>
		/// Ensures that the transaction table exists and has the correct schema.
		/// </summary>
		/// <param name="client"> </param>
		/// <param name="transactionTableName"> </param>
		/// <param name="transactionImagesTableName"> </param>
		/// <exception cref="ResourceInUseException"> if the table exists but has the wrong schema </exception>
		/// <exception cref="ResourceNotFoundException"> if the table does not exist </exception>
		public static void verifyTransactionTablesExist(AmazonDynamoDBClient client, string transactionTableName, string transactionImagesTableName)
		{
			string state = (new TableHelper(client)).verifyTableExists(transactionTableName, TRANSACTIONS_TABLE_ATTRIBUTES, TRANSACTIONS_TABLE_KEY_SCHEMA, null);
			if (!"ACTIVE".Equals(state))
			{
				throw new ResourceInUseException("Table " + transactionTableName + " is not ACTIVE");
			}

			state = (new TableHelper(client)).verifyTableExists(transactionImagesTableName, TRANSACTION_IMAGES_TABLE_ATTRIBUTES, TRANSACTION_IMAGES_TABLE_KEY_SCHEMA, null);
			if (!"ACTIVE".Equals(state))
			{
				throw new ResourceInUseException("Table " + transactionImagesTableName + " is not ACTIVE");
			}
		}

		protected internal virtual double CurrentTime
		{
			get
			{
				return DateTimeHelperClass.CurrentUnixTimeMillis() / 1000.0;
			}
		}

		protected internal virtual AttributeValue CurrentTimeAttribute
		{
			get
			{
				return (new AttributeValue()).withN((new double?(CurrentTime)).ToString());
			}
		}

		private class AttributeDefinitionComparator : IComparer<AttributeDefinition>
		{

			public virtual int Compare(AttributeDefinition arg0, AttributeDefinition arg1)
			{
				if (arg0 == null)
				{
					return -1;
				}

				if (arg1 == null)
				{
					return 1;
				}

				int comp = arg0.AttributeName.compareTo(arg1.AttributeName);
				if (comp != 0)
				{
					return comp;
				}

				comp = arg0.AttributeType.compareTo(arg1.AttributeType);
				return comp;
			}

		}

		/// <summary>
		/// Load an item outside a transaction using the mapper.
		/// </summary>
		/// <param name="item">
		///            An item where the key attributes are populated; the key
		///            attributes from this item are used to form the GetItemRequest
		///            to retrieve the item. </param>
		/// <param name="isolationLevel">
		///            The isolation level to use; this has the same meaning as for
		///            <seealso cref="TransactionManager#GetItemAsync(GetItemRequest, IsolationLevel)"/>
		///            . </param>
		/// <returns> An instance of the item class with all attributes populated from
		///         the table, or null if the item does not exist. </returns>
		public virtual T load<T>(T item, Transaction.IsolationLevel isolationLevel)
		{
			try
			{
				FacadeProxy.Backend = new TransactionManagerDynamoDBFacade(this, isolationLevel);
				return ClientMapper.load(item);
			}
			finally
			{
				FacadeProxy.Backend = null;
			}
		}
	}

 }