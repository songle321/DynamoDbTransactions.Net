using System.Collections.Generic;
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
    public class TransactionManagerDynamoDBFacade : IAmazonDynamoDB
    {
        private readonly TransactionManager txManager;
        private readonly Transaction.IsolationLevel isolationLevel;
        private readonly ReadIsolationHandler isolationHandler;

        public TransactionManagerDynamoDBFacade(TransactionManager txManager, Transaction.IsolationLevel isolationLevel)
        {
            this.txManager = txManager;
            this.isolationLevel = isolationLevel;
            this.isolationHandler = txManager.getReadIsolationHandler(isolationLevel);
        }

        /// <summary>
        /// Returns versions of the items can be read at the specified isolation level stripped of
        /// special attributes. </summary>
        /// <param name="items"> The items to check </param>
        /// <param name="tableName"> The table that contains the item </param>
        /// <param name="attributesToGet"> The attributes to get from the table. If null or empty, will
        ///                        fetch all attributes. </param>
        /// <returns> Versions of the items that can be read at the isolation level stripped of special attributes </returns>
        //JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
        //ORIGINAL LINE: private java.util.List<java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue>> handleItems(final java.util.List<java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue>> items, final String tableName, final java.util.List<String> attributesToGet)
        private List<Dictionary<string, AttributeValue>> handleItems(List<Dictionary<string, AttributeValue>> items, string tableName, List<string> attributesToGet)
        {
            List<Dictionary<string, AttributeValue>> result = new List<Dictionary<string, AttributeValue>>();
            foreach (Dictionary<string, AttributeValue> item in items)
            {
                Dictionary<string, AttributeValue> handledItem = isolationHandler.HandleItemAsync(item, attributesToGet, tableName);
                /// <summary>
                /// If the item is null, BatchGetItems, Scan, and Query should exclude the item from
                /// the returned list. This is based on the DynamoDB documentation.
                /// </summary>
                if (handledItem != null)
                {
                    Transaction.stripSpecialAttributes(handledItem);
                    result.Add(handledItem);
                }
            }
            return result;
        }

        private ICollection<string> addSpecialAttributes(ICollection<string> attributesToGet)
        {
            if (attributesToGet == null)
            {
                return null;
            }
            ISet<string> result = new HashSet<string>(attributesToGet);
            result.addAll(Transaction.SPECIAL_ATTR_NAMES);
            return result;
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.GetItemResponse GetItemAsync(com.amazonaws.services.dynamodbv2.model.GetItemRequest request) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
        public async Task<GetItemResponse> GetItemAsync(GetItemRequest request, CancellationToken cancellationToken)
        {
            return txManager.GetItemAsync(request, isolationLevel);
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.GetItemResponse GetItemAsync(String tableName, java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> key) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
        public override GetItemResponse getItem(string tableName, Dictionary<string, AttributeValue> key)
        {
            return getItem(new GetItemRequest
            {
                TableName = tableName,
                Key = key,
            });
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.GetItemResponse GetItemAsync(String tableName, java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> key, Nullable<bool> consistentRead) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
        public override GetItemResponse getItem(string tableName, Dictionary<string, AttributeValue> key, bool? consistentRead)
        {
            return getItem(new GetItemRequest
            {
                TableName = tableName,
                Key = key,
                ConsistentRead = consistentRead,
            });
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.BatchGetItemResponse batchGetItem(com.amazonaws.services.dynamodbv2.model.BatchGetItemRequest request) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
        public override BatchGetItemResponse batchGetItem(BatchGetItemRequest request)
        {
            foreach (KeysAndAttributes keysAndAttributes in request.RequestItems.values())
            {
                ICollection<string> attributesToGet = keysAndAttributes.AttributesToGet;
                keysAndAttributes.AttributesToGet = addSpecialAttributes(attributesToGet);
            }
            BatchGetItemResponse result = txManager.Client.batchGetItem(request);
            Dictionary<string, List<Dictionary<string, AttributeValue>>> responses = new Dictionary<string, List<Dictionary<string, AttributeValue>>>();
            foreach (KeyValuePair<string, List<Dictionary<string, AttributeValue>>> e in result.Responses.entrySet())
            {
                string tableName = e.Key;
                List<string> attributesToGet = request.RequestItems.get(tableName).AttributesToGet;
                List<Dictionary<string, AttributeValue>> items = handleItems(e.Value, tableName, attributesToGet);
                responses[tableName] = items;
            }
            result.Responses = responses;
            return result;
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.BatchGetItemResponse batchGetItem(java.util.Map<String, com.amazonaws.services.dynamodbv2.model.KeysAndAttributes> requestItems, String returnConsumedCapacity) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
        public override BatchGetItemResponse batchGetItem(Dictionary<string, KeysAndAttributes> requestItems, string returnConsumedCapacity)
        {
            BatchGetItemRequest request = new BatchGetItemRequest
            {
                RequestItems = requestItems,
                ReturnConsumedCapacity = returnConsumedCapacity
            };
            return batchGetItem(request);
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.BatchGetItemResponse batchGetItem(java.util.Map<String, com.amazonaws.services.dynamodbv2.model.KeysAndAttributes> requestItems) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
        public override BatchGetItemResponse batchGetItem(Dictionary<string, KeysAndAttributes> requestItems)
        {
            BatchGetItemRequest request = new BatchGetItemRequest
            {
                RequestItems = requestItems
            };
            return batchGetItem(request);
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.ScanResponse scan(com.amazonaws.services.dynamodbv2.model.ScanRequest request) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
        public override ScanResponse scan(ScanRequest request)
        {
            ICollection<string> attributesToGet = addSpecialAttributes(request.AttributesToGet);
            request.AttributesToGet = attributesToGet;
            ScanResponse result = txManager.Client.scan(request);
            List<Dictionary<string, AttributeValue>> items = handleItems(result.Items, request.TableName, request.AttributesToGet);
            result.Items = items;
            return result;
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.ScanResponse scan(String tableName, java.util.List<String> attributesToGet) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
        public override ScanResponse scan(string tableName, List<string> attributesToGet)
        {
            ScanRequest request = new ScanRequest
            {
                TableName = tableName,
                AttributesToGet = attributesToGet
            };
            return scan(request);
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.ScanResponse scan(String tableName, java.util.Map<String, com.amazonaws.services.dynamodbv2.model.Condition> scanFilter) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
        public override ScanResponse scan(string tableName, Dictionary<string, Condition> scanFilter)
        {
            ScanRequest request = new ScanRequest
            {
                TableName = tableName,
                ScanFilter = scanFilter
            };
            return scan(request);
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.ScanResponse scan(String tableName, java.util.List<String> attributesToGet, java.util.Map<String, com.amazonaws.services.dynamodbv2.model.Condition> scanFilter) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
        public override ScanResponse scan(string tableName, List<string> attributesToGet, Dictionary<string, Condition> scanFilter)
        {
            ScanRequest request = new ScanRequest
            {
                TableName = tableName,
                AttributesToGet = attributesToGet,
                ScanFilter = scanFilter
            };
            return scan(request);
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.QueryResponse query(com.amazonaws.services.dynamodbv2.model.QueryRequest request) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
        public override QueryResponse query(QueryRequest request)
        {
            ICollection<string> attributesToGet = addSpecialAttributes(request.AttributesToGet);
            request.AttributesToGet = attributesToGet;
            QueryResponse result = txManager.Client.query(request);
            List<Dictionary<string, AttributeValue>> items = handleItems(result.Items, request.TableName, request.AttributesToGet);
            result.Items = items;
            return result;
        }
    }

}