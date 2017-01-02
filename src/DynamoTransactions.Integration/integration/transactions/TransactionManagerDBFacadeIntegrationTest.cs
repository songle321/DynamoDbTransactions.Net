using System.Collections.Generic;
using System.Threading;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using static DynamoTransactions.Integration.AssertStatic;

/// <summary>
/// Copyright 2013-2016 Amazon.com, Inc. or its affiliates. All Rights Reserved.
/// 
/// Licensed under the Amazon Software License (the "License").
/// You may not use this file except in compliance with the License.
/// A copy of the License is located at
/// 
///  http://aws.amazon.com/asl/
/// 
/// or in the "license" file accompanying this file. This file is distributed
/// on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, express
/// or implied. See the License for the specific language governing permissions
/// and limitations under the License.
/// </summary>

namespace com.amazonaws.services.dynamodbv2.transactions
{
    //JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to C#:
    //	import static org.junit.Assert.assertEquals;
    //JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to C#:
    //	import static org.junit.Assert.assertFalse;
    //JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to C#:
    //	import static org.junit.Assert.assertNotNull;

    public class TransactionManagerDbFacadeIntegrationTest : IntegrationTest
    {

        private TransactionManagerDynamoDbFacade _uncommittedFacade;
        private TransactionManagerDynamoDbFacade _committedFacade;

        private Dictionary<string, AttributeValueUpdate> _update;
        private Dictionary<string, AttributeValue> _item0Updated;
        private Dictionary<string, AttributeValue> _item0Filtered; // item0 with only the attributesToGet
        private List<string> _attributesToGet;

        public TransactionManagerDbFacadeIntegrationTest() : base()
        {
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Before public void setup()
        public virtual void Setup()
        {
            Dynamodb.Reset();
            _uncommittedFacade = new TransactionManagerDynamoDbFacade(Manager, Transaction.IsolationLevel.Uncommitted);
            _committedFacade = new TransactionManagerDynamoDbFacade(Manager, Transaction.IsolationLevel.Committed);
            Key0 = NewKey(IntegHashTableName);
            Item0 = new Dictionary<string, AttributeValue>(Key0);
            Item0.Add("s_someattr", new AttributeValue("val"));
            _item0Filtered = new Dictionary<string, AttributeValue>(Item0);
            Item0.Add("attr_not_to_get", new AttributeValue("val_not_to_get"));
            _attributesToGet = Arrays.AsList(IdAttribute, "s_someattr"); // not including attr_not_to_get
            _update = Collections.SingletonMap("s_someattr", new AttributeValueUpdate
            {
                Value = new AttributeValue("val2")
            });
            _item0Updated = new Dictionary<string, AttributeValue>(Item0);
            _item0Updated["s_someattr"] = new AttributeValue("val2");
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @After public void cleanup() throws InterruptedException
        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        public virtual void Cleanup()
        {
            DeleteTables();
            CreateTables();
            Dynamodb.Reset();
        }

        //JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
        //ORIGINAL LINE: private void putItem(final boolean commit)
        private void PutItem(bool commit)
        {
            Transaction t = Manager.NewTransaction();
            t.PutItemAsync(new PutItemRequest
            {
                TableName = IntegHashTableName,
                Item = Item0,

            }).Wait();
            if (commit)
            {
                t.CommitAsync().Wait();
                AssertItemNotLocked(IntegHashTableName, Key0, true);
            }
            else
            {
                AssertItemLocked(IntegHashTableName, Key0, t.Id, true, true);
            }
        }

        //JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
        //ORIGINAL LINE: private void updateItemAsync(final boolean commit)
        private void UpdateItemAsync(bool commit)
        {
            Transaction t = Manager.NewTransaction();
            UpdateItemRequest request = new UpdateItemRequest
            {
                TableName = IntegHashTableName,
                Key = Key0,
                AttributeUpdates = _update,

            };
            t.UpdateItemAsync(request).Wait();
            if (commit)
            {
                t.CommitAsync().Wait();
                AssertItemNotLocked(IntegHashTableName, Key0, true);
            }
            else
            {
                AssertItemLocked(IntegHashTableName, Key0, t.Id, false, true);
            }
        }

        //JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
        //ORIGINAL LINE: private void assertContainsNoTransactionAttributes(final java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> item)
        private void AssertContainsNoTransactionAttributes(Dictionary<string, AttributeValue> item)
        {
            AssertFalse(Transaction.IsLocked(item));
            AssertFalse(Transaction.IsApplied(item));
            AssertFalse(Transaction.IsTransient(item));
        }

        //JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
        //ORIGINAL LINE: private com.amazonaws.services.dynamodbv2.model.QueryRequest createQueryRequest(final boolean filterAttributes)
        private QueryRequest CreateQueryRequest(bool filterAttributes)
        {
            Condition hashKeyCondition = new Condition
            {
                ComparisonOperator = ComparisonOperator.EQ,
                AttributeValueList = { Key0[IdAttribute]},

            };
            QueryRequest request = new QueryRequest
            {
                TableName = IntegHashTableName,
                KeyConditions = Collections.SingletonMap(IdAttribute, hashKeyCondition)
            };
            if (filterAttributes)
            {
                request.AttributesToGet = _attributesToGet;
            }
            return request;
        }

        //JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
        //ORIGINAL LINE: private com.amazonaws.services.dynamodbv2.model.BatchGetItemRequest createBatchGetItemRequest(final boolean filterAttributes)
        private BatchGetItemRequest CreateBatchGetItemRequest(bool filterAttributes)
        {
            KeysAndAttributes keysAndAttributes = new KeysAndAttributes
            {
                Keys = { Key0},
            };
            if (filterAttributes)
            {
                keysAndAttributes.AttributesToGet = _attributesToGet;
            }
            return new BatchGetItemRequest
            {
                RequestItems = Collections.SingletonMap(IntegHashTableName, keysAndAttributes)
            };
        }

        //JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
        //ORIGINAL LINE: private void testGetItemContainsItem(final TransactionManagerDynamoDBFacade facade, final java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> item, final boolean filterAttributes)
        private void TestGetItemContainsItem(TransactionManagerDynamoDbFacade facade, Dictionary<string, AttributeValue> item, bool filterAttributes)
        {
            GetItemRequest request = new GetItemRequest
            {
                TableName = IntegHashTableName,
                Key = Key0,

            };
            if (filterAttributes)
            {
                request.AttributesToGet = _attributesToGet;
            }
            GetItemResponse result = facade.GetItemAsync(request).Result;
            AssertContainsNoTransactionAttributes(result.Item);
            AssertEquals(item, result.Item);
        }

        //JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
        //ORIGINAL LINE: private void testScanContainsItem(final TransactionManagerDynamoDBFacade facade, final java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> item, final boolean filterAttributes)
        private void TestScanContainsItem(TransactionManagerDynamoDbFacade facade, Dictionary<string, AttributeValue> item, bool filterAttributes)
        {
            ScanRequest scanRequest = new ScanRequest
            {
                TableName = IntegHashTableName,

            };
            if (filterAttributes)
            {
                scanRequest.AttributesToGet = _attributesToGet;
            }
            ScanResponse scanResponse = facade.ScanAsync(scanRequest).Result;
            AssertEquals(1, scanResponse.Items.Count);
            AssertContainsNoTransactionAttributes(scanResponse.Items[0]);
            AssertEquals(item, scanResponse.Items[0]);
        }

        //JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
        //ORIGINAL LINE: private void testScanIsEmpty(final TransactionManagerDynamoDBFacade facade)
        private void TestScanIsEmpty(TransactionManagerDynamoDbFacade facade)
        {
            ScanResponse scanResponse = facade.ScanAsync(new ScanRequest
            {
                TableName = IntegHashTableName,

            }).Result;
            AssertNotNull(scanResponse.Items);
            AssertEquals(0, scanResponse.Items.Count);
        }

        //JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
        //ORIGINAL LINE: private void testQueryContainsItem(final TransactionManagerDynamoDBFacade facade, final java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> item, final boolean filterAttributes)
        private void TestQueryContainsItem(TransactionManagerDynamoDbFacade facade, Dictionary<string, AttributeValue> item, bool filterAttributes)
        {
            QueryRequest queryRequest = CreateQueryRequest(filterAttributes);
            QueryResponse queryResponse = facade.QueryAsync(queryRequest).Result;
            AssertEquals(1, queryResponse.Items.Count);
            AssertContainsNoTransactionAttributes(queryResponse.Items[0]);
            AssertEquals(item, queryResponse.Items[0]);
        }

        //JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
        //ORIGINAL LINE: private void testQueryIsEmpty(final TransactionManagerDynamoDBFacade facade)
        private void TestQueryIsEmpty(TransactionManagerDynamoDbFacade facade)
        {
            QueryRequest queryRequest = CreateQueryRequest(false);
            QueryResponse queryResponse = facade.QueryAsync(queryRequest).Result;
            AssertNotNull(queryResponse.Items);
            AssertEquals(0, queryResponse.Items.Count);
        }

        //JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
        //ORIGINAL LINE: private void testBatchGetItemsContainsItem(final TransactionManagerDynamoDBFacade facade, final java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> item, final boolean filterAttributes)
        private void TestBatchGetItemsContainsItem(TransactionManagerDynamoDbFacade facade, Dictionary<string, AttributeValue> item, bool filterAttributes)
        {
            BatchGetItemRequest batchGetItemRequest = CreateBatchGetItemRequest(filterAttributes);
            BatchGetItemResponse batchGetItemResponse = facade.BatchGetItemAsync(batchGetItemRequest).Result;
            List<Dictionary<string, AttributeValue>> items = batchGetItemResponse.Responses[IntegHashTableName];
            AssertEquals(1, items.Count);
            AssertContainsNoTransactionAttributes(items[0]);
            AssertEquals(item, items[0]);
        }

        //JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
        //ORIGINAL LINE: private void testBatchGetItemsIsEmpty(final TransactionManagerDynamoDBFacade facade)
        private void TestBatchGetItemsIsEmpty(TransactionManagerDynamoDbFacade facade)
        {
            BatchGetItemRequest batchGetItemRequest = CreateBatchGetItemRequest(false);
            BatchGetItemResponse batchGetItemResponse = facade.BatchGetItemAsync(batchGetItemRequest).Result;
            AssertNotNull(batchGetItemResponse.Responses);
            AssertEquals(1, batchGetItemResponse.Responses.Count);
            AssertNotNull(batchGetItemResponse.Responses[IntegHashTableName]);
            AssertEquals(0, batchGetItemResponse.Responses[IntegHashTableName].Count);

        }

        /// <summary>
        /// Test that calls to scan, query, getItem, and batchGetItems contain
        /// the expected result. </summary>
        /// <param name="facade"> The facade to test </param>
        /// <param name="item"> The expected item to be found </param>
        /// <param name="filterAttributes"> Whether or not to filter attributes using attributesToGet </param>
        //JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
        //ORIGINAL LINE: private void testReadCallsContainItem(final TransactionManagerDynamoDBFacade facade, final java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> item, final boolean filterAttributes)
        private void TestReadCallsContainItem(TransactionManagerDynamoDbFacade facade, Dictionary<string, AttributeValue> item, bool filterAttributes)
        {

            // GetItem contains the expected result
            TestGetItemContainsItem(facade, item, filterAttributes);

            // Scan contains the expected result
            TestScanContainsItem(facade, item, filterAttributes);

            // Query contains the expected result
            TestQueryContainsItem(facade, item, filterAttributes);

            // BatchGetItems contains the expected result
            TestBatchGetItemsContainsItem(facade, item, filterAttributes);
        }

        //JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
        //ORIGINAL LINE: private void testReadCallsReturnEmpty(final TransactionManagerDynamoDBFacade facade)
        private void TestReadCallsReturnEmpty(TransactionManagerDynamoDbFacade facade)
        {

            // GetItem contains null
            TestGetItemContainsItem(facade, null, false);

            // Scan returns empty
            TestScanIsEmpty(facade);

            // Query returns empty
            TestQueryIsEmpty(facade);

            // BatchGetItems does not return item
            TestBatchGetItemsIsEmpty(facade);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void uncommittedFacadeReadsItemIfCommitted()
        public virtual void UncommittedFacadeReadsItemIfCommitted()
        {
            PutItem(true);

            // test that read calls contain the committed item
            TestReadCallsContainItem(_uncommittedFacade, Item0, false);

            // test that read calls contain the committed item respecting attributesToGet
            TestReadCallsContainItem(_uncommittedFacade, _item0Filtered, true);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void uncommittedFacadeReadsItemIfNotCommitted()
        public virtual void UncommittedFacadeReadsItemIfNotCommitted()
        {
            PutItem(false);

            // test that read calls contain the uncommitted item
            TestReadCallsContainItem(_uncommittedFacade, Item0, false);

            // test that read calls contain the uncommitted item respecting attributesToGet
            TestReadCallsContainItem(_uncommittedFacade, _item0Filtered, true);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void uncommittedFacadeReadsUncommittedUpdate()
        public virtual void UncommittedFacadeReadsUncommittedUpdate()
        {
            PutItem(true);
            UpdateItemAsync(false);

            // test that read calls contain the updated uncommitted item
            TestReadCallsContainItem(_uncommittedFacade, _item0Updated, false);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void committedFacadeReadsCommittedItem()
        public virtual void CommittedFacadeReadsCommittedItem()
        {
            PutItem(true);

            // test that read calls contain the committed item
            TestReadCallsContainItem(_committedFacade, Item0, false);

            // test that read calls contain the committed item respecting attributesToGet
            TestReadCallsContainItem(_committedFacade, _item0Filtered, true);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void committedFacadeDoesNotReadUncommittedItem()
        public virtual void CommittedFacadeDoesNotReadUncommittedItem()
        {
            PutItem(false);

            // test that read calls do not contain the uncommitted item
            TestReadCallsReturnEmpty(_committedFacade);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void committedFacadeDoesNotReadUncommittedUpdate()
        public virtual void CommittedFacadeDoesNotReadUncommittedUpdate()
        {
            PutItem(true);
            UpdateItemAsync(false);

            // test that read calls contain the last committed version of the item
            TestReadCallsContainItem(_committedFacade, Item0, false);

            // test that read calls contain the last committed version of the item
            // respecting attributesToGet
            TestReadCallsContainItem(_committedFacade, _item0Filtered, true);
        }

    }

}