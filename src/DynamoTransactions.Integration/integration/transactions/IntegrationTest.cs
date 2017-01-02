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

        protected internal static readonly FailingAmazonDynamoDbClient Dynamodb;
        //protected internal static readonly AmazonDynamoDBClient documentDynamoDB;
        private const string DynamodbEndpoint = "http://dynamodb.us-west-2.amazonaws.com";
        private const string DynamodbEndpointProperty = "dynamodb-local.endpoint";

        protected internal const string IdAttribute = "Id";
        protected internal const string HashTableName = "TransactionsIntegrationTest_Hash";
        protected internal const string HashRangeTableName = "TransactionsIntegrationTest_HashRange";
        protected internal const string LockTableName = "TransactionsIntegrationTest_Transactions";
        protected internal const string ImagesTableName = "TransactionsIntegrationTest_ItemImages";
        protected internal static readonly string TableNamePrefix = DateTime.Now.ToString("yyyy-MM-dd'T'HH-mm-ss");

        protected internal static readonly string IntegLockTableName = TableNamePrefix + "_" + LockTableName;
        protected internal static readonly string IntegImagesTableName = TableNamePrefix + "_" + ImagesTableName;
        protected internal static readonly string IntegHashTableName = TableNamePrefix + "_" + HashTableName;
        protected internal static readonly string IntegHashRangeTableName = TableNamePrefix + "_" + HashRangeTableName;

        protected internal readonly TransactionManager Manager;

        public IntegrationTest()
        {
            Manager = new TransactionManager(Dynamodb, IntegLockTableName, IntegImagesTableName);
        }

        public IntegrationTest(DynamoDBContextConfig config)
        {
            Manager = new TransactionManager(Dynamodb, IntegLockTableName, IntegImagesTableName, config);
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
                endpoint = DynamodbEndpoint;
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

            Dynamodb = new FailingAmazonDynamoDbClient(credentials, new AmazonDynamoDBConfig { ServiceURL = endpoint });
        }

        protected internal Dictionary<string, AttributeValue> Key0;
        protected internal Dictionary<string, AttributeValue> Item0;

        protected internal virtual Dictionary<string, AttributeValue> NewKey(string tableName)
        {
            Dictionary<string, AttributeValue> key = new Dictionary<string, AttributeValue>();
            key[IdAttribute] = new AttributeValue
            {
                S = "val_" + GlobalRandom.NextDouble
            };
            if (IntegHashRangeTableName.Equals(tableName))
            {
                key["RangeAttr"] = new AttributeValue
                {
                    N = Convert.ToString(GlobalRandom.NextDouble)
                };
            }
            else if (!IntegHashTableName.Equals(tableName))
            {
                throw new System.ArgumentException();
            }
            return key;
        }

        private static void WaitForTableToBecomeAvailable(string tableName)
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
                    var res = Dynamodb.DescribeTableAsync(new DescribeTableRequest
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

        public static void WaitForDelete(string tableName)
        {
            try
            {
                while (true)
                {
                    DescribeTableResponse desc = Dynamodb.DescribeTableAsync(new DescribeTableRequest
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
        public static void CreateTables()
        {
            try
            {
                CreateTableRequest createHash = new CreateTableRequest
                {
                    TableName = IntegHashTableName,
                    AttributeDefinitions =
                    {
                        new AttributeDefinition(IdAttribute, ScalarAttributeType.S)
                    },
                    KeySchema =
                    {
                        new KeySchemaElement(IdAttribute, KeyType.HASH)
                    },
                    ProvisionedThroughput = new ProvisionedThroughput(5L, 5L)
                };

                Dynamodb.CreateTableAsync(createHash).Wait();
            }
            catch (ResourceInUseException)
            {
                Console.Error.WriteLine("Warning: " + IntegHashTableName + " was already in use");
            }

            try
            {
                TransactionManager.VerifyOrCreateTransactionTableAsync(Dynamodb, IntegLockTableName, 10L, 10L, 5L * 60).Wait();
                TransactionManager.VerifyOrCreateTransactionImagesTableAsync(Dynamodb, IntegImagesTableName, 10L, 10L, 5L * 60).Wait();
            }
            catch (ResourceInUseException)
            {
                Console.Error.WriteLine("Warning: " + IntegHashTableName + " was already in use");
            }


            WaitForTableToBecomeAvailable(IntegHashTableName);

            WaitForTableToBecomeAvailable(IntegLockTableName);

            WaitForTableToBecomeAvailable(IntegImagesTableName);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @AfterClass public static void deleteTables() throws InterruptedException
        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        public static void DeleteTables()
        {
            try
            {
                Table hashTable = Table.LoadTable(Dynamodb, IntegHashTableName);
                Table lockTable = Table.LoadTable(Dynamodb, IntegLockTableName);
                Table imagesTable = Table.LoadTable(Dynamodb, IntegImagesTableName);

                Console.WriteLine("Issuing DeleteTable request for " + IntegHashTableName);
                Dynamodb.DeleteTableAsync(IntegHashTableName);
                Console.WriteLine("Issuing DeleteTable request for " + IntegLockTableName);
                Dynamodb.DeleteTableAsync(IntegLockTableName);
                Console.WriteLine("Issuing DeleteTable request for " + IntegImagesTableName);
                Dynamodb.DeleteTableAsync(IntegImagesTableName);

                Console.WriteLine("Waiting for " + IntegHashTableName + " to be deleted...this may take a while...");
                WaitForDelete(IntegHashTableName);
                Console.WriteLine("Waiting for " + IntegLockTableName + " to be deleted...this may take a while...");
                WaitForDelete(IntegLockTableName);
                Console.WriteLine("Waiting for " + IntegImagesTableName + " to be deleted...this may take a while...");
                WaitForDelete(IntegImagesTableName);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("DeleteTable request failed for some table");
                Console.Error.WriteLine(e.Message);
            }
        }

        protected internal virtual void AssertItemLocked(string tableName, Dictionary<string, AttributeValue> key, Dictionary<string, AttributeValue> expected, string owner, bool isTransient, bool isApplied)
        {
            AssertItemLocked(tableName, key, expected, owner, isTransient, isApplied, true);
        }

        protected internal virtual void AssertItemLocked(string tableName, Dictionary<string, AttributeValue> key, Dictionary<string, AttributeValue> expected, string owner, bool isTransient, bool isApplied, bool checkTxItem)
        {
            Dictionary<string, AttributeValue> item = GetItem(tableName, key);
            AssertNotNull(item);
            AssertEquals(owner, item[Transaction.AttributeName.Txid.ToString()].S);
            if (isTransient)
            {
                AssertTrue("item is not transient, and should have been", item.ContainsKey(Transaction.AttributeName.Transient.ToString()));
                AssertEquals("item is not transient, and should have been", "1", item[Transaction.AttributeName.Transient.ToString()].S);
            }
            else
            {
                AssertNull("item is transient, and should not have been", item[Transaction.AttributeName.Transient.ToString()]);
            }
            if (isApplied)
            {
                AssertTrue("item is not applied, and should have been", item.ContainsKey(Transaction.AttributeName.Applied.ToString()));
                AssertEquals("item is not applied, and should have been", "1", item[Transaction.AttributeName.Applied.ToString()].S);
            }
            else
            {
                AssertNull("item is applied, and should not have been", item[Transaction.AttributeName.Applied.ToString()]);
            }
            AssertTrue(item.ContainsKey(Transaction.AttributeName.Date.ToString()));
            if (expected != null)
            {
                item.Remove(Transaction.AttributeName.Txid.ToString());
                item.Remove(Transaction.AttributeName.Transient.ToString());
                item.Remove(Transaction.AttributeName.Applied.ToString());
                item.Remove(Transaction.AttributeName.Date.ToString());
                AssertEquals(expected, item);
            }
            // Also verify that it is locked in the tx record
            if (checkTxItem)
            {
                TransactionItem txItem = new TransactionItem(owner, Manager, false);
                AssertTrue(txItem.RequestMap.ContainsKey(tableName));
                AssertTrue(txItem.RequestMap[tableName].ContainsKey(new ImmutableKey(key)));
            }
        }

        protected internal virtual void AssertItemLocked(string tableName, Dictionary<string, AttributeValue> key, string owner, bool isTransient, bool isApplied)
        {
            AssertItemLocked(tableName, key, null, owner, isTransient, isApplied);
        }

        protected internal virtual void AssertItemNotLocked(string tableName, Dictionary<string, AttributeValue> key, Dictionary<string, AttributeValue> expected, bool shouldExist)
        {
            Dictionary<string, AttributeValue> item = GetItem(tableName, key);
            if (shouldExist)
            {
                AssertNotNull("Item does not exist in the table, but it should", item);
                AssertNull(item[Transaction.AttributeName.Transient.ToString()]);
                AssertNull(item[Transaction.AttributeName.Txid.ToString()]);
                AssertNull(item[Transaction.AttributeName.Applied.ToString()]);
                AssertNull(item[Transaction.AttributeName.Date.ToString()]);
            }
            else
            {
                AssertNull("Item should have been null: " + item, item);
            }

            if (expected != null)
            {
                item.Remove(Transaction.AttributeName.Txid.ToString());
                item.Remove(Transaction.AttributeName.Transient.ToString());
                AssertEquals(expected, item);
            }
        }

        protected internal virtual void AssertItemNotLocked(string tableName, Dictionary<string, AttributeValue> key, bool shouldExist)
        {
            AssertItemNotLocked(tableName, key, null, shouldExist);
        }

        protected internal virtual void AssertTransactionDeleted(Transaction t)
        {
            try
            {
                Manager.ResumeTransaction(t.Id);
                Fail();
            }
            catch (TransactionNotFoundException e)
            {
                AssertTrue(e.Message.Contains("Transaction not found"));
            }
        }

        protected internal virtual void AssertNoSpecialAttributes(Dictionary<string, AttributeValue> item)
        {
            foreach (string attrName in Transaction.SpecialAttrNames)
            {
                if (item.ContainsKey(attrName))
                {
                    Fail("Should not have contained attribute " + attrName + " " + item);
                }
            }
        }

        protected internal virtual void AssertOldItemImage(string txId, string tableName, Dictionary<string, AttributeValue> key, Dictionary<string, AttributeValue> item, bool shouldExist)
        {
            Transaction t = Manager.ResumeTransaction(txId);
            Dictionary<string, Dictionary<ImmutableKey, Request>> requests = t.TxItem.RequestMap;
            Request r = requests[tableName][new ImmutableKey(key)];
            Dictionary<string, AttributeValue> image = t.TxItem.LoadItemImageAsync(r.Rid).Result;
            if (shouldExist)
            {
                AssertNotNull(image);
                image.Remove(Transaction.AttributeName.Txid.ToString());
                image.Remove(Transaction.AttributeName.ImageId.ToString());
                image.Remove(Transaction.AttributeName.Date.ToString());
                AssertFalse(image.ContainsKey(Transaction.AttributeName.Transient.ToString()));
                AssertEquals(item, image); // TODO does not work for Set AttributeValue types (DynamoDB does not preserve ordering)
            }
            else
            {
                AssertNull(image);
            }
        }

        protected internal virtual Dictionary<string, AttributeValue> GetItem(string tableName, Dictionary<string, AttributeValue> key)
        {
            GetItemResponse result = Dynamodb.GetItemAsync(new GetItemRequest
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