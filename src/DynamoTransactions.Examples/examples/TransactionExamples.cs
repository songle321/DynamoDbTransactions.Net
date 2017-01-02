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

        protected internal const string TxTableName = "Transactions";
        protected internal const string TxImagesTableName = "TransactionImages";
        protected internal const string ExampleTableName = "TransactionExamples";
        protected internal const string ExampleTableHashKey = "ItemId";
        protected internal const string DynamodbEndpoint = "http://dynamodb.us-west-2.amazonaws.com";

        protected internal readonly AmazonDynamoDBClient Dynamodb;
        protected internal readonly TransactionManager TxManager;

        public static void Main(string[] args)
        {
            Print("Running DynamoDB transaction examples");
            try
            {
                (new TransactionExamples()).Run();
                Print("Exiting normally");
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

            Dynamodb = new AmazonDynamoDBClient(credentials, new AmazonDynamoDBConfig
            {
                ServiceURL = DynamodbEndpoint
            });
            TxManager = new TransactionManager(Dynamodb, TxTableName, TxImagesTableName);
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public void run() throws Exception
        public virtual void Run()
        {
            Setup();
            TwoItemTransaction();
            ConflictingTransactions();
            ErrorHandling();
            BadRequest();
            ReadThenWrite();
            ConditionallyCreateOrUpdateWithMapper();
            Reading();
            ReadCommittedWithMapper();
            SweepForStuckAndOldTransactions();
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public void setup() throws Exception
        public virtual void Setup()
        {
            Print("\n*** setup() ***\n");
            TableHelper tableHelper = new TableHelper(Dynamodb);

            // 1. Verify that the transaction table exists, or create it if it doesn't exist
            Print("Verifying or creating table " + TxTableName);
            TransactionManager.VerifyOrCreateTransactionTableAsync(Dynamodb, TxTableName, 1, 1, null).Wait();

            // 2. Verify that the transaction item images table exists, or create it otherwise
            Print("Verifying or creating table " + TxImagesTableName);
            TransactionManager.VerifyOrCreateTransactionImagesTableAsync(Dynamodb, TxImagesTableName, 1, 1, null).Wait();

            // 3. Create a table to do transactions on
            Print("Verifying or creating table " + ExampleTableName);
            List<AttributeDefinition> attributeDefinitions = Arrays.AsList((new AttributeDefinition()).WithAttributeName(ExampleTableHashKey).WithAttributeType(ScalarAttributeType.S));
            List<KeySchemaElement> keySchema = Arrays.AsList((new KeySchemaElement()).WithAttributeName(ExampleTableHashKey).WithKeyType(KeyType.HASH));
            ProvisionedThroughput provisionedThroughput = (new ProvisionedThroughput
            {
                ReadCapacityUnits = 1L,
                WriteCapacityUnits = 1L
            });

            tableHelper.VerifyOrCreateTableAsync(ExampleTableName, attributeDefinitions, keySchema, null, provisionedThroughput, null).Wait();

            // 4. Wait for tables to be created
            Print("Waiting for table to become ACTIVE: " + ExampleTableName);
            tableHelper.WaitForTableActiveAsync(ExampleTableName, 5 * 60L).Wait();
            Print("Waiting for table to become ACTIVE: " + TxTableName);
            tableHelper.WaitForTableActiveAsync(TxTableName, 5 * 60L).Wait();
            Print("Waiting for table to become ACTIVE: " + TxImagesTableName);
            tableHelper.WaitForTableActiveAsync(TxImagesTableName, 5 * 60L).Wait();
        }

        /// <summary>
        /// This example writes two items.
        /// </summary>
        public virtual void TwoItemTransaction()
        {
            Print("\n*** twoItemTransaction() ***\n");

            // Create a new transaction from the transaction manager
            Transaction t1 = TxManager.NewTransaction();

            // Add a new PutItem request to the transaction object (instead of on the AmazonDynamoDBClient client)
            Dictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue>();
            item1[ExampleTableHashKey] = new AttributeValue("Item1");
            Print("Put item: " + item1);
            t1.PutItemAsync(new PutItemRequest
            {
                TableName = ExampleTableName,
                Item = item1
            }).Wait();
            Print("At this point Item1 is in the table, but is not yet committed");

            // Add second PutItem request for a different item to the transaction object
            Dictionary<string, AttributeValue> item2 = new Dictionary<string, AttributeValue>();
            item2[ExampleTableHashKey] = new AttributeValue("Item2");
            Print("Put item: " + item2);
            t1.PutItemAsync(new PutItemRequest
            {
                TableName = ExampleTableName,
                Item = item2
            }).Wait();
            Print("At this point Item2 is in the table, but is not yet committed");

            // Commit the transaction.  
            t1.CommitAsync().Wait();
            Print("Committed transaction.  Item1 and Item2 are now both committed.");

            t1.DeleteAsync().Wait();
            Print("Deleted the transaction item.");
        }

        /// <summary>
        /// This example demonstrates two transactions attempting to write to the same item.  
        /// Only one transaction will go through.
        /// </summary>
        public virtual void ConflictingTransactions()
        {
            Print("\n*** conflictingTransactions() ***\n");
            // Start transaction t1
            Transaction t1 = TxManager.NewTransaction();

            // Construct a primary key of an item that will overlap between two transactions.
            Dictionary<string, AttributeValue> item1Key = new Dictionary<string, AttributeValue>();
            item1Key[ExampleTableHashKey] = new AttributeValue("conflictingTransactions_Item1");

            // Add a new PutItem request to the transaction object (instead of on the AmazonDynamoDBClient client)
            // This will eventually get rolled back when t2 tries to work on the same item
            Dictionary<string, AttributeValue> item1T1 = new Dictionary<string, AttributeValue>(item1Key);
            item1T1["WhichTransaction?"] = new AttributeValue("t1");
            Print("T1 - Put item: " + item1T1);
            t1.PutItemAsync(new PutItemRequest
            {
                TableName = ExampleTableName,
                Item = item1T1
            }).Wait();
            Print("T1 - At this point Item1 is in the table, but is not yet committed");

            Dictionary<string, AttributeValue> item2T1 = new Dictionary<string, AttributeValue>();
            item2T1[ExampleTableHashKey] = new AttributeValue("conflictingTransactions_Item2");
            Print("T1 - Put a second, non-overlapping item: " + item2T1);
            t1.PutItemAsync(new PutItemRequest
            {
                TableName = ExampleTableName,
                Item = item2T1
            }).Wait();
            Print("T1 - At this point Item2 is also in the table, but is not yet committed");

            // Start a new transaction t2
            Transaction t2 = TxManager.NewTransaction();

            Dictionary<string, AttributeValue> item1T2 = new Dictionary<string, AttributeValue>(item1Key);
            item1T1["WhichTransaction?"] = new AttributeValue("t2 - I win!");
            Print("T2 - Put item: " + item1T2);
            t2.PutItemAsync(new PutItemRequest
            {
                TableName = ExampleTableName,
                Item = item1T2
            }).Wait();
            Print("T2 - At this point Item1 from t2 is in the table, but is not yet committed. t1 was rolled back.");

            // To prove that t1 will have been rolled back by this point, attempt to commitAsync it.
            try
            {
                Print("Attempting to commitAsync t1 (this will fail)");
                t1.CommitAsync().Wait();
                throw new Exception("T1 should have been rolled back. This is a bug.");
            }
            catch (TransactionRolledBackException)
            {
                Print("Transaction t1 was rolled back, as expected");
                t1.DeleteAsync().Wait(); // Delete it, no longer needed
            }

            // Now put a second item as a part of t2
            Dictionary<string, AttributeValue> item3T2 = new Dictionary<string, AttributeValue>();
            item3T2[ExampleTableHashKey] = new AttributeValue("conflictingTransactions_Item3");
            Print("T2 - Put item: " + item3T2);
            t2.PutItemAsync(new PutItemRequest
            {
                TableName = ExampleTableName,
                Item = item3T2
            }).Wait();
            Print("T2 - At this point Item3 is in the table, but is not yet committed");

            Print("Committing and deleting t2");
            t2.CommitAsync().Wait();

            t2.DeleteAsync().Wait();

            // Now to verify, get the items Item1, Item2, and Item3.
            // More on read operations later. 
            GetItemResponse result = TxManager.GetItemAsync(
                new GetItemRequest
                {
                    TableName = ExampleTableName,
                    Key = item1Key
                },
                Transaction.IsolationLevel.Uncommitted,
                CancellationToken.None)
                .Result;

            Print("Notice that t2's write to Item1 won: " + result.Item);

            result = TxManager.GetItemAsync(
                new GetItemRequest
                {
                    TableName = ExampleTableName,
                    Key = item3T2
                },
                Transaction.IsolationLevel.Uncommitted,
                CancellationToken.None)
                .Result;
            Print("Notice that t2's write to Item3 also went through: " + result.Item);

            result = TxManager.GetItemAsync(
                new GetItemRequest
                {
                    TableName = ExampleTableName,
                    Key = item2T1
                },
                Transaction.IsolationLevel.Uncommitted,
                CancellationToken.None)
                .Result;
            Print("However, t1's write to Item2 did not go through (since Item2 is null): " + result.Item);
        }

        /// <summary>
        /// This example shows the kinds of exceptions that you might need to handle
        /// </summary>
        public virtual void ErrorHandling()
        {
            Print("\n*** errorHandling() ***\n");

            // Create a new transaction from the transaction manager
            Transaction t1 = TxManager.NewTransaction();

            bool success = false;
            try
            {
                // Add a new PutItem request to the transaction object (instead of on the AmazonDynamoDBClient client)
                Dictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue>();
                item1[ExampleTableHashKey] = new AttributeValue("Item1");
                Print("Put item: " + item1);
                t1.PutItemAsync(new PutItemRequest
                {
                    TableName = ExampleTableName,
                    Item = item1
                }).Wait();

                // Commit the transaction.  
                t1.CommitAsync().Wait();
                success = true;
                Print("Committed transaction.  We aren't actually expecting failures in this example.");
            }
            catch (TransactionRolledBackException)
            {
                // This gets thrown if the transaction was rolled back by another transaction
                throw;
            }
            catch (ItemNotLockedException)
            {
                // This gets thrown if there is too much contention with other transactions for the item you're trying to lock
                throw;
            }
            catch (DuplicateRequestException)
            {
                // This happens if you try to do two write operations on the same item in the same transaction
                throw;
            }
            catch (InvalidRequestException)
            {
                // This happens if you do something like forget the TableName or key attributes in the request
                throw;
            }
            catch (TransactionException)
            {
                // All exceptions thrown directly by this library derive from this.  It is a catch-all
                throw;
            }
            catch (AmazonServiceException)
            {
                // However, your own requests can still fail if they're invalid.  For example, you can get a 
                // ValidationException if you try to add a "number" to a "string" in UpdateItem.  So you have to handle
                // errors from DynamoDB in the same way you did before.  Except now you should roll back the transaction if it fails.
                throw;
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
                        t1.RollbackAsync().Wait();
                    }
                    catch (TransactionException)
                    {
                    } // ignore, but should probably log
                }

                try
                {
                    t1.DeleteAsync().Wait();
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
        public virtual void BadRequest()
        {
            Print("\n*** badRequest() ***\n");

            // Create a "success" flag and set it to false.  We'll roll back the transaction in a finally {} if this wasn't set to true by then.
            bool success = false;
            Transaction t1 = TxManager.NewTransaction();

            try
            {
                // Construct a request that we know DynamoDB will reject.
                Dictionary<string, AttributeValue> key = new Dictionary<string, AttributeValue>();
                key[ExampleTableHashKey] = new AttributeValue("Item1");

                // You cannot "add" a String type attribute.  This request will be rejected by DynamoDB.
                Dictionary<string, AttributeValueUpdate> updates = new Dictionary<string, AttributeValueUpdate>();
                updates["Will this work?"] = new AttributeValueUpdate
                {
                    Action = AttributeAction.ADD,
                    Value = new AttributeValue("Nope.")
                };

                // The transaction library will make the request here, so we actually see
                Print("Making invalid request");
                t1.UpdateItemAsync(new UpdateItemRequest
                {
                    TableName = ExampleTableName,
                    Key = key,
                    AttributeUpdates = updates
                }).Wait();

                t1.CommitAsync().Wait();
                success = true;
                throw new Exception("This should NOT have happened (actually should have failed before commitAsync)");
            }
            catch (AmazonServiceException e)
            {
                Print("Caught a ValidationException. This is what we expected. The transaction will be rolled back: " + e.Message);
                // in a real application, you'll probably want to throw an exception to your caller 
            }
            finally
            {
                if (!success)
                {
                    Print("The transaction didn't work, as expected.  Rolling back.");
                    // It can be a good idea to use a "success" flag in this way, to ensure that you roll back if you get any exceptions 
                    // from the transaction library, or from DynamoDB, or any from the DynamoDB client library.  These 3 exception base classes are:
                    // TransactionException, AmazonServiceException, or AmazonClientExeption.
                    // If you forget to roll back, no problem - another transaction will come along and roll yours back eventually.
                    try
                    {
                        t1.RollbackAsync().Wait();
                    }
                    catch (TransactionException)
                    {
                    } // ignore, but should probably log
                }

                try
                {
                    t1.DeleteAsync().Wait();
                }
                catch (TransactionException)
                {
                } // ignore, but should probably log
            }
        }

        /// <summary>
        /// This example shows that reads can be performed in a transaction, and read locks can be upgraded to write locks. 
        /// </summary>
        public virtual void ReadThenWrite()
        {
            Print("\n*** readThenWrite() ***\n");

            Transaction t1 = TxManager.NewTransaction();

            // Perform a GetItem request on the transaction
            Print("Reading Item1");
            Dictionary<string, AttributeValue> key1 = new Dictionary<string, AttributeValue>();
            key1[ExampleTableHashKey] = new AttributeValue("Item1");

            Dictionary<string, AttributeValue> item1 = t1.GetItemAsync(
                new GetItemRequest
                {
                    Key = key1,
                    TableName = ExampleTableName
                }).Result.Item;
            Print("Item1: " + item1);

            // Now callAsync UpdateItem to add a new attribute.
            // Notice that the library supports ReturnValues in writes
            Print("Updating Item1");
            Dictionary<string, AttributeValueUpdate> updates = new Dictionary<string, AttributeValueUpdate>();
            updates["Color"] = new AttributeValueUpdate
            {
                Action = AttributeAction.PUT,
                Value = new AttributeValue("Green")
            };

            item1 = t1.UpdateItemAsync(new UpdateItemRequest
            {
                Key = key1,
                TableName = ExampleTableName,
                AttributeUpdates = updates,
                ReturnValues = ReturnValue.ALL_NEW
            }).Result.Attributes;

            Print("Item1 is now: " + item1);

            t1.CommitAsync().Wait();

            t1.DeleteAsync().Wait();
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
        public virtual void ConditionallyCreateOrUpdateWithMapper()
        {
            Print("\n*** conditionallyCreateOrUpdateWithMapper() ***\n");

            Transaction t1 = TxManager.NewTransaction();

            Print("Reading Item1");
            ExampleItem keyItem = new ExampleItem();
            keyItem.ItemId = "Item1";

            // Performs a GetItem request on the transaction
            ExampleItem item = t1.LoadAsync(keyItem).Result;
            if (item != null)
            {
                Print("Item1: " + item.Value);
                Print("Item1 version: " + item.Version);

                Print("Updating Item1");
                item.Value = "Magenta";

                // Performs an UpdateItem request after verifying the version is unchanged as of this transaction
                t1.SaveAsync(item).Wait();
                Print("Item1 is now: " + item.Value);
                Print("Item1 version is now: " + item.Version);
            }
            else
            {
                Print("Creating Item1");
                item = new ExampleItem();
                item.ItemId = keyItem.ItemId;
                item.Value = "Violet";

                // Performs a CreateItem request after verifying the version attribute is not set as of this transaction
                t1.SaveAsync(item).Wait();

                Print("Item1 is now: " + item.Value);
                Print("Item1 version is now: " + item.Version);
            }

            t1.CommitAsync().Wait();
            t1.DeleteAsync().Wait();
        }

        /// <summary>
        /// Demonstrates the 3 levels of supported read isolation: Uncommitted, Committed, Locked
        /// </summary>
        public virtual void Reading()
        {
            Print("\n*** reading() ***\n");

            // First, create a new transaction and update Item1, but don't commitAsync yet.
            Print("Starting a transaction to modify Item1");
            Transaction t1 = TxManager.NewTransaction();

            Dictionary<string, AttributeValue> key1 = new Dictionary<string, AttributeValue>();
            key1[ExampleTableHashKey] = new AttributeValue("Item1");

            // Show the current value before any changes
            Print("Getting the current value of Item1 within the transaction.  This is the strongest form of read isolation.");
            Print("  However, you can't trust the value you get back until your transaction commits!");
            Dictionary<string, AttributeValue> item1 = t1.GetItemAsync(new GetItemRequest
            {
                Key = key1,
                TableName = ExampleTableName
            }).Result.Item;
            Print("Before any changes, Item1 is: " + item1);

            // Show the current value before any changes
            Print("Changing the Color of Item1, but not committing yet");
            Dictionary<string, AttributeValueUpdate> updates = new Dictionary<string, AttributeValueUpdate>();
            updates["Color"] = new AttributeValueUpdate
            {
                Action = AttributeAction.PUT,
                Value = new AttributeValue("Purple")
            };

            item1 = t1.UpdateItemAsync(new UpdateItemRequest
            {
                Key = key1,
                TableName = ExampleTableName,
                AttributeUpdates = updates,
                ReturnValues = ReturnValue.ALL_NEW
            }).Result.Attributes;
            Print("Item1 is not yet committed, but if committed, will be: " + item1);

            // Perform an Uncommitted read
            Print("The weakest (but cheapest) form of read is Uncommitted, where you can get back changes that aren't yet committed");
            Print("  And might be rolled back!");
            item1 = TxManager.GetItemAsync(new GetItemRequest() // Uncommitted reads happen on the transaction manager, not on a transaction.
            {
                Key = key1,
                TableName = ExampleTableName,
            }, Transaction.IsolationLevel.Uncommitted, CancellationToken.None).Result.Item; // Note the isloationLevel
            Print("The read, which could return changes that will be rolled back, returned: " + item1);

            // Perform a Committed read
            Print("A strong read is Committed.  This means that you are guaranteed to read only committed changes,");
            Print("  but not necessarily the *latest* committed change!");
            item1 = TxManager.GetItemAsync(new GetItemRequest // Uncommitted reads happen on the transaction manager, not on a transaction.
            {
                Key = key1,
                TableName = ExampleTableName
            }, Transaction.IsolationLevel.Committed, CancellationToken.None).Result.Item; // Note the isloationLevel
            Print("The read, which should return the same value of the original read, returned: " + item1);

            // Now start a new transaction and do a read, write, and read in it
            Transaction t2 = TxManager.NewTransaction();

            Print("Getting Item1, but this time under a new transaction t2");
            item1 = t2.GetItemAsync(new GetItemRequest
            {
                Key = key1,
                TableName = ExampleTableName
            }).Result.Item;
            Print("Before any changes, in t2, Item1 is: " + item1);
            Print(" This just rolled back transaction t1! Notice that this is the same value as *before* t1!");

            updates = new Dictionary<string, AttributeValueUpdate>();
            updates["Color"] = new AttributeValueUpdate
            {
                Action = AttributeAction.PUT,
                Value = new AttributeValue("Magenta")
            };

            Print("Updating item1 again, but now under t2");
            item1 = t2.UpdateItemAsync(new UpdateItemRequest
            {
                Key = key1,
                TableName = ExampleTableName,
                AttributeUpdates = updates,
                ReturnValues = ReturnValue.ALL_NEW
            }).Result.Attributes;
            Print("Item1 is not yet committed, but if committed, will now be: " + item1);

            Print("Getting Item1, again, under lock in t2.  Notice that the transaction library makes your write during this transaction visible to future reads.");
            item1 = t2.GetItemAsync(new GetItemRequest
            {
                Key = key1,
                TableName = ExampleTableName
            }).Result.Item;
            Print("Under transaction t2, Item1 is going to be: " + item1);

            Print("Committing t2");
            t2.CommitAsync().Wait();

            try
            {
                Print("Committing t1 (this will fail because it was rolled back)");
                t1.CommitAsync().Wait();
                throw new Exception("Should have been rolled back");
            }
            catch (TransactionRolledBackException)
            {
                Print("t1 was rolled back as expected.  I hope you didn't act on the GetItem you did under the lock in t1!");
            }
        }

        /// <summary>
        /// Demonstrates reading with COMMITTED isolation level using the mapper.
        /// </summary>
        public virtual void ReadCommittedWithMapper()
        {
            Print("\n*** readCommittedWithMapper() ***\n");

            Print("Reading Item1 with IsolationLevel.COMMITTED");
            ExampleItem keyItem = new ExampleItem();
            keyItem.ItemId = "Item1";
            ExampleItem item = TxManager.LoadAsync(keyItem, Transaction.IsolationLevel.Committed).Result;

            Print("Committed value of Item1: " + item.Value);
        }

        public virtual void SweepForStuckAndOldTransactions()
        {
            Print("\n*** sweepForStuckAndOldTransactions() ***\n");

            // The scan should be done in a loop to follow the LastEvaluatedKey, and done with following the best practices for scanning a table.
            // This includes sleeping between pages, using Limit to limit the throughput of each operation to avoid hotspots,
            // and using parallel scan.
            Print("Scanning one full page of the transactions table");
            ScanResponse result = Dynamodb.ScanAsync(new ScanRequest
                {
                    TableName = TxTableName
                }).Result;

            // Pick some duration where transactions should be rolled back if they were sitting there PENDING.
            // 
            //long rollbackAfterDurationMills = 5 * 60 * 1000; // Must be idle and PENDING for 5 minutes to be rolled back
            //long deleteAfterDurationMillis = 24 * 60 * 60 * 1000; // Must be completed for 24 hours to be deleted
            long rollbackAfterDurationMills = 1;
            long deleteAfterDurationMillis = 1;
            foreach (Dictionary<string, AttributeValue> txItem in result.Items)
            {
                Print("Sweeping transaction " + txItem);
                try
                {
                    if (TransactionManager.IsTransactionItem(txItem))
                    {
                        Transaction t = TxManager.ResumeTransaction(txItem);
                        t.SweepAsync(rollbackAfterDurationMills, deleteAfterDurationMillis).Wait();
                        Print("  - Swept transaction (but it might have been skipped)");
                    }
                }
                catch (TransactionException e)
                {
                    // Log and report an error "unsticking" this transaction, but keep going.
                    Print("  - Error sweeping transaction " + txItem + " " + e);
                }
            }

        }

        private static void Print(string line)
        {
            Console.WriteLine(line.ToString());
        }
    }

}