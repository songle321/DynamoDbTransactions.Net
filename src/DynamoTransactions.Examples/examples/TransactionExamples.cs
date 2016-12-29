using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using com.amazonaws.services.dynamodbv2.transactions.exceptions;
using com.amazonaws.services.dynamodbv2.util;
using DynamoTransactions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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

namespace com.amazonaws.services.dynamodbv2.transactions.examples
{
    /// <summary>
    /// Demonstrates creating the required transactions tables, an example user table, and performs several transactions
    /// to demonstrate the best practices for using this library.
    /// 
    /// To use this library, you will need to fill in the AwsCredentials.properties file with credentials for your account, 
    /// or otherwise modify this file to inject your credentials in another way (such as by using IAM Roles for EC2) 
    /// </summary>
    public class TransactionExamples
    {

        protected internal const string TX_TABLE_NAME = "Transactions";
        protected internal const string TX_IMAGES_TABLE_NAME = "TransactionImages";
        protected internal const string EXAMPLE_TABLE_NAME = "TransactionExamples";
        protected internal const string EXAMPLE_TABLE_HASH_KEY = "ItemId";
        protected internal const string DYNAMODB_ENDPOINT = "http://dynamodb.us-west-2.amazonaws.com";

        protected internal readonly AmazonDynamoDBClient dynamodb;
        protected internal readonly TransactionManager txManager;

        public static void Main(string[] args)
        {
            print("Running DynamoDB transaction examples");
            try
            {
                (new TransactionExamples()).run();
                print("Exiting normally");
            }
            catch (Exception t)
            {
                Console.Error.WriteLine("Uncaught exception:" + t);
            }
        }

        public TransactionExamples()
        {
            AWSCredentials credentials;
            
            try
            {
                var credentialOptions = JsonConvert.DeserializeObject<JObject>(File.ReadAllText("config.json"));
                credentials = new BasicAWSCredentials(credentialOptions["apiKey"].Value<string>(), credentialOptions["secretKey"].Value<string>());
            }
            catch (FileNotFoundException)
            {
                credentials = null;
            }
            if (credentials != null)
            {
                Console.Error.WriteLine("No credentials supplied in AwsCredentials.properties, will try with default credentials file");
                credentials = new StoredProfileAWSCredentials();
            }

            dynamodb = new AmazonDynamoDBClient(credentials, new AmazonDynamoDBConfig
            {
                ServiceURL = DYNAMODB_ENDPOINT
            });
            txManager = new TransactionManager(dynamodb, TX_TABLE_NAME, TX_IMAGES_TABLE_NAME);
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public void run() throws Exception
        public virtual void run()
        {
            setup();
            twoItemTransaction();
            conflictingTransactions();
            errorHandling();
            badRequest();
            readThenWrite();
            conditionallyCreateOrUpdateWithMapper();
            reading();
            readCommittedWithMapper();
            sweepForStuckAndOldTransactions();
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public void setup() throws Exception
        public virtual void setup()
        {
            print("\n*** setup() ***\n");
            TableHelper tableHelper = new TableHelper(dynamodb);

            // 1. Verify that the transaction table exists, or create it if it doesn't exist
            print("Verifying or creating table " + TX_TABLE_NAME);
            TransactionManager.verifyOrCreateTransactionTable(dynamodb, TX_TABLE_NAME, 1, 1, null);

            // 2. Verify that the transaction item images table exists, or create it otherwise
            print("Verifying or creating table " + TX_IMAGES_TABLE_NAME);
            TransactionManager.verifyOrCreateTransactionImagesTable(dynamodb, TX_IMAGES_TABLE_NAME, 1, 1, null);

            // 3. Create a table to do transactions on
            print("Verifying or creating table " + EXAMPLE_TABLE_NAME);
            List<AttributeDefinition> attributeDefinitions = Arrays.asList((new AttributeDefinition()).withAttributeName(EXAMPLE_TABLE_HASH_KEY).withAttributeType(ScalarAttributeType.S));
            List<KeySchemaElement> keySchema = Arrays.asList((new KeySchemaElement()).withAttributeName(EXAMPLE_TABLE_HASH_KEY).withKeyType(KeyType.HASH));
            ProvisionedThroughput provisionedThroughput = (new ProvisionedThroughput
            {
                ReadCapacityUnits = 1L,
                WriteCapacityUnits = 1L
            });

            tableHelper.verifyOrCreateTableAsync(EXAMPLE_TABLE_NAME, attributeDefinitions, keySchema, null, provisionedThroughput, null).Wait();

            // 4. Wait for tables to be created
            print("Waiting for table to become ACTIVE: " + EXAMPLE_TABLE_NAME);
            tableHelper.waitForTableActiveAsync(EXAMPLE_TABLE_NAME, 5 * 60L).Wait();
            print("Waiting for table to become ACTIVE: " + TX_TABLE_NAME);
            tableHelper.waitForTableActiveAsync(TX_TABLE_NAME, 5 * 60L).Wait();
            print("Waiting for table to become ACTIVE: " + TX_IMAGES_TABLE_NAME);
            tableHelper.waitForTableActiveAsync(TX_IMAGES_TABLE_NAME, 5 * 60L).Wait();
        }

        /// <summary>
        /// This example writes two items.
        /// </summary>
        public virtual void twoItemTransaction()
        {
            print("\n*** twoItemTransaction() ***\n");

            // Create a new transaction from the transaction manager
            Transaction t1 = txManager.newTransaction();

            // Add a new PutItem request to the transaction object (instead of on the AmazonDynamoDBClient client)
            Dictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue>();
            item1[EXAMPLE_TABLE_HASH_KEY] = new AttributeValue("Item1");
            print("Put item: " + item1);
            t1.putItemAsync(new PutItemRequest
            {
                TableName = EXAMPLE_TABLE_NAME,
                Item = item1
            }).Wait();
            print("At this point Item1 is in the table, but is not yet committed");

            // Add second PutItem request for a different item to the transaction object
            Dictionary<string, AttributeValue> item2 = new Dictionary<string, AttributeValue>();
            item2[EXAMPLE_TABLE_HASH_KEY] = new AttributeValue("Item2");
            print("Put item: " + item2);
            t1.putItemAsync(new PutItemRequest
            {
                TableName = EXAMPLE_TABLE_NAME,
                Item = item2
            }).Wait();
            print("At this point Item2 is in the table, but is not yet committed");

            // Commit the transaction.  
            t1.commitAsync().Wait();
            print("Committed transaction.  Item1 and Item2 are now both committed.");

            t1.deleteAsync().Wait();
            print("Deleted the transaction item.");
        }

        /// <summary>
        /// This example demonstrates two transactions attempting to write to the same item.  
        /// Only one transaction will go through.
        /// </summary>
        public virtual void conflictingTransactions()
        {
            print("\n*** conflictingTransactions() ***\n");
            // Start transaction t1
            Transaction t1 = txManager.newTransaction();

            // Construct a primary key of an item that will overlap between two transactions.
            Dictionary<string, AttributeValue> item1Key = new Dictionary<string, AttributeValue>();
            item1Key[EXAMPLE_TABLE_HASH_KEY] = new AttributeValue("conflictingTransactions_Item1");

            // Add a new PutItem request to the transaction object (instead of on the AmazonDynamoDBClient client)
            // This will eventually get rolled back when t2 tries to work on the same item
            Dictionary<string, AttributeValue> item1T1 = new Dictionary<string, AttributeValue>(item1Key);
            item1T1["WhichTransaction?"] = new AttributeValue("t1");
            print("T1 - Put item: " + item1T1);
            t1.putItemAsync(new PutItemRequest
            {
                TableName = EXAMPLE_TABLE_NAME,
                Item = item1T1
            }).Wait();
            print("T1 - At this point Item1 is in the table, but is not yet committed");

            Dictionary<string, AttributeValue> item2T1 = new Dictionary<string, AttributeValue>();
            item2T1[EXAMPLE_TABLE_HASH_KEY] = new AttributeValue("conflictingTransactions_Item2");
            print("T1 - Put a second, non-overlapping item: " + item2T1);
            t1.putItemAsync(new PutItemRequest
            {
                TableName = EXAMPLE_TABLE_NAME,
                Item = item2T1
            }).Wait();
            print("T1 - At this point Item2 is also in the table, but is not yet committed");

            // Start a new transaction t2
            Transaction t2 = txManager.newTransaction();

            Dictionary<string, AttributeValue> item1T2 = new Dictionary<string, AttributeValue>(item1Key);
            item1T1["WhichTransaction?"] = new AttributeValue("t2 - I win!");
            print("T2 - Put item: " + item1T2);
            t2.putItemAsync(new PutItemRequest
            {
                TableName = EXAMPLE_TABLE_NAME,
                Item = item1T2
            }).Wait();
            print("T2 - At this point Item1 from t2 is in the table, but is not yet committed. t1 was rolled back.");

            // To prove that t1 will have been rolled back by this point, attempt to commitAsync it.
            try
            {
                print("Attempting to commitAsync t1 (this will fail)");
                t1.commitAsync().Wait();
                throw new Exception("T1 should have been rolled back. This is a bug.");
            }
            catch (TransactionRolledBackException)
            {
                print("Transaction t1 was rolled back, as expected");
                t1.deleteAsync().Wait(); // Delete it, no longer needed
            }

            // Now put a second item as a part of t2
            Dictionary<string, AttributeValue> item3T2 = new Dictionary<string, AttributeValue>();
            item3T2[EXAMPLE_TABLE_HASH_KEY] = new AttributeValue("conflictingTransactions_Item3");
            print("T2 - Put item: " + item3T2);
            t2.putItemAsync(new PutItemRequest
            {
                TableName = EXAMPLE_TABLE_NAME,
                Item = item3T2
            }).Wait();
            print("T2 - At this point Item3 is in the table, but is not yet committed");

            print("Committing and deleting t2");
            t2.commitAsync().Wait();

            t2.deleteAsync().Wait();

            // Now to verify, get the items Item1, Item2, and Item3.
            // More on read operations later. 
            GetItemResponse result = txManager.GetItemAsync(
                new GetItemRequest
                {
                    TableName = EXAMPLE_TABLE_NAME,
                    Key = item1Key
                },
                Transaction.IsolationLevel.UNCOMMITTED,
                CancellationToken.None)
                .Result;

            print("Notice that t2's write to Item1 won: " + result.Item);

            result = txManager.GetItemAsync(
                new GetItemRequest
                {
                    TableName = EXAMPLE_TABLE_NAME,
                    Key = item3T2
                },
                Transaction.IsolationLevel.UNCOMMITTED,
                CancellationToken.None)
                .Result;
            print("Notice that t2's write to Item3 also went through: " + result.Item);

            result = txManager.GetItemAsync(
                new GetItemRequest
                {
                    TableName = EXAMPLE_TABLE_NAME,
                    Key = item2T1
                },
                Transaction.IsolationLevel.UNCOMMITTED,
                CancellationToken.None)
                .Result;
            print("However, t1's write to Item2 did not go through (since Item2 is null): " + result.Item);
        }

        /// <summary>
        /// This example shows the kinds of exceptions that you might need to handle
        /// </summary>
        public virtual void errorHandling()
        {
            print("\n*** errorHandling() ***\n");

            // Create a new transaction from the transaction manager
            Transaction t1 = txManager.newTransaction();

            bool success = false;
            try
            {
                // Add a new PutItem request to the transaction object (instead of on the AmazonDynamoDBClient client)
                Dictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue>();
                item1[EXAMPLE_TABLE_HASH_KEY] = new AttributeValue("Item1");
                print("Put item: " + item1);
                t1.putItemAsync(new PutItemRequest
                {
                    TableName = EXAMPLE_TABLE_NAME,
                    Item = item1
                }).Wait();

                // Commit the transaction.  
                t1.commitAsync().Wait();
                success = true;
                print("Committed transaction.  We aren't actually expecting failures in this example.");
            }
            catch (TransactionRolledBackException e)
            {
                // This gets thrown if the transaction was rolled back by another transaction
                throw e;
            }
            catch (ItemNotLockedException e)
            {
                // This gets thrown if there is too much contention with other transactions for the item you're trying to lock
                throw e;
            }
            catch (DuplicateRequestException e)
            {
                // This happens if you try to do two write operations on the same item in the same transaction
                throw e;
            }
            catch (InvalidRequestException e)
            {
                // This happens if you do something like forget the TableName or key attributes in the request
                throw e;
            }
            catch (TransactionException e)
            {
                // All exceptions thrown directly by this library derive from this.  It is a catch-all
                throw e;
            }
            catch (AmazonServiceException e)
            {
                // However, your own requests can still fail if they're invalid.  For example, you can get a 
                // ValidationException if you try to add a "number" to a "string" in UpdateItem.  So you have to handle
                // errors from DynamoDB in the same way you did before.  Except now you should roll back the transaction if it fails.
                throw e;
            }
            finally
            {
                if (!success)
                {
                    // It can be a good idea to use a "success" flag in this way, to ensure that you roll back if you get any exceptions 
                    // from the transaction library, or from DynamoDB, or any from the DynamoDB client library.  These 3 exception base classes are:
                    // TransactionException, AmazonServiceException, or AmazonClientExeption.
                    // If you forget to roll back, no problem - another transaction will come along and roll yours back eventually.
                    try
                    {
                        t1.rollbackAsync().Wait();
                    }
                    catch (TransactionException)
                    {
                    } // ignore, but should probably log
                }

                try
                {
                    t1.deleteAsync().Wait();
                }
                catch (TransactionException)
                {
                } // ignore, but should probably log
            }
        }

        /// <summary>
        /// This example shows an example of how to handle errors
        /// </summary>
        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public void badRequest() throws RuntimeException
        public virtual void badRequest()
        {
            print("\n*** badRequest() ***\n");

            // Create a "success" flag and set it to false.  We'll roll back the transaction in a finally {} if this wasn't set to true by then.
            bool success = false;
            Transaction t1 = txManager.newTransaction();

            try
            {
                // Construct a request that we know DynamoDB will reject.
                Dictionary<string, AttributeValue> key = new Dictionary<string, AttributeValue>();
                key[EXAMPLE_TABLE_HASH_KEY] = new AttributeValue("Item1");

                // You cannot "add" a String type attribute.  This request will be rejected by DynamoDB.
                Dictionary<string, AttributeValueUpdate> updates = new Dictionary<string, AttributeValueUpdate>();
                updates["Will this work?"] = new AttributeValueUpdate
                {
                    Action = AttributeAction.ADD,
                    Value = new AttributeValue("Nope.")
                };

                // The transaction library will make the request here, so we actually see
                print("Making invalid request");
                t1.updateItemAsync(new UpdateItemRequest
                {
                    TableName = EXAMPLE_TABLE_NAME,
                    Key = key,
                    AttributeUpdates = updates
                }).Wait();

                t1.commitAsync().Wait();
                success = true;
                throw new Exception("This should NOT have happened (actually should have failed before commitAsync)");
            }
            catch (AmazonServiceException e)
            {
                print("Caught a ValidationException. This is what we expected. The transaction will be rolled back: " + e.Message);
                // in a real application, you'll probably want to throw an exception to your caller 
            }
            finally
            {
                if (!success)
                {
                    print("The transaction didn't work, as expected.  Rolling back.");
                    // It can be a good idea to use a "success" flag in this way, to ensure that you roll back if you get any exceptions 
                    // from the transaction library, or from DynamoDB, or any from the DynamoDB client library.  These 3 exception base classes are:
                    // TransactionException, AmazonServiceException, or AmazonClientExeption.
                    // If you forget to roll back, no problem - another transaction will come along and roll yours back eventually.
                    try
                    {
                        t1.rollbackAsync().Wait();
                    }
                    catch (TransactionException)
                    {
                    } // ignore, but should probably log
                }

                try
                {
                    t1.deleteAsync().Wait();
                }
                catch (TransactionException)
                {
                } // ignore, but should probably log
            }
        }

        /// <summary>
        /// This example shows that reads can be performed in a transaction, and read locks can be upgraded to write locks. 
        /// </summary>
        public virtual void readThenWrite()
        {
            print("\n*** readThenWrite() ***\n");

            Transaction t1 = txManager.newTransaction();

            // Perform a GetItem request on the transaction
            print("Reading Item1");
            Dictionary<string, AttributeValue> key1 = new Dictionary<string, AttributeValue>();
            key1[EXAMPLE_TABLE_HASH_KEY] = new AttributeValue("Item1");

            Dictionary<string, AttributeValue> item1 = t1.getItemAsync(
                new GetItemRequest
                {
                    Key = key1,
                    TableName = EXAMPLE_TABLE_NAME
                }).Result.Item;
            print("Item1: " + item1);

            // Now callAsync UpdateItem to add a new attribute.
            // Notice that the library supports ReturnValues in writes
            print("Updating Item1");
            Dictionary<string, AttributeValueUpdate> updates = new Dictionary<string, AttributeValueUpdate>();
            updates["Color"] = new AttributeValueUpdate
            {
                Action = AttributeAction.PUT,
                Value = new AttributeValue("Green")
            };

            item1 = t1.updateItemAsync(new UpdateItemRequest
            {
                Key = key1,
                TableName = EXAMPLE_TABLE_NAME,
                AttributeUpdates = updates,
                ReturnValues = ReturnValue.ALL_NEW
            }).Result.Attributes;

            print("Item1 is now: " + item1);

            t1.commitAsync().Wait();

            t1.deleteAsync().Wait();
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @DynamoDBTable(tableName = EXAMPLE_TABLE_NAME) public static class ExampleItem
        public class ExampleItem
        {

            internal string itemId;
            internal string value;
            internal long? version;

            //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
            //ORIGINAL LINE: @DynamoDBHashKey(attributeName = "ItemId") public String getItemId()
            public virtual string ItemId
            {
                get
                {
                    return itemId;
                }
                set
                {
                    this.itemId = value;
                }
            }


            public virtual string Value
            {
                get
                {
                    return value;
                }
                set
                {
                    this.value = value;
                }
            }


            //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
            //ORIGINAL LINE: @DynamoDBVersionAttribute public Nullable<long> getVersion()
            public virtual long? Version
            {
                get
                {
                    return version;
                }
                set
                {
                    this.version = value;
                }
            }


        }

        /// <summary>
        /// This example shows how to conditionally create or update an item in a transaction.
        /// </summary>
        public virtual void conditionallyCreateOrUpdateWithMapper()
        {
            print("\n*** conditionallyCreateOrUpdateWithMapper() ***\n");

            Transaction t1 = txManager.newTransaction();

            print("Reading Item1");
            ExampleItem keyItem = new ExampleItem();
            keyItem.ItemId = "Item1";

            // Performs a GetItem request on the transaction
            ExampleItem item = t1.loadAsync(keyItem).Result;
            if (item != null)
            {
                print("Item1: " + item.Value);
                print("Item1 version: " + item.Version);

                print("Updating Item1");
                item.Value = "Magenta";

                // Performs an UpdateItem request after verifying the version is unchanged as of this transaction
                t1.saveAsync(item);
                print("Item1 is now: " + item.Value);
                print("Item1 version is now: " + item.Version);
            }
            else
            {
                print("Creating Item1");
                item = new ExampleItem();
                item.ItemId = keyItem.ItemId;
                item.Value = "Violet";

                // Performs a CreateItem request after verifying the version attribute is not set as of this transaction
                t1.saveAsync(item);

                print("Item1 is now: " + item.Value);
                print("Item1 version is now: " + item.Version);
            }

            t1.commitAsync();
            t1.deleteAsync();
        }

        /// <summary>
        /// Demonstrates the 3 levels of supported read isolation: Uncommitted, Committed, Locked
        /// </summary>
        public virtual void reading()
        {
            print("\n*** reading() ***\n");

            // First, create a new transaction and update Item1, but don't commitAsync yet.
            print("Starting a transaction to modify Item1");
            Transaction t1 = txManager.newTransaction();

            Dictionary<string, AttributeValue> key1 = new Dictionary<string, AttributeValue>();
            key1[EXAMPLE_TABLE_HASH_KEY] = new AttributeValue("Item1");

            // Show the current value before any changes
            print("Getting the current value of Item1 within the transaction.  This is the strongest form of read isolation.");
            print("  However, you can't trust the value you get back until your transaction commits!");
            Dictionary<string, AttributeValue> item1 = t1.getItemAsync(new GetItemRequest
            {
                Key = key1,
                TableName = EXAMPLE_TABLE_NAME
            }).Result.Item;
            print("Before any changes, Item1 is: " + item1);

            // Show the current value before any changes
            print("Changing the Color of Item1, but not committing yet");
            Dictionary<string, AttributeValueUpdate> updates = new Dictionary<string, AttributeValueUpdate>();
            updates["Color"] = new AttributeValueUpdate
            {
                Action = AttributeAction.PUT,
                Value = new AttributeValue("Purple")
            };

            item1 = t1.updateItemAsync(new UpdateItemRequest
            {
                Key = key1,
                TableName = EXAMPLE_TABLE_NAME,
                AttributeUpdates = updates,
                ReturnValues = ReturnValue.ALL_NEW
            }).Result.Attributes;
            print("Item1 is not yet committed, but if committed, will be: " + item1);

            // Perform an Uncommitted read
            print("The weakest (but cheapest) form of read is Uncommitted, where you can get back changes that aren't yet committed");
            print("  And might be rolled back!");
            item1 = txManager.GetItemAsync(new GetItemRequest() // Uncommitted reads happen on the transaction manager, not on a transaction.
            {
                Key = key1,
                TableName = EXAMPLE_TABLE_NAME,
            }, Transaction.IsolationLevel.UNCOMMITTED, CancellationToken.None).Result.Item; // Note the isloationLevel
            print("The read, which could return changes that will be rolled back, returned: " + item1);

            // Perform a Committed read
            print("A strong read is Committed.  This means that you are guaranteed to read only committed changes,");
            print("  but not necessarily the *latest* committed change!");
            item1 = txManager.GetItemAsync(new GetItemRequest // Uncommitted reads happen on the transaction manager, not on a transaction.
            {
                Key = key1,
                TableName = EXAMPLE_TABLE_NAME
            }, Transaction.IsolationLevel.COMMITTED, CancellationToken.None).Result.Item; // Note the isloationLevel
            print("The read, which should return the same value of the original read, returned: " + item1);

            // Now start a new transaction and do a read, write, and read in it
            Transaction t2 = txManager.newTransaction();

            print("Getting Item1, but this time under a new transaction t2");
            item1 = t2.getItemAsync(new GetItemRequest
            {
                Key = key1,
                TableName = EXAMPLE_TABLE_NAME
            }).Result.Item;
            print("Before any changes, in t2, Item1 is: " + item1);
            print(" This just rolled back transaction t1! Notice that this is the same value as *before* t1!");

            updates = new Dictionary<string, AttributeValueUpdate>();
            updates["Color"] = new AttributeValueUpdate
            {
                Action = AttributeAction.PUT,
                Value = new AttributeValue("Magenta")
            };

            print("Updating item1 again, but now under t2");
            item1 = t2.updateItemAsync(new UpdateItemRequest
            {
                Key = key1,
                TableName = EXAMPLE_TABLE_NAME,
                AttributeUpdates = updates,
                ReturnValues = ReturnValue.ALL_NEW
            }).Result.Attributes;
            print("Item1 is not yet committed, but if committed, will now be: " + item1);

            print("Getting Item1, again, under lock in t2.  Notice that the transaction library makes your write during this transaction visible to future reads.");
            item1 = t2.getItemAsync(new GetItemRequest
            {
                Key = key1,
                TableName = EXAMPLE_TABLE_NAME
            }).Result.Item;
            print("Under transaction t2, Item1 is going to be: " + item1);

            print("Committing t2");
            t2.commitAsync().Wait();

            try
            {
                print("Committing t1 (this will fail because it was rolled back)");
                t1.commitAsync().Wait();
                throw new Exception("Should have been rolled back");
            }
            catch (TransactionRolledBackException)
            {
                print("t1 was rolled back as expected.  I hope you didn't act on the GetItem you did under the lock in t1!");
            }
        }

        /// <summary>
        /// Demonstrates reading with COMMITTED isolation level using the mapper.
        /// </summary>
        public virtual void readCommittedWithMapper()
        {
            print("\n*** readCommittedWithMapper() ***\n");

            print("Reading Item1 with IsolationLevel.COMMITTED");
            ExampleItem keyItem = new ExampleItem();
            keyItem.ItemId = "Item1";
            ExampleItem item = txManager.loadAsync(keyItem, Transaction.IsolationLevel.COMMITTED).Result;

            print("Committed value of Item1: " + item.Value);
        }

        public virtual void sweepForStuckAndOldTransactions()
        {
            print("\n*** sweepForStuckAndOldTransactions() ***\n");

            // The scan should be done in a loop to follow the LastEvaluatedKey, and done with following the best practices for scanning a table.
            // This includes sleeping between pages, using Limit to limit the throughput of each operation to avoid hotspots,
            // and using parallel scan.
            print("Scanning one full page of the transactions table");
            ScanResponse result = dynamodb.ScanAsync(new ScanRequest
                {
                    TableName = TX_TABLE_NAME
                }).Result;

            // Pick some duration where transactions should be rolled back if they were sitting there PENDING.
            // 
            //long rollbackAfterDurationMills = 5 * 60 * 1000; // Must be idle and PENDING for 5 minutes to be rolled back
            //long deleteAfterDurationMillis = 24 * 60 * 60 * 1000; // Must be completed for 24 hours to be deleted
            long rollbackAfterDurationMills = 1;
            long deleteAfterDurationMillis = 1;
            foreach (Dictionary<string, AttributeValue> txItem in result.Items)
            {
                print("Sweeping transaction " + txItem);
                try
                {
                    if (TransactionManager.isTransactionItem(txItem))
                    {
                        Transaction t = txManager.resumeTransaction(txItem);
                        t.sweepAsync(rollbackAfterDurationMills, deleteAfterDurationMillis).Wait();
                        print("  - Swept transaction (but it might have been skipped)");
                    }
                }
                catch (TransactionException e)
                {
                    // Log and report an error "unsticking" this transaction, but keep going.
                    print("  - Error sweeping transaction " + txItem + " " + e);
                }
            }

        }

        private static void print(string line)
        {
            Console.WriteLine(line.ToString());
        }
    }

}