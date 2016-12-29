using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using com.amazonaws.services.dynamodbv2.transactions.exceptions;
using com.amazonaws.services.dynamodbv2.util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static DynamoTransactions.Integration.AssertStatic;

// <summary>
// Copyright 2015-2015 Amazon.com, Inc. or its affiliates. All Rights Reserved.
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
    //	import static org.junit.Assert.assertEquals;
    //JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to C#:
    //	import static org.junit.Assert.assertFalse;
    //JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to C#:
    //	import static org.junit.Assert.assertNotNull;
    //JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to C#:
    //	import static org.junit.Assert.assertNull;
    //JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to C#:
    //	import static org.junit.Assert.assertTrue;
    //JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to C#:
    //	import static org.junit.Assert.fail;

    //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
    //ORIGINAL LINE: @Ignore public class IntegrationTest
    public class IntegrationTest
    {

        protected internal static readonly FailingAmazonDynamoDBClient dynamodb;
        //protected internal static readonly AmazonDynamoDBClient documentDynamoDB;
        private const string DYNAMODB_ENDPOINT = "http://dynamodb.us-west-2.amazonaws.com";
        private const string DYNAMODB_ENDPOINT_PROPERTY = "dynamodb-local.endpoint";

        protected internal const string ID_ATTRIBUTE = "Id";
        protected internal const string HASH_TABLE_NAME = "TransactionsIntegrationTest_Hash";
        protected internal const string HASH_RANGE_TABLE_NAME = "TransactionsIntegrationTest_HashRange";
        protected internal const string LOCK_TABLE_NAME = "TransactionsIntegrationTest_Transactions";
        protected internal const string IMAGES_TABLE_NAME = "TransactionsIntegrationTest_ItemImages";
        protected internal static readonly string TABLE_NAME_PREFIX = DateTime.Now.ToString("yyyy-MM-dd'T'HH-mm-ss");

        protected internal static readonly string INTEG_LOCK_TABLE_NAME = TABLE_NAME_PREFIX + "_" + LOCK_TABLE_NAME;
        protected internal static readonly string INTEG_IMAGES_TABLE_NAME = TABLE_NAME_PREFIX + "_" + IMAGES_TABLE_NAME;
        protected internal static readonly string INTEG_HASH_TABLE_NAME = TABLE_NAME_PREFIX + "_" + HASH_TABLE_NAME;
        protected internal static readonly string INTEG_HASH_RANGE_TABLE_NAME = TABLE_NAME_PREFIX + "_" + HASH_RANGE_TABLE_NAME;

        protected internal readonly TransactionManager manager;

        public IntegrationTest()
        {
            manager = new TransactionManager(dynamodb, INTEG_LOCK_TABLE_NAME, INTEG_IMAGES_TABLE_NAME);
        }

        public IntegrationTest(DynamoDBContextConfig config)
        {
            manager = new TransactionManager(dynamodb, INTEG_LOCK_TABLE_NAME, INTEG_IMAGES_TABLE_NAME, config);
        }

        static IntegrationTest()
        {
            AWSCredentials credentials;

            JObject config;
            try
            {
                config = JsonConvert.DeserializeObject<JObject>(File.ReadAllText("config.json"));
            }
            catch (FileNotFoundException)
            {
                config = null;
            }

            string endpoint = config?["endpoint"]?.Value<string>();
            if (endpoint != null)
            {
                credentials = new BasicAWSCredentials("local", "local");
            }
            else
            {
                endpoint = DYNAMODB_ENDPOINT;
                string apiKey = config?["apiKey"]?.Value<string>();
                string secretKey = config?["secretKey"]?.Value<string>();
                if(apiKey != null && secretKey != null)
                {
                    credentials = new BasicAWSCredentials(apiKey, secretKey);
                }
                else
                {
                    Console.Error.WriteLine("No credentials supplied in AwsCredentials.properties, will try with default credentials file");
                    credentials = new StoredProfileAWSCredentials();
                }
            }

            dynamodb = new FailingAmazonDynamoDBClient(credentials, new AmazonDynamoDBConfig { ServiceURL = endpoint });
        }

        protected internal IDictionary<string, AttributeValue> key0;
        protected internal IDictionary<string, AttributeValue> item0;

        protected internal virtual IDictionary<string, AttributeValue> newKey(string tableName)
        {
            IDictionary<string, AttributeValue> key = new Dictionary<string, AttributeValue>();
            key[ID_ATTRIBUTE] = new AttributeValue
            {
                S = "val_" + GlobalRandom.NextDouble
            };
            if (INTEG_HASH_RANGE_TABLE_NAME.Equals(tableName))
            {
                key["RangeAttr"] = new AttributeValue
                {
                    N = Convert.ToString(GlobalRandom.NextDouble)
                };
            }
            else if (!INTEG_HASH_TABLE_NAME.Equals(tableName))
            {
                throw new System.ArgumentException();
            }
            return key;
        }

        private static void waitForTableToBecomeAvailable(string tableName)
        {
            try
            {
                Console.WriteLine("Waiting for " + tableName + " to become ACTIVE...");
                WaitUntilTableReady(tableName);
            }
            catch (Exception)
            {
                throw new Exception("Table " + tableName + " never went active");
            }
        }


        private static void WaitUntilTableReady(string tableName)
        {
            string status = null;
            // Let us wait until table is created. Call DescribeTable.
            do
            {
                System.Threading.Thread.Sleep(5000); // Wait 5 seconds.
                try
                {
                    var res = dynamodb.DescribeTableAsync(new DescribeTableRequest
                    {
                        TableName = tableName
                    }, CancellationToken.None).Result;

                    Console.WriteLine("Table name: {0}, status: {1}",
                                   res.Table.TableName,
                                   res.Table.TableStatus);
                    status = res.Table.TableStatus;
                }
                catch (ResourceNotFoundException)
                {
                    // DescribeTable is eventually consistent. So you might
                    // get resource not found. So we handle the potential exception.
                }
            } while (status != "ACTIVE");
        }

        public static void waitForDelete(string tableName)
        {
            try
            {
                while (true)
                {
                    DescribeTableResponse desc = dynamodb.DescribeTableAsync(new DescribeTableRequest
                    {
                        TableName = tableName
                    }, CancellationToken.None).Result;

                    var status = desc.Table.TableStatus;
                    if (status == TableStatus.DELETING)
                    {
                        Thread.Sleep(5000);
                    }
                    else
                        throw new ArgumentException("Table " + tableName
                            + " is not being deleted (with status=" + status + ")");
                }
            }
            catch (ResourceNotFoundException)
            {
            }
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @BeforeClass public static void createTables() throws InterruptedException
        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        public static void createTables()
        {
            try
            {
                CreateTableRequest createHash = new CreateTableRequest
                {
                    TableName = INTEG_HASH_TABLE_NAME,
                    AttributeDefinitions =
                    {
                        new AttributeDefinition(ID_ATTRIBUTE, ScalarAttributeType.S)
                    },
                    KeySchema =
                    {
                        new KeySchemaElement(ID_ATTRIBUTE, KeyType.HASH)
                    },
                    ProvisionedThroughput = new ProvisionedThroughput(5L, 5L)
                };

                dynamodb.CreateTableAsync(createHash).Wait();
            }
            catch (ResourceInUseException)
            {
                Console.Error.WriteLine("Warning: " + INTEG_HASH_TABLE_NAME + " was already in use");
            }

            try
            {
                TransactionManager.verifyOrCreateTransactionTable(dynamodb, INTEG_LOCK_TABLE_NAME, 10L, 10L, 5L * 60);
                TransactionManager.verifyOrCreateTransactionImagesTable(dynamodb, INTEG_IMAGES_TABLE_NAME, 10L, 10L, 5L * 60);
            }
            catch (ResourceInUseException)
            {
                Console.Error.WriteLine("Warning: " + INTEG_HASH_TABLE_NAME + " was already in use");
            }


            waitForTableToBecomeAvailable(INTEG_HASH_TABLE_NAME);

            waitForTableToBecomeAvailable(INTEG_LOCK_TABLE_NAME);

            waitForTableToBecomeAvailable(INTEG_IMAGES_TABLE_NAME);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @AfterClass public static void deleteTables() throws InterruptedException
        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        public static void deleteTables()
        {
            try
            {
                Table hashTable = Table.LoadTable(dynamodb, INTEG_HASH_TABLE_NAME);
                Table lockTable = Table.LoadTable(dynamodb, INTEG_LOCK_TABLE_NAME);
                Table imagesTable = Table.LoadTable(dynamodb, INTEG_IMAGES_TABLE_NAME);

                Console.WriteLine("Issuing DeleteTable request for " + INTEG_HASH_TABLE_NAME);
                dynamodb.DeleteTableAsync(INTEG_HASH_TABLE_NAME);
                Console.WriteLine("Issuing DeleteTable request for " + INTEG_LOCK_TABLE_NAME);
                dynamodb.DeleteTableAsync(INTEG_LOCK_TABLE_NAME);
                Console.WriteLine("Issuing DeleteTable request for " + INTEG_IMAGES_TABLE_NAME);
                dynamodb.DeleteTableAsync(INTEG_IMAGES_TABLE_NAME);

                Console.WriteLine("Waiting for " + INTEG_HASH_TABLE_NAME + " to be deleted...this may take a while...");
                waitForDelete(INTEG_HASH_TABLE_NAME);
                Console.WriteLine("Waiting for " + INTEG_LOCK_TABLE_NAME + " to be deleted...this may take a while...");
                waitForDelete(INTEG_LOCK_TABLE_NAME);
                Console.WriteLine("Waiting for " + INTEG_IMAGES_TABLE_NAME + " to be deleted...this may take a while...");
                waitForDelete(INTEG_IMAGES_TABLE_NAME);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("DeleteTable request failed for some table");
                Console.Error.WriteLine(e.Message);
            }
        }

        protected internal virtual void assertItemLocked(string tableName, Dictionary<string, AttributeValue> key, Dictionary<string, AttributeValue> expected, string owner, bool isTransient, bool isApplied)
        {
            assertItemLocked(tableName, key, expected, owner, isTransient, isApplied, true);
        }

        protected internal virtual void assertItemLocked(string tableName, Dictionary<string, AttributeValue> key, Dictionary<string, AttributeValue> expected, string owner, bool isTransient, bool isApplied, bool checkTxItem)
        {
            Dictionary<string, AttributeValue> item = getItem(tableName, key);
            assertNotNull(item);
            assertEquals(owner, item[Transaction.AttributeName.TXID.ToString()].S);
            if (isTransient)
            {
                assertTrue("item is not transient, and should have been", item.ContainsKey(Transaction.AttributeName.TRANSIENT.ToString()));
                assertEquals("item is not transient, and should have been", "1", item[Transaction.AttributeName.TRANSIENT.ToString()].S);
            }
            else
            {
                assertNull("item is transient, and should not have been", item[Transaction.AttributeName.TRANSIENT.ToString()]);
            }
            if (isApplied)
            {
                assertTrue("item is not applied, and should have been", item.ContainsKey(Transaction.AttributeName.APPLIED.ToString()));
                assertEquals("item is not applied, and should have been", "1", item[Transaction.AttributeName.APPLIED.ToString()].S);
            }
            else
            {
                assertNull("item is applied, and should not have been", item[Transaction.AttributeName.APPLIED.ToString()]);
            }
            assertTrue(item.ContainsKey(Transaction.AttributeName.DATE.ToString()));
            if (expected != null)
            {
                item.Remove(Transaction.AttributeName.TXID.ToString());
                item.Remove(Transaction.AttributeName.TRANSIENT.ToString());
                item.Remove(Transaction.AttributeName.APPLIED.ToString());
                item.Remove(Transaction.AttributeName.DATE.ToString());
                assertEquals(expected, item);
            }
            // Also verify that it is locked in the tx record
            if (checkTxItem)
            {
                TransactionItem txItem = new TransactionItem(owner, manager, false);
                assertTrue(txItem.RequestMap.ContainsKey(tableName));
                assertTrue(txItem.RequestMap[tableName].ContainsKey(new ImmutableKey(key)));
            }
        }

        protected internal virtual void assertItemLocked(string tableName, Dictionary<string, AttributeValue> key, string owner, bool isTransient, bool isApplied)
        {
            assertItemLocked(tableName, key, null, owner, isTransient, isApplied);
        }

        protected internal virtual void assertItemNotLocked(string tableName, Dictionary<string, AttributeValue> key, Dictionary<string, AttributeValue> expected, bool shouldExist)
        {
            Dictionary<string, AttributeValue> item = getItem(tableName, key);
            if (shouldExist)
            {
                assertNotNull("Item does not exist in the table, but it should", item);
                assertNull(item[Transaction.AttributeName.TRANSIENT.ToString()]);
                assertNull(item[Transaction.AttributeName.TXID.ToString()]);
                assertNull(item[Transaction.AttributeName.APPLIED.ToString()]);
                assertNull(item[Transaction.AttributeName.DATE.ToString()]);
            }
            else
            {
                assertNull("Item should have been null: " + item, item);
            }

            if (expected != null)
            {
                item.Remove(Transaction.AttributeName.TXID.ToString());
                item.Remove(Transaction.AttributeName.TRANSIENT.ToString());
                assertEquals(expected, item);
            }
        }

        protected internal virtual void assertItemNotLocked(string tableName, Dictionary<string, AttributeValue> key, bool shouldExist)
        {
            assertItemNotLocked(tableName, key, null, shouldExist);
        }

        protected internal virtual void assertTransactionDeleted(Transaction t)
        {
            try
            {
                manager.resumeTransaction(t.Id);
                fail();
            }
            catch (TransactionNotFoundException e)
            {
                assertTrue(e.Message.Contains("Transaction not found"));
            }
        }

        protected internal virtual void assertNoSpecialAttributes(IDictionary<string, AttributeValue> item)
        {
            foreach (string attrName in Transaction.SPECIAL_ATTR_NAMES)
            {
                if (item.ContainsKey(attrName))
                {
                    fail("Should not have contained attribute " + attrName + " " + item);
                }
            }
        }

        protected internal virtual void assertOldItemImage(string txId, string tableName, Dictionary<string, AttributeValue> key, IDictionary<string, AttributeValue> item, bool shouldExist)
        {
            Transaction t = manager.resumeTransaction(txId);
            Dictionary<string, Dictionary<ImmutableKey, Request>> requests = t.TxItem.RequestMap;
            Request r = requests[tableName][new ImmutableKey(key)];
            Dictionary<string, AttributeValue> image = t.TxItem.loadItemImageAsync(r.Rid).Result;
            if (shouldExist)
            {
                assertNotNull(image);
                image.Remove(Transaction.AttributeName.TXID.ToString());
                image.Remove(Transaction.AttributeName.IMAGE_ID.ToString());
                image.Remove(Transaction.AttributeName.DATE.ToString());
                assertFalse(image.ContainsKey(Transaction.AttributeName.TRANSIENT.ToString()));
                assertEquals(item, image); // TODO does not work for Set AttributeValue types (DynamoDB does not preserve ordering)
            }
            else
            {
                assertNull(image);
            }
        }

        protected internal virtual Dictionary<string, AttributeValue> getItem(string tableName, Dictionary<string, AttributeValue> key)
        {
            GetItemResponse result = dynamodb.GetItemAsync(new GetItemRequest
                {
                    TableName = tableName,
                    Key = key,
                    ReturnConsumedCapacity = ReturnConsumedCapacity.TOTAL,
                    ConsistentRead = true
                }).Result;
            return result.Item;
        }

    }
}