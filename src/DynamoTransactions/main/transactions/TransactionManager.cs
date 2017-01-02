using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using Amazon.Util;
using com.amazonaws.services.dynamodbv2.util;
using DynamoDBContextConfig = Amazon.DynamoDBv2.DataModel.DynamoDBContextConfig;

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
        private static readonly Log Log = LogFactory.GetLog(typeof(TransactionManager));
        private static readonly List<AttributeDefinition> TransactionsTableAttributes;

        private static readonly List<KeySchemaElement> TransactionsTableKeySchema = new List<KeySchemaElement>
        {
            new KeySchemaElement
            {
                AttributeName = Transaction.AttributeName.Txid.ToString(),
                KeyType = KeyType.HASH
            }
        };

        private static readonly List<AttributeDefinition> TransactionImagesTableAttributes;

        private static readonly List<KeySchemaElement> TransactionImagesTableKeySchema = new List<KeySchemaElement>
        {
            new KeySchemaElement
            {
                AttributeName = Transaction.AttributeName.ImageId.ToString(),
                KeyType = KeyType.HASH
            }
        };


        static TransactionManager()
        {
            List<AttributeDefinition> definition = new List<AttributeDefinition> { new AttributeDefinition {
                AttributeName = Transaction.AttributeName.Txid.ToString(),
                AttributeType = ScalarAttributeType.S,
            }};
            definition.Sort(new AttributeDefinitionComparator());
            TransactionsTableAttributes = definition;

            definition = new List<AttributeDefinition> { new AttributeDefinition
            {
AttributeName = Transaction.AttributeName.ImageId.ToString(),
                AttributeType = ScalarAttributeType.S,
            }};
            definition.Sort(new AttributeDefinitionComparator());
            TransactionImagesTableAttributes = definition;
        }

        private readonly AmazonDynamoDBClient _client;
        private readonly string _transactionTableName;
        private readonly string _itemImageTableName;
        private readonly ConcurrentDictionary<string, List<KeySchemaElement>> _tableSchemaCache = new ConcurrentDictionary<string, List<KeySchemaElement>>();
        private readonly DynamoDBContext _clientMapper;
        private readonly ThreadLocalDynamoDbFacade _facadeProxy;
        private readonly ReadUncommittedIsolationHandlerImpl _readUncommittedIsolationHandler;
        private readonly ReadCommittedIsolationHandlerImpl _readCommittedIsolationHandler;

        public TransactionManager(AmazonDynamoDBClient client, string transactionTableName, string itemImageTableName) : this(client, transactionTableName, itemImageTableName, new DynamoDBContextConfig())
        {
        }

        public TransactionManager(AmazonDynamoDBClient client, string transactionTableName, string itemImageTableName, DynamoDBContextConfig config)
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
            this._client = client;
            this._transactionTableName = transactionTableName;
            this._itemImageTableName = itemImageTableName;
            this._facadeProxy = new ThreadLocalDynamoDbFacade();
            this._clientMapper = new DynamoDBContext(_facadeProxy, config);
            this._readUncommittedIsolationHandler = new ReadUncommittedIsolationHandlerImpl();
            this._readCommittedIsolationHandler = new ReadCommittedIsolationHandlerImpl(this);
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: protected java.util.List<com.amazonaws.services.dynamodbv2.model.KeySchemaElement> GetTableSchemaAsync(String tableName) throws com.amazonaws.services.dynamodbv2.model.ResourceNotFoundException
        protected internal virtual async Task<List<KeySchemaElement>> GetTableSchemaAsync(string tableName, CancellationToken cancellationToken = default(CancellationToken))
        {
            List<KeySchemaElement> schema = _tableSchemaCache[tableName];
            if (schema == null)
            {
                DescribeTableResponse result = await _client.DescribeTableAsync(new DescribeTableRequest
                {
                    TableName = tableName,
                }, cancellationToken);
                schema = result.Table.KeySchema;
                _tableSchemaCache[tableName] = schema;
            }
            return schema;
        }

        //JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
        //ORIGINAL LINE: protected java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> createKeyMap(final String tableName, final java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> item)
        protected internal virtual async Task<Dictionary<string, AttributeValue>> CreateKeyMapAsync(string tableName, Dictionary<string, AttributeValue> item, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.ReferenceEquals(tableName, null))
            {
                throw new System.ArgumentException("must specify a tableName");
            }
            if (item == null)
            {
                throw new System.ArgumentException("must specify an item");
            }
            List<KeySchemaElement> schema = await GetTableSchemaAsync(tableName, cancellationToken);
            Dictionary<string, AttributeValue> key = new Dictionary<string, AttributeValue>(schema.Count);
            foreach (KeySchemaElement element in schema)
            {
                key[element.AttributeName] = item[element.AttributeName];
            }
            return key;
        }

        public virtual Transaction NewTransaction()
        {
            Transaction transaction = new Transaction(Guid.NewGuid().ToString(), this, true);
            Log.Info("Started transaction " + transaction.Id);
            return transaction;
        }

        public virtual Transaction ResumeTransaction(string txId)
        {
            Transaction transaction = new Transaction(txId, this, false);
            Log.Info("Resuming transaction from id " + transaction.Id);
            return transaction;
        }

        public virtual Transaction ResumeTransaction(Dictionary<string, AttributeValue> txItem)
        {
            Transaction transaction = new Transaction(txItem, this);
            Log.Info("Resuming transaction from item " + transaction.Id);
            return transaction;
        }

        public static bool IsTransactionItem(Dictionary<string, AttributeValue> txItem)
        {
            return TransactionItem.IsTransactionItem(txItem);
        }

        public virtual AmazonDynamoDBClient Client
        {
            get
            {
                return _client;
            }
        }

        public virtual DynamoDBContext ClientMapper
        {
            get
            {
                return _clientMapper;
            }
        }

        protected internal virtual ThreadLocalDynamoDbFacade FacadeProxy
        {
            get
            {
                return _facadeProxy;
            }
        }

        protected internal virtual IReadIsolationHandler getReadIsolationHandler(Transaction.IsolationLevel isolationLevel)
        {
            if (isolationLevel == null)
            {
                throw new System.ArgumentException("isolation level is required");
            }
            switch (isolationLevel)
            {
                case Transaction.IsolationLevel.Uncommitted:
                    return _readUncommittedIsolationHandler;
                case Transaction.IsolationLevel.Committed:
                    return _readCommittedIsolationHandler;
                case Transaction.IsolationLevel.ReadLock:
                    throw new System.ArgumentException("Cannot callAsync GetItemAsync at the READ_LOCK isolation level outside of a transaction. Call GetItemAsync on a transaction directly instead.");
                default:
                    throw new System.ArgumentException("Unrecognized isolation level: " + isolationLevel);
            }
        }

        public virtual async Task<GetItemResponse> GetItemAsync(GetItemRequest request, Transaction.IsolationLevel isolationLevel, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (request.AttributesToGet != null)
            {
                ISet<string> attributesToGet = new HashSet<string>(request.AttributesToGet);
                attributesToGet.UnionWith(Transaction.SpecialAttrNames);
                request.AttributesToGet = attributesToGet.ToList();
            }
            GetItemResponse result = await Client.GetItemAsync(request, cancellationToken);
            Dictionary<string, AttributeValue> item = await getReadIsolationHandler(isolationLevel).HandleItemAsync(result.Item, request.AttributesToGet, request.TableName, cancellationToken);
            Transaction.StripSpecialAttributes(item);
            result.Item = item;
            return result;
        }

        public virtual string TransactionTableName
        {
            get
            {
                return _transactionTableName;
            }
        }

        public virtual string ItemImageTableName
        {
            get
            {
                return _itemImageTableName;
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
        public virtual void BreakLock(string tableName, Dictionary<string, AttributeValue> item, string txId)
        {
            if (Log.WarnEnabled)
            {
                Log.Warn("Breaking a lock on table " + tableName + " for transaction " + txId + " for item " + item + ".  This will leave the item in an unknown state");
            }
            Transaction.UnlockItemUnsafeAsync(this, tableName, item, txId);
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public static void verifyOrCreateTransactionTable(com.amazonaws.services.dynamodbv2.AmazonDynamoDBClient client, String tableName, long readCapacityUnits, long writeCapacityUnits, Nullable<long> waitTimeSeconds) throws InterruptedException
        public static async Task VerifyOrCreateTransactionTableAsync(AmazonDynamoDBClient client, string tableName, long readCapacityUnits, long writeCapacityUnits, long? waitTimeSeconds)
        {
            await (new TableHelper(client)).VerifyOrCreateTableAsync(tableName, TransactionsTableAttributes, TransactionsTableKeySchema, null, new ProvisionedThroughput
            {
                ReadCapacityUnits = readCapacityUnits,
                WriteCapacityUnits = writeCapacityUnits,
            }, waitTimeSeconds);
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public static void verifyOrCreateTransactionImagesTableAsync(com.amazonaws.services.dynamodbv2.AmazonDynamoDBClient client, String tableName, long readCapacityUnits, long writeCapacityUnits, Nullable<long> waitTimeSeconds) throws InterruptedException
        public static async Task VerifyOrCreateTransactionImagesTableAsync(AmazonDynamoDBClient client, string tableName, long readCapacityUnits, long writeCapacityUnits, long? waitTimeSeconds)
        {
            await (new TableHelper(client)).VerifyOrCreateTableAsync(tableName, TransactionImagesTableAttributes, TransactionImagesTableKeySchema, null, new ProvisionedThroughput
            {
                ReadCapacityUnits = readCapacityUnits,
                WriteCapacityUnits = writeCapacityUnits,
            }, waitTimeSeconds);
        }

        /// <summary>
        /// Ensures that the transaction table exists and has the correct schema.
        /// </summary>
        /// <param name="client"> </param>
        /// <param name="transactionTableName"> </param>
        /// <param name="transactionImagesTableName"> </param>
        /// <exception cref="ResourceInUseException"> if the table exists but has the wrong schema </exception>
        /// <exception cref="ResourceNotFoundException"> if the table does not exist </exception>
        public static async Task VerifyTransactionTablesExistAsync(AmazonDynamoDBClient client, string transactionTableName, string transactionImagesTableName)
        {
            string state = await (new TableHelper(client)).VerifyTableExistsAsync(transactionTableName, TransactionsTableAttributes, TransactionsTableKeySchema, null);
            if (!"ACTIVE".Equals(state))
            {
                throw new ResourceInUseException("Table " + transactionTableName + " is not ACTIVE");
            }

            state = await (new TableHelper(client)).VerifyTableExistsAsync(transactionImagesTableName, TransactionImagesTableAttributes, TransactionImagesTableKeySchema, null);
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
                return new AttributeValue { N = new double?(CurrentTime).ToString() };
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

                int comp = String.Compare(arg0.AttributeName, arg1.AttributeName, StringComparison.Ordinal);
                if (comp != 0)
                {
                    return comp;
                }

                comp = arg0.AttributeType.Value.CompareTo(arg1.AttributeType.Value);
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
        public virtual async Task<T> LoadAsync<T>(T item, Transaction.IsolationLevel isolationLevel)
        {
            try
            {
                FacadeProxy.Backend = new TransactionManagerDynamoDbFacade(this, isolationLevel);
                return await ClientMapper.LoadAsync<T>(item);
            }
            finally
            {
                FacadeProxy.Backend = null;
            }
        }
    }

}