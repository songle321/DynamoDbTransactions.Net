using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;

// <summary>
// Copyright 2013-2014 Amazon.com, Inc. or its affiliates. All Rights Reserved.
// 
// Licensed under the Amazon Software License (the "License"). You may not use
// this file except in compliance with the License. A copy of the License is
// located at
// 
// http://aws.amazon.com/asl/
// 
// or in the "license" file accompanying this file. This file is distributed on
// an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, express or
// implied. See the License for the specific language governing permissions and
// limitations under the License.
// </summary>
namespace com.amazonaws.services.dynamodbv2.transactions
{
    /// <summary>
    /// Facade to support the DynamoDBMapper doing a read using a specific isolation
    /// level. Used by <seealso cref="TransactionManager#loadAsync(Object, IsolationLevel)"/>.
    /// </summary>
    public class TransactionManagerDynamoDbFacade : IAmazonDynamoDB
    {
        private readonly TransactionManager _txManager;
        private readonly Transaction.IsolationLevel _isolationLevel;
        private readonly IReadIsolationHandler _isolationHandler;

        public IClientConfig Config
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public TransactionManagerDynamoDbFacade(TransactionManager txManager, Transaction.IsolationLevel isolationLevel)
        {
            this._txManager = txManager;
            this._isolationLevel = isolationLevel;
            this._isolationHandler = txManager.getReadIsolationHandler(isolationLevel);
        }

        /// <summary>
        /// Returns versions of the items can be read at the specified isolation level stripped of
        /// special attributes. </summary>
        /// <param name="items"> The items to check </param>
        /// <param name="tableName"> The table that contains the item </param>
        /// <param name="attributesToGet"> The attributes to get from the table. If null or empty, will
        ///     fetch all attributes. </param>
        /// <param name="cancellationToken"></param>
        /// <returns> Versions of the items that can be read at the isolation level stripped of special attributes </returns>
        private async Task<List<Dictionary<string, AttributeValue>>> HandleItemsAsync(List<Dictionary<string, AttributeValue>> items, string tableName, List<string> attributesToGet, CancellationToken cancellationToken = default(CancellationToken))
        {
            List<Dictionary<string, AttributeValue>> result = new List<Dictionary<string, AttributeValue>>();
            foreach (Dictionary<string, AttributeValue> item in items)
            {
                Dictionary<string, AttributeValue> handledItem = await _isolationHandler.HandleItemAsync(item, attributesToGet, tableName, cancellationToken);
                // <summary>
                // If the item is null, BatchGetItems, Scan, and Query should exclude the item from
                // the returned list. This is based on the DynamoDB documentation.
                // </summary>
                if (handledItem != null)
                {
                    Transaction.StripSpecialAttributes(handledItem);
                    result.Add(handledItem);
                }
            }
            return result;
        }

        private List<string> AddSpecialAttributes(ICollection<string> attributesToGet)
        {
            if (attributesToGet == null)
            {
                return null;
            }
            ISet<string> result = new HashSet<string>(attributesToGet);
            result.UnionWith(Transaction.SpecialAttrNames);
            return result.ToList();
        }

        public async Task<GetItemResponse> GetItemAsync(GetItemRequest request, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await _txManager.GetItemAsync(request, _isolationLevel, cancellationToken);
        }

        public async Task<GetItemResponse> GetItemAsync(string tableName, Dictionary<string, AttributeValue> key, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await GetItemAsync(new GetItemRequest
            {
                TableName = tableName,
                Key = key
            }, cancellationToken);
        }

        public async Task<GetItemResponse> GetItemAsync(string tableName, Dictionary<string, AttributeValue> key, bool consistentRead, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await GetItemAsync(new GetItemRequest
            {
                TableName = tableName,
                Key = key,
                ConsistentRead = consistentRead,
            }, cancellationToken);
        }

        public async Task<BatchGetItemResponse> BatchGetItemAsync(BatchGetItemRequest request, CancellationToken cancellationToken = default(CancellationToken))
        {
            foreach (KeysAndAttributes keysAndAttributes in request.RequestItems.Values)
            {
                ICollection<string> attributesToGet = keysAndAttributes.AttributesToGet;
                keysAndAttributes.AttributesToGet = AddSpecialAttributes(attributesToGet);
            }
            BatchGetItemResponse result = await _txManager.Client.BatchGetItemAsync(request);
            Dictionary<string, List<Dictionary<string, AttributeValue>>> responses = new Dictionary<string, List<Dictionary<string, AttributeValue>>>();
            foreach (KeyValuePair<string, List<Dictionary<string, AttributeValue>>> e in result.Responses)
            {
                string tableName = e.Key;
                List<string> attributesToGet = request.RequestItems[tableName].AttributesToGet;
                List<Dictionary<string, AttributeValue>> items = await HandleItemsAsync(e.Value, tableName, attributesToGet, cancellationToken);
                responses[tableName] = items;
            }
            result.Responses = responses;
            return result;
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.BatchGetItemResponse BatchGetItemAsync(java.util.Map<String, com.amazonaws.services.dynamodbv2.model.KeysAndAttributes> requestItems, String returnConsumedCapacity) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
        public async Task<BatchGetItemResponse> BatchGetItemAsync(Dictionary<string, KeysAndAttributes> requestItems, ReturnConsumedCapacity returnConsumedCapacity, CancellationToken cancellationToken = default(CancellationToken))
        {
            BatchGetItemRequest request = new BatchGetItemRequest
            {
                RequestItems = requestItems,
                ReturnConsumedCapacity = returnConsumedCapacity
            };
            return await BatchGetItemAsync(request, cancellationToken);
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.BatchGetItemResponse BatchGetItemAsync(java.util.Map<String, com.amazonaws.services.dynamodbv2.model.KeysAndAttributes> requestItems) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
        public async Task<BatchGetItemResponse> BatchGetItemAsync(Dictionary<string, KeysAndAttributes> requestItems, CancellationToken cancellationToken = default(CancellationToken))
        {
            BatchGetItemRequest request = new BatchGetItemRequest
            {
                RequestItems = requestItems
            };
            return await BatchGetItemAsync(request, cancellationToken);
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.ScanResponse scan(com.amazonaws.services.dynamodbv2.model.ScanRequest request) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
        public async Task<ScanResponse> ScanAsync(ScanRequest request, CancellationToken cancellationToken = default(CancellationToken))
        {
            List<string> attributesToGet = AddSpecialAttributes(request.AttributesToGet);
            request.AttributesToGet = attributesToGet;
            ScanResponse result = await _txManager.Client.ScanAsync(request);
            List<Dictionary<string, AttributeValue>> items = await HandleItemsAsync(result.Items, request.TableName, request.AttributesToGet, cancellationToken);
            result.Items = items;
            return result;
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.ScanResponse scan(String tableName, java.util.List<String> attributesToGet) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
        public async Task<ScanResponse> ScanAsync(string tableName, List<string> attributesToGet, CancellationToken cancellationToken = default(CancellationToken))
        {
            ScanRequest request = new ScanRequest
            {
                TableName = tableName,
                AttributesToGet = attributesToGet
            };
            return await ScanAsync(request, cancellationToken);
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.ScanResponse scan(String tableName, java.util.Map<String, com.amazonaws.services.dynamodbv2.model.Condition> scanFilter) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
        public async Task<ScanResponse> ScanAsync(string tableName, Dictionary<string, Condition> scanFilter, CancellationToken cancellationToken = default(CancellationToken))
        {
            ScanRequest request = new ScanRequest
            {
                TableName = tableName,
                ScanFilter = scanFilter
            };
            return await ScanAsync(request, cancellationToken);
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.ScanResponse scan(String tableName, java.util.List<String> attributesToGet, java.util.Map<String, com.amazonaws.services.dynamodbv2.model.Condition> scanFilter) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
        public async Task<ScanResponse> ScanAsync(string tableName, List<string> attributesToGet, Dictionary<string, Condition> scanFilter, CancellationToken cancellationToken = default(CancellationToken))
        {
            ScanRequest request = new ScanRequest
            {
                TableName = tableName,
                AttributesToGet = attributesToGet,
                ScanFilter = scanFilter
            };
            return await ScanAsync(request, cancellationToken);
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.QueryResponse QueryAsync(com.amazonaws.services.dynamodbv2.model.QueryRequest request) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
        public async Task<QueryResponse> QueryAsync(QueryRequest request, CancellationToken cancellationToken = default(CancellationToken))
        {
            List<string> attributesToGet = AddSpecialAttributes(request.AttributesToGet);
            request.AttributesToGet = attributesToGet;
            QueryResponse result = await _txManager.Client.QueryAsync(request);
            List<Dictionary<string, AttributeValue>> items = await HandleItemsAsync(result.Items, request.TableName, request.AttributesToGet, cancellationToken);
            result.Items = items;
            return result;
        }

        public Task<BatchWriteItemResponse> BatchWriteItemAsync(Dictionary<string, List<WriteRequest>> requestItems, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<BatchWriteItemResponse> BatchWriteItemAsync(BatchWriteItemRequest request, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<CreateTableResponse> CreateTableAsync(string tableName, List<KeySchemaElement> keySchema, List<AttributeDefinition> attributeDefinitions, ProvisionedThroughput provisionedThroughput, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<CreateTableResponse> CreateTableAsync(CreateTableRequest request, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<DeleteItemResponse> DeleteItemAsync(string tableName, Dictionary<string, AttributeValue> key, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<DeleteItemResponse> DeleteItemAsync(string tableName, Dictionary<string, AttributeValue> key, ReturnValue returnValues, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<DeleteItemResponse> DeleteItemAsync(DeleteItemRequest request, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<DeleteTableResponse> DeleteTableAsync(string tableName, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<DeleteTableResponse> DeleteTableAsync(DeleteTableRequest request, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<DescribeLimitsResponse> DescribeLimitsAsync(DescribeLimitsRequest request, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<DescribeTableResponse> DescribeTableAsync(string tableName, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<DescribeTableResponse> DescribeTableAsync(DescribeTableRequest request, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<ListTablesResponse> ListTablesAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<ListTablesResponse> ListTablesAsync(string exclusiveStartTableName, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<ListTablesResponse> ListTablesAsync(string exclusiveStartTableName, int limit, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<ListTablesResponse> ListTablesAsync(int limit, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<ListTablesResponse> ListTablesAsync(ListTablesRequest request, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<PutItemResponse> PutItemAsync(string tableName, Dictionary<string, AttributeValue> item, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<PutItemResponse> PutItemAsync(string tableName, Dictionary<string, AttributeValue> item, ReturnValue returnValues, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<PutItemResponse> PutItemAsync(PutItemRequest request, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<UpdateItemResponse> UpdateItemAsync(string tableName, Dictionary<string, AttributeValue> key, Dictionary<string, AttributeValueUpdate> attributeUpdates, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<UpdateItemResponse> UpdateItemAsync(string tableName, Dictionary<string, AttributeValue> key, Dictionary<string, AttributeValueUpdate> attributeUpdates, ReturnValue returnValues, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<UpdateItemResponse> UpdateItemAsync(UpdateItemRequest request, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<UpdateTableResponse> UpdateTableAsync(string tableName, ProvisionedThroughput provisionedThroughput, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<UpdateTableResponse> UpdateTableAsync(UpdateTableRequest request, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }

}