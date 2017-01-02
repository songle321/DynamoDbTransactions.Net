using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;

// <summary>
// Copyright 2014-2014 Amazon.com, Inc. or its affiliates. All Rights Reserved.
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
    /// Necessary to work around a limitation of the mapper. The mapper always gets
    /// created with a fresh reflection cache, which is expensive to repopulate.
    /// Using this class to route to a different facade for each request allows us to
    /// reuse the mapper and its underlying cache for each callAsync to the mapper from a
    /// transaction or the transaction manager.
    /// </summary>
    public class ThreadLocalDynamoDbFacade : IAmazonDynamoDB
    {
        private readonly ThreadLocal<IAmazonDynamoDB> _backend = new ThreadLocal<IAmazonDynamoDB>();

        public IClientConfig Config
        {
            get
            {
                return ((IAmazonDynamoDB)Backend).Config;
            }
        }

        internal IAmazonDynamoDB Backend
        {
            get
            {
                if (_backend.Value == null)
                {
                    throw new Exception("No backend to proxy");
                }
                return _backend.Value;
            }
            set
            {
                _backend.Value = value;
            }
        }

        public Task<BatchGetItemResponse> BatchGetItemAsync(BatchGetItemRequest request, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ((IAmazonDynamoDB)Backend).BatchGetItemAsync(request, cancellationToken);
        }

        public Task<BatchGetItemResponse> BatchGetItemAsync(Dictionary<string, KeysAndAttributes> requestItems, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ((IAmazonDynamoDB)Backend).BatchGetItemAsync(requestItems, cancellationToken);
        }

        public Task<BatchGetItemResponse> BatchGetItemAsync(Dictionary<string, KeysAndAttributes> requestItems, ReturnConsumedCapacity returnConsumedCapacity, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ((IAmazonDynamoDB)Backend).BatchGetItemAsync(requestItems, returnConsumedCapacity, cancellationToken);
        }

        public Task<BatchWriteItemResponse> BatchWriteItemAsync(BatchWriteItemRequest request, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ((IAmazonDynamoDB)Backend).BatchWriteItemAsync(request, cancellationToken);
        }

        public Task<BatchWriteItemResponse> BatchWriteItemAsync(Dictionary<string, List<WriteRequest>> requestItems, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ((IAmazonDynamoDB)Backend).BatchWriteItemAsync(requestItems, cancellationToken);
        }

        public Task<CreateTableResponse> CreateTableAsync(CreateTableRequest request, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ((IAmazonDynamoDB)Backend).CreateTableAsync(request, cancellationToken);
        }

        public Task<CreateTableResponse> CreateTableAsync(string tableName, List<KeySchemaElement> keySchema, List<AttributeDefinition> attributeDefinitions, ProvisionedThroughput provisionedThroughput, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ((IAmazonDynamoDB)Backend).CreateTableAsync(tableName, keySchema, attributeDefinitions, provisionedThroughput, cancellationToken);
        }

        public Task<DeleteItemResponse> DeleteItemAsync(DeleteItemRequest request, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ((IAmazonDynamoDB)Backend).DeleteItemAsync(request, cancellationToken);
        }

        public Task<DeleteItemResponse> DeleteItemAsync(string tableName, Dictionary<string, AttributeValue> key, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ((IAmazonDynamoDB)Backend).DeleteItemAsync(tableName, key, cancellationToken);
        }

        public Task<DeleteItemResponse> DeleteItemAsync(string tableName, Dictionary<string, AttributeValue> key, ReturnValue returnValues, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ((IAmazonDynamoDB)Backend).DeleteItemAsync(tableName, key, returnValues, cancellationToken);
        }

        public Task<DeleteTableResponse> DeleteTableAsync(DeleteTableRequest request, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ((IAmazonDynamoDB)Backend).DeleteTableAsync(request, cancellationToken);
        }

        public Task<DeleteTableResponse> DeleteTableAsync(string tableName, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ((IAmazonDynamoDB)Backend).DeleteTableAsync(tableName, cancellationToken);
        }

        public Task<DescribeLimitsResponse> DescribeLimitsAsync(DescribeLimitsRequest request, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ((IAmazonDynamoDB)Backend).DescribeLimitsAsync(request, cancellationToken);
        }

        public Task<DescribeTableResponse> DescribeTableAsync(DescribeTableRequest request, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ((IAmazonDynamoDB)Backend).DescribeTableAsync(request, cancellationToken);
        }

        public Task<DescribeTableResponse> DescribeTableAsync(string tableName, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ((IAmazonDynamoDB)Backend).DescribeTableAsync(tableName, cancellationToken);
        }

        public void Dispose()
        {
            ((IAmazonDynamoDB)Backend).Dispose();
        }

        public Task<GetItemResponse> GetItemAsync(GetItemRequest request, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ((IAmazonDynamoDB)Backend).GetItemAsync(request, cancellationToken);
        }

        public Task<GetItemResponse> GetItemAsync(string tableName, Dictionary<string, AttributeValue> key, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ((IAmazonDynamoDB)Backend).GetItemAsync(tableName, key, cancellationToken);
        }

        public Task<GetItemResponse> GetItemAsync(string tableName, Dictionary<string, AttributeValue> key, bool consistentRead, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ((IAmazonDynamoDB)Backend).GetItemAsync(tableName, key, consistentRead, cancellationToken);
        }

        public Task<ListTablesResponse> ListTablesAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return ((IAmazonDynamoDB)Backend).ListTablesAsync(cancellationToken);
        }

        public Task<ListTablesResponse> ListTablesAsync(int limit, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ((IAmazonDynamoDB)Backend).ListTablesAsync(limit, cancellationToken);
        }

        public Task<ListTablesResponse> ListTablesAsync(ListTablesRequest request, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ((IAmazonDynamoDB)Backend).ListTablesAsync(request, cancellationToken);
        }

        public Task<ListTablesResponse> ListTablesAsync(string exclusiveStartTableName, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ((IAmazonDynamoDB)Backend).ListTablesAsync(exclusiveStartTableName, cancellationToken);
        }

        public Task<ListTablesResponse> ListTablesAsync(string exclusiveStartTableName, int limit, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ((IAmazonDynamoDB)Backend).ListTablesAsync(exclusiveStartTableName, limit, cancellationToken);
        }

        public Task<PutItemResponse> PutItemAsync(PutItemRequest request, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ((IAmazonDynamoDB)Backend).PutItemAsync(request, cancellationToken);
        }

        public Task<PutItemResponse> PutItemAsync(string tableName, Dictionary<string, AttributeValue> item, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ((IAmazonDynamoDB)Backend).PutItemAsync(tableName, item, cancellationToken);
        }

        public Task<PutItemResponse> PutItemAsync(string tableName, Dictionary<string, AttributeValue> item, ReturnValue returnValues, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ((IAmazonDynamoDB)Backend).PutItemAsync(tableName, item, returnValues, cancellationToken);
        }

        public Task<QueryResponse> QueryAsync(QueryRequest request, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ((IAmazonDynamoDB)Backend).QueryAsync(request, cancellationToken);
        }

        public Task<ScanResponse> ScanAsync(ScanRequest request, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ((IAmazonDynamoDB)Backend).ScanAsync(request, cancellationToken);
        }

        public Task<ScanResponse> ScanAsync(string tableName, Dictionary<string, Condition> scanFilter, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ((IAmazonDynamoDB)Backend).ScanAsync(tableName, scanFilter, cancellationToken);
        }

        public Task<ScanResponse> ScanAsync(string tableName, List<string> attributesToGet, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ((IAmazonDynamoDB)Backend).ScanAsync(tableName, attributesToGet, cancellationToken);
        }

        public Task<ScanResponse> ScanAsync(string tableName, List<string> attributesToGet, Dictionary<string, Condition> scanFilter, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ((IAmazonDynamoDB)Backend).ScanAsync(tableName, attributesToGet, scanFilter, cancellationToken);
        }

        public Task<UpdateItemResponse> UpdateItemAsync(UpdateItemRequest request, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ((IAmazonDynamoDB)Backend).UpdateItemAsync(request, cancellationToken);
        }

        public Task<UpdateItemResponse> UpdateItemAsync(string tableName, Dictionary<string, AttributeValue> key, Dictionary<string, AttributeValueUpdate> attributeUpdates, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ((IAmazonDynamoDB)Backend).UpdateItemAsync(tableName, key, attributeUpdates, cancellationToken);
        }

        public Task<UpdateItemResponse> UpdateItemAsync(string tableName, Dictionary<string, AttributeValue> key, Dictionary<string, AttributeValueUpdate> attributeUpdates, ReturnValue returnValues, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ((IAmazonDynamoDB)Backend).UpdateItemAsync(tableName, key, attributeUpdates, returnValues, cancellationToken);
        }

        public Task<UpdateTableResponse> UpdateTableAsync(UpdateTableRequest request, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ((IAmazonDynamoDB)Backend).UpdateTableAsync(request, cancellationToken);
        }

        public Task<UpdateTableResponse> UpdateTableAsync(string tableName, ProvisionedThroughput provisionedThroughput, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ((IAmazonDynamoDB)Backend).UpdateTableAsync(tableName, provisionedThroughput, cancellationToken);
        }
    }

}