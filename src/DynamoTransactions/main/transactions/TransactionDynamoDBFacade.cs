using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
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
    /// Facade for <seealso cref="AmazonDynamoDBClient"/> that forwards requests to a
    /// <seealso cref="Transaction"/>, omitting conditional checks and consistent read options
    /// from each request. Only supports the operations needed by DynamoDBMapper for
    /// loading, saving or deleting items.
    /// </summary>
    public class TransactionDynamoDBFacade : IAmazonDynamoDB
    {
        private readonly Transaction txn;
        private readonly TransactionManager txManager;

        public TransactionDynamoDBFacade(Transaction txn, TransactionManager txManager)
        {
            this.txn = txn;
            this.txManager = txManager;
        }

        public async Task<DeleteItemResponse> DeleteItemAsync(DeleteItemRequest request, CancellationToken cancellationToken)
        {
            Dictionary<string, ExpectedAttributeValue> expectedValues = request.Expected;
            await checkExpectedValuesAsync(request.TableName, request.Key, expectedValues, cancellationToken);

            // conditional checks are handled by the above callAsync
            request.Expected = null;
            return await txn.deleteItemAsync(request);
        }

        public Task<DeleteTableResponse> DeleteTableAsync(string tableName, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<DeleteTableResponse> DeleteTableAsync(DeleteTableRequest request, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<DescribeLimitsResponse> DescribeLimitsAsync(DescribeLimitsRequest request, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<DescribeTableResponse> DescribeTableAsync(string tableName, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<DescribeTableResponse> DescribeTableAsync(DescribeTableRequest request, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public async Task<GetItemResponse> GetItemAsync(GetItemRequest request, CancellationToken cancellationToken)
        {
            return await txn.getItemAsync(request);
        }

        public Task<ListTablesResponse> ListTablesAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<ListTablesResponse> ListTablesAsync(string exclusiveStartTableName, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<ListTablesResponse> ListTablesAsync(string exclusiveStartTableName, int limit,
            CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<ListTablesResponse> ListTablesAsync(int limit, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<ListTablesResponse> ListTablesAsync(ListTablesRequest request, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public async Task<PutItemResponse> PutItemAsync(PutItemRequest request, CancellationToken cancellationToken)
        {
            Dictionary<string, ExpectedAttributeValue> expectedValues = request.Expected;
            checkExpectedValuesAsync(request.TableName, Request.getKeyFromItemAsync(request.TableName, request.Item, txManager), expectedValues, cancellationToken);

            // conditional checks are handled by the above callAsync
            request.Expected = null;
            return await txn.putItemAsync(request);
        }

        public Task<QueryResponse> QueryAsync(QueryRequest request, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<ScanResponse> ScanAsync(string tableName, List<string> attributesToGet, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<ScanResponse> ScanAsync(string tableName, Dictionary<string, Condition> scanFilter, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<ScanResponse> ScanAsync(string tableName, List<string> attributesToGet, Dictionary<string, Condition> scanFilter,
            CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<ScanResponse> ScanAsync(ScanRequest request, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public async Task<UpdateItemResponse> UpdateItemAsync(UpdateItemRequest request, CancellationToken cancellationToken)
        {
            Dictionary<string, ExpectedAttributeValue> expectedValues = request.Expected;
            checkExpectedValuesAsync(request.TableName, request.Key, expectedValues, cancellationToken);

            // conditional checks are handled by the above callAsync
            request.Expected = null;
            return await txn.updateItemAsync(request);
        }

        public Task<UpdateTableResponse> UpdateTableAsync(string tableName, ProvisionedThroughput provisionedThroughput,
            CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<UpdateTableResponse> UpdateTableAsync(UpdateTableRequest request, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        private async Task checkExpectedValuesAsync(string tableName, Dictionary<string, AttributeValue> itemKey, Dictionary<string, ExpectedAttributeValue> expectedValues, CancellationToken cancellationToken)
        {
            if (expectedValues != null && expectedValues.Count > 0)
            {
                foreach (KeyValuePair<string, ExpectedAttributeValue> entry in expectedValues.SetOfKeyValuePairs())
                {
                    if ((entry.Value.Exists == null || entry.Value.Exists == true) && entry.Value.Value == null)
                    {
                        throw new System.ArgumentException("An explicit value is required when Exists is null or true, " + "but none was found in expected values for item with key " + itemKey + ": " + expectedValues);
                    }
                }

                // simulate by loading the item and checking the values;
                // this also has the effect of locking the item, which gives the
                // same behavior
                GetItemResponse result = await GetItemAsync(new GetItemRequest
                {
                    AttributesToGet = expectedValues.Keys.Select(x => x).ToList(),
                    Key = itemKey,
                    TableName = tableName
                }, cancellationToken);
                Dictionary<string, AttributeValue> item = result.Item;
                try
                {
                    checkExpectedValuesAsync(expectedValues, item);
                }
                catch (ConditionalCheckFailedException e)
                {
                    throw new ConditionalCheckFailedException("Item " + itemKey + " had unexpected attributes: " + e.Message);
                }
            }
        }

        /// <summary>
        /// Checks a map of expected values against a map of actual values in a way
        /// that's compatible with how the DynamoDB service interprets the Expected
        /// parameter of PutItem, UpdateItem and DeleteItem.
        /// </summary>
        /// <param name="expectedValues">
        ///            A description of the expected values. </param>
        /// <param name="item">
        ///            The actual values. </param>
        /// <exception cref="ConditionalCheckFailedException">
        ///             Thrown if the values do not match the expected values. </exception>
        public static async Task checkExpectedValuesAsync(Dictionary<string, ExpectedAttributeValue> expectedValues, Dictionary<string, AttributeValue> item)
        {
            foreach (KeyValuePair<string, ExpectedAttributeValue> entry in expectedValues.SetOfKeyValuePairs())
            {
                // if the attribute is expected to exist (null for isExists means
                // true)
                if ((entry.Value.Exists == null || entry.Value.Exists == true) && (item == null || item[entry.Key] == null || !expectedValueMatches(entry.Value.Value, item[entry.Key])))
                // but the item doesn't
                // or the attribute doesn't
                // or it doesn't have the expected value
                {
                    throw new ConditionalCheckFailedException("expected attribute(s) " + expectedValues + " but found " + item);
                }
                else if (entry.Value.Exists != null && !entry.Value.Exists && item != null && item[entry.Key] != null)
                {
                    // the attribute isn't expected to exist, but the item exists
                    // and the attribute does too
                    throw new ConditionalCheckFailedException("expected attribute(s) " + expectedValues + " but found " + item);
                }
            }
        }

        private static bool expectedValueMatches(AttributeValue expected, AttributeValue actual)
        {
            if (expected.N != null)
            {
                return actual.N != null && (decimal.Parse(expected.N)).CompareTo(decimal.Parse(actual.N)) == 0;
            }
            else if (expected.S != null || expected.B != null)
            {
                return expected.Equals(actual);
            }
            else
            {
                throw new System.ArgumentException("Expect condition using unsupported value type: " + expected);
            }
        }

        public async Task<GetItemResponse> GetItemAsync(string tableName, Dictionary<string, AttributeValue> key, CancellationToken cancellationToken)
        {
            return await GetItemAsync(new GetItemRequest
            {
                TableName = tableName,
                Key = key
            }, cancellationToken);
        }

        public async Task<GetItemResponse> GetItemAsync(string tableName, Dictionary<string, AttributeValue> key, bool consistentRead, CancellationToken cancellationToken)
        {
            return await GetItemAsync(new GetItemRequest
            {
                TableName = tableName,
                Key = key,
                ConsistentRead = consistentRead
            }, cancellationToken);
        }

        public Task<BatchGetItemResponse> BatchGetItemAsync(Dictionary<string, KeysAndAttributes> requestItems, ReturnConsumedCapacity returnConsumedCapacity,
            CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<BatchGetItemResponse> BatchGetItemAsync(Dictionary<string, KeysAndAttributes> requestItems, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<BatchGetItemResponse> BatchGetItemAsync(BatchGetItemRequest request, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<BatchWriteItemResponse> BatchWriteItemAsync(Dictionary<string, List<WriteRequest>> requestItems, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<BatchWriteItemResponse> BatchWriteItemAsync(BatchWriteItemRequest request, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<CreateTableResponse> CreateTableAsync(string tableName, List<KeySchemaElement> keySchema, List<AttributeDefinition> attributeDefinitions,
            ProvisionedThroughput provisionedThroughput, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<CreateTableResponse> CreateTableAsync(CreateTableRequest request, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public async Task<DeleteItemResponse> DeleteItemAsync(string tableName, Dictionary<string, AttributeValue> key, CancellationToken cancellationToken)
        {
            return await DeleteItemAsync(new DeleteItemRequest
            {
                TableName = tableName,
                Key = key
            }, cancellationToken);
        }

        public async Task<DeleteItemResponse> DeleteItemAsync(string tableName, Dictionary<string, AttributeValue> key, ReturnValue returnValues, CancellationToken cancellationToken)
        {
            return await DeleteItemAsync(new DeleteItemRequest
            {
                TableName = tableName,
                Key = key,
                ReturnValues = returnValues
            }, cancellationToken);
        }

        public async Task<PutItemResponse> PutItemAsync(string tableName, Dictionary<string, AttributeValue> item, CancellationToken cancellationToken)
        {
            return await PutItemAsync(new PutItemRequest
            {
                TableName = tableName,
                Item = item
            }, cancellationToken);
        }

        public async Task<PutItemResponse> PutItemAsync(string tableName, Dictionary<string, AttributeValue> item, ReturnValue returnValues, CancellationToken cancellationToken)
        {
            return await PutItemAsync(new PutItemRequest
            {
                TableName = tableName,
                Item = item,
                ReturnValues = returnValues
            }, cancellationToken);
        }

        public async Task<UpdateItemResponse> UpdateItemAsync(string tableName, Dictionary<string, AttributeValue> key, Dictionary<string, AttributeValueUpdate> attributeUpdates, CancellationToken cancellationToken)
        {
            return await UpdateItemAsync(new UpdateItemRequest
            {
                TableName = tableName,
                Key = key,
                AttributeUpdates = attributeUpdates
            }, cancellationToken);
        }

        public async Task<UpdateItemResponse> UpdateItemAsync(string tableName, Dictionary<string, AttributeValue> key, Dictionary<string, AttributeValueUpdate> attributeUpdates, ReturnValue returnValues, CancellationToken cancellationToken)
        {
            return await UpdateItemAsync(new UpdateItemRequest
            {
                TableName = tableName,
                Key = key,
                AttributeUpdates = attributeUpdates,
                ReturnValues = returnValues
            }, cancellationToken);
        }

        public IClientConfig Config { get; }
        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}