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

    public class TransactionManagerDBFacadeIntegrationTest : IntegrationTest
    {

        private TransactionManagerDynamoDBFacade uncommittedFacade;
        private TransactionManagerDynamoDBFacade committedFacade;

        private Dictionary<string, AttributeValueUpdate> update;
        private Dictionary<string, AttributeValue> item0Updated;
        private Dictionary<string, AttributeValue> item0Filtered; // item0 with only the attributesToGet
        private List<string> attributesToGet;

        public TransactionManagerDBFacadeIntegrationTest() : base()
        {
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Before public void setup()
        public virtual void setup()
        {
            dynamodb.reset();
            uncommittedFacade = new TransactionManagerDynamoDBFacade(manager, Transaction.IsolationLevel.UNCOMMITTED);
            committedFacade = new TransactionManagerDynamoDBFacade(manager, Transaction.IsolationLevel.COMMITTED);
            key0 = newKey(INTEG_HASH_TABLE_NAME);
            item0 = new Dictionary<string, AttributeValue>(key0);
            item0.Add("s_someattr", new AttributeValue("val"));
            item0Filtered = new Dictionary<string, AttributeValue>(item0);
            item0.Add("attr_not_to_get", new AttributeValue("val_not_to_get"));
            attributesToGet = Arrays.asList(ID_ATTRIBUTE, "s_someattr"); // not including attr_not_to_get
            update = Collections.singletonMap("s_someattr", new AttributeValueUpdate
            {
                Value = new AttributeValue("val2")
            });
            item0Updated = new Dictionary<string, AttributeValue>(item0);
            item0Updated["s_someattr"] = new AttributeValue("val2");
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @After public void cleanup() throws InterruptedException
        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        public virtual void cleanup()
        {
            deleteTables();
            createTables();
            dynamodb.reset();
        }

        //JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
        //ORIGINAL LINE: private void putItem(final boolean commit)
        private void putItem(bool commit)
        {
            Transaction t = manager.newTransaction();
            t.putItemAsync(new PutItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Item = item0,

            }).Wait();
            if (commit)
            {
                t.commitAsync().Wait();
                assertItemNotLocked(INTEG_HASH_TABLE_NAME, key0, true);
            }
            else
            {
                assertItemLocked(INTEG_HASH_TABLE_NAME, key0, t.Id, true, true);
            }
        }

        //JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
        //ORIGINAL LINE: private void updateItemAsync(final boolean commit)
        private void updateItemAsync(bool commit)
        {
            Transaction t = manager.newTransaction();
            UpdateItemRequest request = new UpdateItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Key = key0,
                AttributeUpdates = update,

            };
            t.updateItemAsync(request).Wait();
            if (commit)
            {
                t.commitAsync().Wait();
                assertItemNotLocked(INTEG_HASH_TABLE_NAME, key0, true);
            }
            else
            {
                assertItemLocked(INTEG_HASH_TABLE_NAME, key0, t.Id, false, true);
            }
        }

        //JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
        //ORIGINAL LINE: private void assertContainsNoTransactionAttributes(final java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> item)
        private void assertContainsNoTransactionAttributes(Dictionary<string, AttributeValue> item)
        {
            assertFalse(Transaction.isLocked(item));
            assertFalse(Transaction.isApplied(item));
            assertFalse(Transaction.isTransient(item));
        }

        //JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
        //ORIGINAL LINE: private com.amazonaws.services.dynamodbv2.model.QueryRequest createQueryRequest(final boolean filterAttributes)
        private QueryRequest createQueryRequest(bool filterAttributes)
        {
            Condition hashKeyCondition = new Condition
            {
                ComparisonOperator = ComparisonOperator.EQ,
                AttributeValueList = { key0[ID_ATTRIBUTE]},

            };
            QueryRequest request = new QueryRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                KeyConditions = Collections.singletonMap(ID_ATTRIBUTE, hashKeyCondition)
            };
            if (filterAttributes)
            {
                request.AttributesToGet = attributesToGet;
            }
            return request;
        }

        //JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
        //ORIGINAL LINE: private com.amazonaws.services.dynamodbv2.model.BatchGetItemRequest createBatchGetItemRequest(final boolean filterAttributes)
        private BatchGetItemRequest createBatchGetItemRequest(bool filterAttributes)
        {
            KeysAndAttributes keysAndAttributes = new KeysAndAttributes
            {
                Keys = { key0},
            };
            if (filterAttributes)
            {
                keysAndAttributes.AttributesToGet = attributesToGet;
            }
            return new BatchGetItemRequest
            {
                RequestItems = Collections.singletonMap(INTEG_HASH_TABLE_NAME, keysAndAttributes)
            };
        }

        //JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
        //ORIGINAL LINE: private void testGetItemContainsItem(final TransactionManagerDynamoDBFacade facade, final java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> item, final boolean filterAttributes)
        private void testGetItemContainsItem(TransactionManagerDynamoDBFacade facade, Dictionary<string, AttributeValue> item, bool filterAttributes)
        {
            GetItemRequest request = new GetItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Key = key0,

            };
            if (filterAttributes)
            {
                request.AttributesToGet = attributesToGet;
            }
            GetItemResponse result = facade.GetItemAsync(request).Result;
            assertContainsNoTransactionAttributes(result.Item);
            assertEquals(item, result.Item);
        }

        //JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
        //ORIGINAL LINE: private void testScanContainsItem(final TransactionManagerDynamoDBFacade facade, final java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> item, final boolean filterAttributes)
        private void testScanContainsItem(TransactionManagerDynamoDBFacade facade, Dictionary<string, AttributeValue> item, bool filterAttributes)
        {
            ScanRequest scanRequest = new ScanRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,

            };
            if (filterAttributes)
            {
                scanRequest.AttributesToGet = attributesToGet;
            }
            ScanResponse scanResponse = facade.ScanAsync(scanRequest).Result;
            assertEquals(1, scanResponse.Items.Count);
            assertContainsNoTransactionAttributes(scanResponse.Items[0]);
            assertEquals(item, scanResponse.Items[0]);
        }

        //JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
        //ORIGINAL LINE: private void testScanIsEmpty(final TransactionManagerDynamoDBFacade facade)
        private void testScanIsEmpty(TransactionManagerDynamoDBFacade facade)
        {
            ScanResponse scanResponse = facade.ScanAsync(new ScanRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,

            }).Result;
            assertNotNull(scanResponse.Items);
            assertEquals(0, scanResponse.Items.Count);
        }

        //JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
        //ORIGINAL LINE: private void testQueryContainsItem(final TransactionManagerDynamoDBFacade facade, final java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> item, final boolean filterAttributes)
        private void testQueryContainsItem(TransactionManagerDynamoDBFacade facade, Dictionary<string, AttributeValue> item, bool filterAttributes)
        {
            QueryRequest queryRequest = createQueryRequest(filterAttributes);
            QueryResponse queryResponse = facade.QueryAsync(queryRequest).Result;
            assertEquals(1, queryResponse.Items.Count);
            assertContainsNoTransactionAttributes(queryResponse.Items[0]);
            assertEquals(item, queryResponse.Items[0]);
        }

        //JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
        //ORIGINAL LINE: private void testQueryIsEmpty(final TransactionManagerDynamoDBFacade facade)
        private void testQueryIsEmpty(TransactionManagerDynamoDBFacade facade)
        {
            QueryRequest queryRequest = createQueryRequest(false);
            QueryResponse queryResponse = facade.QueryAsync(queryRequest).Result;
            assertNotNull(queryResponse.Items);
            assertEquals(0, queryResponse.Items.Count);
        }

        //JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
        //ORIGINAL LINE: private void testBatchGetItemsContainsItem(final TransactionManagerDynamoDBFacade facade, final java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> item, final boolean filterAttributes)
        private void testBatchGetItemsContainsItem(TransactionManagerDynamoDBFacade facade, Dictionary<string, AttributeValue> item, bool filterAttributes)
        {
            BatchGetItemRequest batchGetItemRequest = createBatchGetItemRequest(filterAttributes);
            BatchGetItemResponse batchGetItemResponse = facade.BatchGetItemAsync(batchGetItemRequest).Result;
            List<Dictionary<string, AttributeValue>> items = batchGetItemResponse.Responses[INTEG_HASH_TABLE_NAME];
            assertEquals(1, items.Count);
            assertContainsNoTransactionAttributes(items[0]);
            assertEquals(item, items[0]);
        }

        //JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
        //ORIGINAL LINE: private void testBatchGetItemsIsEmpty(final TransactionManagerDynamoDBFacade facade)
        private void testBatchGetItemsIsEmpty(TransactionManagerDynamoDBFacade facade)
        {
            BatchGetItemRequest batchGetItemRequest = createBatchGetItemRequest(false);
            BatchGetItemResponse batchGetItemResponse = facade.BatchGetItemAsync(batchGetItemRequest).Result;
            assertNotNull(batchGetItemResponse.Responses);
            assertEquals(1, batchGetItemResponse.Responses.Count);
            assertNotNull(batchGetItemResponse.Responses[INTEG_HASH_TABLE_NAME]);
            assertEquals(0, batchGetItemResponse.Responses[INTEG_HASH_TABLE_NAME].Count);

        }

        /// <summary>
        /// Test that calls to scan, query, getItem, and batchGetItems contain
        /// the expected result. </summary>
        /// <param name="facade"> The facade to test </param>
        /// <param name="item"> The expected item to be found </param>
        /// <param name="filterAttributes"> Whether or not to filter attributes using attributesToGet </param>
        //JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
        //ORIGINAL LINE: private void testReadCallsContainItem(final TransactionManagerDynamoDBFacade facade, final java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> item, final boolean filterAttributes)
        private void testReadCallsContainItem(TransactionManagerDynamoDBFacade facade, Dictionary<string, AttributeValue> item, bool filterAttributes)
        {

            // GetItem contains the expected result
            testGetItemContainsItem(facade, item, filterAttributes);

            // Scan contains the expected result
            testScanContainsItem(facade, item, filterAttributes);

            // Query contains the expected result
            testQueryContainsItem(facade, item, filterAttributes);

            // BatchGetItems contains the expected result
            testBatchGetItemsContainsItem(facade, item, filterAttributes);
        }

        //JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
        //ORIGINAL LINE: private void testReadCallsReturnEmpty(final TransactionManagerDynamoDBFacade facade)
        private void testReadCallsReturnEmpty(TransactionManagerDynamoDBFacade facade)
        {

            // GetItem contains null
            testGetItemContainsItem(facade, null, false);

            // Scan returns empty
            testScanIsEmpty(facade);

            // Query returns empty
            testQueryIsEmpty(facade);

            // BatchGetItems does not return item
            testBatchGetItemsIsEmpty(facade);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void uncommittedFacadeReadsItemIfCommitted()
        public virtual void uncommittedFacadeReadsItemIfCommitted()
        {
            putItem(true);

            // test that read calls contain the committed item
            testReadCallsContainItem(uncommittedFacade, item0, false);

            // test that read calls contain the committed item respecting attributesToGet
            testReadCallsContainItem(uncommittedFacade, item0Filtered, true);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void uncommittedFacadeReadsItemIfNotCommitted()
        public virtual void uncommittedFacadeReadsItemIfNotCommitted()
        {
            putItem(false);

            // test that read calls contain the uncommitted item
            testReadCallsContainItem(uncommittedFacade, item0, false);

            // test that read calls contain the uncommitted item respecting attributesToGet
            testReadCallsContainItem(uncommittedFacade, item0Filtered, true);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void uncommittedFacadeReadsUncommittedUpdate()
        public virtual void uncommittedFacadeReadsUncommittedUpdate()
        {
            putItem(true);
            updateItemAsync(false);

            // test that read calls contain the updated uncommitted item
            testReadCallsContainItem(uncommittedFacade, item0Updated, false);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void committedFacadeReadsCommittedItem()
        public virtual void committedFacadeReadsCommittedItem()
        {
            putItem(true);

            // test that read calls contain the committed item
            testReadCallsContainItem(committedFacade, item0, false);

            // test that read calls contain the committed item respecting attributesToGet
            testReadCallsContainItem(committedFacade, item0Filtered, true);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void committedFacadeDoesNotReadUncommittedItem()
        public virtual void committedFacadeDoesNotReadUncommittedItem()
        {
            putItem(false);

            // test that read calls do not contain the uncommitted item
            testReadCallsReturnEmpty(committedFacade);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void committedFacadeDoesNotReadUncommittedUpdate()
        public virtual void committedFacadeDoesNotReadUncommittedUpdate()
        {
            putItem(true);
            updateItemAsync(false);

            // test that read calls contain the last committed version of the item
            testReadCallsContainItem(committedFacade, item0, false);

            // test that read calls contain the last committed version of the item
            // respecting attributesToGet
            testReadCallsContainItem(committedFacade, item0Filtered, true);
        }

    }

}