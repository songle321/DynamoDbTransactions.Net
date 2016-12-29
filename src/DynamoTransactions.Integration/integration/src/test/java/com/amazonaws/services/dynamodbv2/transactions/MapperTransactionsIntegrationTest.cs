﻿using System.Collections.Generic;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using com.amazonaws.services.dynamodbv2.transactions.exceptions;
using static DynamoTransactions.Integration.AssertStatic;

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

    public class MapperTransactionsIntegrationTest : IntegrationTest
    {

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @DynamoDBTable(tableName = HASH_TABLE_NAME) public static class ExampleHashKeyItem
        public class ExampleHashKeyItem
        {
            internal string id;
            internal string something;
            internal ISet<string> someSet;

            //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
            //ORIGINAL LINE: @DynamoDBHashKey(attributeName = ID_ATTRIBUTE) @DynamoDBAutoGeneratedKey public String getId()
            public virtual string Id
            {
                get
                {
                    return id;
                }
                set
                {
                    this.id = value;
                }
            }


            //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
            //ORIGINAL LINE: @DynamoDBAttribute public String getSomething()
            public virtual string Something
            {
                get
                {
                    return something;
                }
                set
                {
                    this.something = value;
                }
            }


            //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
            //ORIGINAL LINE: @DynamoDBAttribute public java.util.Set<String> getSomeSet()
            public virtual ISet<string> SomeSet
            {
                get
                {
                    return someSet;
                }
                set
                {
                    this.someSet = value;
                }
            }


            //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
            //ORIGINAL LINE: @DynamoDBIgnore public java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> getKey()
            public virtual Dictionary<string, AttributeValue> Key
            {
                get
                {
                    return Collections.singletonMap(ID_ATTRIBUTE, new AttributeValue
                    {
                        S = Id,

                    });
                }
            }

            //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
            //ORIGINAL LINE: @DynamoDBIgnore public java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> getExpectedValues()
            public virtual Dictionary<string, AttributeValue> ExpectedValues
            {
                get
                {
                    Dictionary<string, AttributeValue> expected = new Dictionary<string, AttributeValue>();
                    expected["something"] = new AttributeValue
                    {
                        S = Something,

                    };
                    if (SomeSet != null)
                    {
                        // we need to sort the values, because AttributeValue internally
                        // copies our set to an ordered list
                        List<string> valuesList = new List<string>(SomeSet);
                        valuesList.Sort();
                        expected["someSet"] = new AttributeValue
                        {
                            SS = valuesList,

                        };
                    }
                    //JAVA TO C# CONVERTER TODO TASK: There is no .NET Dictionary equivalent to the Java 'putAll' method:
                    expected.putAll(Key);
                    return expected;
                }
            }

        }

        private ExampleHashKeyItem hashItem0;


        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Before public void setup()
        public virtual void setup()
        {
            Transaction t = manager.newTransaction();
            hashItem0 = new ExampleHashKeyItem();
            hashItem0.Id = UUID.randomUUID().ToString();
            hashItem0.Something = "val";
            hashItem0.SomeSet = new HashSet<string>(Arrays.asList("one", "two"));
            t.save(hashItem0);
            key0 = newKey(INTEG_HASH_TABLE_NAME);
            item0 = new Dictionary<string, AttributeValue>(key0);
            item0["s_someattr"] = new AttributeValue("val");
            item0["ss_otherattr"] = (new AttributeValue()).withSS("one", "two");
            Dictionary<string, AttributeValue> putResponse = t.putItem(new PutItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Item = item0,
                ReturnValues = ReturnValue.ALL_OLD,

            }).Attributes;
            assertNull(putResult);
            t.commit();
            assertItemNotLocked(INTEG_HASH_TABLE_NAME, key0, item0, true);
            assertItemNotLocked(INTEG_HASH_TABLE_NAME, hashItem0.Key, hashItem0.ExpectedValues, true);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @After public void teardown()
        public virtual void teardown()
        {
            Transaction t = manager.newTransaction();
            t.deleteItem(new DeleteItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Key = key0,

            });
            t.commit();
            assertItemNotLocked(INTEG_HASH_TABLE_NAME, key0, false);
        }

        private ExampleHashKeyItem newItem()
        {
            ExampleHashKeyItem item1 = new ExampleHashKeyItem();
            item1.Id = UUID.randomUUID().ToString();
            return item1;
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public MapperTransactionsIntegrationTest() throws java.io.IOException
        public MapperTransactionsIntegrationTest() : base(new DynamoDBMapperConfig(DynamoDBMapperConfig.TableNameOverride.withTableNamePrefix(TABLE_NAME_PREFIX + "_")))
        {
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void phantomItemFromDelete()
        public virtual void phantomItemFromDelete()
        {
            ExampleHashKeyItem item1 = newItem();
            Transaction transaction = manager.newTransaction();
            transaction.delete(item1);
            assertItemLocked(INTEG_HASH_TABLE_NAME, item1.Key, transaction.Id, true, false);
            transaction.rollback();
            assertItemNotLocked(INTEG_HASH_TABLE_NAME, item1.Key, false);
            transaction.delete(long.MaxValue);
        }

        /*
		 * GetItem tests
		 */

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void lockItem()
        public virtual void lockItem()
        {
            ExampleHashKeyItem item1 = newItem();

            Transaction t1 = manager.newTransaction();
            Transaction t2 = manager.newTransaction();

            ExampleHashKeyItem loadedItem = t1.load(item1);

            assertItemLocked(INTEG_HASH_TABLE_NAME, item1.Key, t1.Id, true, false); // we're not applying locks
            assertNull(loadedItem);

            t2.delete(item1);
            assertItemLocked(INTEG_HASH_TABLE_NAME, item1.Key, t2.Id, true, false); // we're not applying deletes either

            t2.commit();

            try
            {
                t1.commit();
                fail();
            }
            catch (TransactionRolledBackException)
            {
            }

            t1.delete(long.MaxValue);
            t2.delete(long.MaxValue);

            assertItemNotLocked(INTEG_HASH_TABLE_NAME, item1.Key, false);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void lock2Items()
        public virtual void lock2Items()
        {
            ExampleHashKeyItem item1 = newItem();
            ExampleHashKeyItem item2 = newItem();

            Transaction t0 = manager.newTransaction();
            item1.Something = "val";
            t0.save(item1);

            t0.commit();

            Transaction t1 = manager.newTransaction();

            ExampleHashKeyItem loadedItem1 = t1.load(item1);
            assertItemLocked(INTEG_HASH_TABLE_NAME, item1.Key, item1.ExpectedValues, t1.Id, false, false);
            assertEquals(item1.ExpectedValues, loadedItem1.ExpectedValues);

            ExampleHashKeyItem loadedItem2 = t1.load(item2);
            assertItemLocked(INTEG_HASH_TABLE_NAME, item1.Key, item1.ExpectedValues, t1.Id, false, false);
            assertItemLocked(INTEG_HASH_TABLE_NAME, item2.Key, t1.Id, true, false);
            assertNull(loadedItem2);

            t1.commit();
            t1.delete(long.MaxValue);

            assertItemNotLocked(INTEG_HASH_TABLE_NAME, item1.Key, item1.ExpectedValues, true);
            assertItemNotLocked(INTEG_HASH_TABLE_NAME, item2.Key, false);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void getItemWithDelete()
        public virtual void getItemWithDelete()
        {
            Transaction t1 = manager.newTransaction();
            ExampleHashKeyItem loadedItem1 = t1.load(hashItem0);
            assertEquals(hashItem0.ExpectedValues, loadedItem1.ExpectedValues);
            assertItemLocked(INTEG_HASH_TABLE_NAME, hashItem0.Key, loadedItem1.ExpectedValues, t1.Id, false, false);

            t1.delete(hashItem0);
            assertItemLocked(INTEG_HASH_TABLE_NAME, hashItem0.Key, hashItem0.ExpectedValues, t1.Id, false, false);

            ExampleHashKeyItem loadedItem2 = t1.load(hashItem0);
            assertNull(loadedItem2);
            assertItemLocked(INTEG_HASH_TABLE_NAME, hashItem0.Key, hashItem0.ExpectedValues, t1.Id, false, false);

            t1.commit();
            assertItemNotLocked(INTEG_HASH_TABLE_NAME, hashItem0.Key, false);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void getItemNotExists()
        public virtual void getItemNotExists()
        {
            Transaction t1 = manager.newTransaction();
            ExampleHashKeyItem item1 = newItem();

            ExampleHashKeyItem loadedItem1 = t1.load(item1);
            assertNull(loadedItem1);
            assertItemLocked(INTEG_HASH_TABLE_NAME, item1.Key, t1.Id, true, false);

            ExampleHashKeyItem loadedItem2 = t1.load(item1);
            assertNull(loadedItem2);
            assertItemLocked(INTEG_HASH_TABLE_NAME, item1.Key, t1.Id, true, false);

            t1.commit();

            assertItemNotLocked(INTEG_HASH_TABLE_NAME, item1.Key, false);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void getItemAfterPutItemInsert()
        public virtual void getItemAfterPutItemInsert()
        {
            Transaction t1 = manager.newTransaction();
            ExampleHashKeyItem item1 = newItem();
            item1.Something = "wef";

            ExampleHashKeyItem loadedItem1 = t1.load(item1);
            assertNull(loadedItem1);
            assertItemLocked(INTEG_HASH_TABLE_NAME, item1.Key, t1.Id, true, false);

            t1.save(item1);
            assertItemLocked(INTEG_HASH_TABLE_NAME, item1.Key, item1.ExpectedValues, t1.Id, true, true);

            ExampleHashKeyItem loadedItem2 = t1.load(item1);
            assertEquals(item1.ExpectedValues, loadedItem2.ExpectedValues);
            assertItemLocked(INTEG_HASH_TABLE_NAME, item1.Key, item1.ExpectedValues, t1.Id, true, true);

            t1.commit();

            assertItemNotLocked(INTEG_HASH_TABLE_NAME, item1.Key, item1.ExpectedValues, true);
        }

        /*
		 * Transaction isolation and error tests
		 */

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void conflictingWrites()
        public virtual void conflictingWrites()
        {
            ExampleHashKeyItem item1 = newItem();
            Transaction t1 = manager.newTransaction();
            Transaction t2 = manager.newTransaction();
            Transaction t3 = manager.newTransaction();

            // Finish t1
            ExampleHashKeyItem t1Item = new ExampleHashKeyItem();
            t1Item.Id = item1.Id;
            t1Item.Something = "t1";

            t1.save(t1Item);
            assertItemLocked(INTEG_HASH_TABLE_NAME, item1.Key, t1Item.ExpectedValues, t1.Id, true, true);

            t1.commit();
            assertItemNotLocked(INTEG_HASH_TABLE_NAME, item1.Key, t1Item.ExpectedValues, true);

            // Begin t2
            ExampleHashKeyItem t2Item = new ExampleHashKeyItem();
            t2Item.Id = item1.Id;
            t2Item.Something = "t2";
            t2Item.SomeSet = Collections.singleton("extra");

            t2.save(t2Item);
            assertItemLocked(INTEG_HASH_TABLE_NAME, item1.Key, t2Item.ExpectedValues, t2.Id, false, true);

            // Begin and finish t3
            ExampleHashKeyItem t3Item = new ExampleHashKeyItem();
            t3Item.Id = item1.Id;
            t3Item.Something = "t3";
            t3Item.SomeSet = Collections.singleton("things");

            t3.save(t3Item);
            assertItemLocked(INTEG_HASH_TABLE_NAME, item1.Key, t3Item.ExpectedValues, t3.Id, false, true);

            t3.commit();

            assertItemNotLocked(INTEG_HASH_TABLE_NAME, item1.Key, t3Item.ExpectedValues, true);

            // Ensure t2 rolled back
            try
            {
                t2.commit();
                fail();
            }
            catch (TransactionRolledBackException)
            {
            }

            t1.delete(long.MaxValue);
            t2.delete(long.MaxValue);
            t3.delete(long.MaxValue);

            assertItemNotLocked(INTEG_HASH_TABLE_NAME, item1.Key, t3Item.ExpectedValues, true);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @DynamoDBTable(tableName = HASH_TABLE_NAME) public static class ExampleVersionedHashKeyItem
        public class ExampleVersionedHashKeyItem
        {
            internal string id;
            internal long? version = null;

            //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
            //ORIGINAL LINE: @DynamoDBHashKey(attributeName = ID_ATTRIBUTE) @DynamoDBAutoGeneratedKey public String getId()
            public virtual string Id
            {
                get
                {
                    return id;
                }
                set
                {
                    this.id = value;
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


            //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
            //ORIGINAL LINE: @DynamoDBIgnore public java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> getKey()
            public virtual Dictionary<string, AttributeValue> Key
            {
                get
                {
                    return Collections.singletonMap(ID_ATTRIBUTE, new AttributeValue
                    {
                        S = Id,

                    });
                }
            }

            //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
            //ORIGINAL LINE: @DynamoDBIgnore public java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> getExpectedValues()
            public virtual Dictionary<string, AttributeValue> ExpectedValues
            {
                get
                {
                    Dictionary<string, AttributeValue> expected = new Dictionary<string, AttributeValue>();
                    expected["version"] = new AttributeValue
                    {
                        N = Version.ToString(),

                    };
                    //JAVA TO C# CONVERTER TODO TASK: There is no .NET Dictionary equivalent to the Java 'putAll' method:
                    expected.putAll(Key);
                    return expected;
                }
            }

        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void createVersionedItemWhenItemAlreadyExists()
        public virtual void createVersionedItemWhenItemAlreadyExists()
        {
            ExampleVersionedHashKeyItem item1 = newVersionedItem();

            Transaction t1 = manager.newTransaction();
            t1.save(item1);
            t1.commit();
            assertItemNotLocked(INTEG_HASH_TABLE_NAME, item1.Key, item1.ExpectedValues, true);

            ExampleVersionedHashKeyItem item2 = new ExampleVersionedHashKeyItem();
            item2.Id = item1.Id;
            Transaction t2 = manager.newTransaction();
            try
            {
                t2.save(item2);
                fail();
            }
            catch (ConditionalCheckFailedException)
            {
                t2.rollback();
            }
            assertItemNotLocked(INTEG_HASH_TABLE_NAME, item1.Key, item1.ExpectedValues, true);

            t1.delete(long.MaxValue);
            t2.delete(long.MaxValue);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void createVersionedItemInConflictingTransactions()
        public virtual void createVersionedItemInConflictingTransactions()
        {
            ExampleVersionedHashKeyItem item1 = newVersionedItem();

            // establish the item with version 1
            Transaction t1 = manager.newTransaction();
            t1.save(item1);
            assertItemLocked(INTEG_HASH_TABLE_NAME, item1.Key, item1.ExpectedValues, t1.Id, true, true);

            // while the item is being created in the first transaction, create it in another transaction
            ExampleVersionedHashKeyItem item2 = new ExampleVersionedHashKeyItem();
            item2.Id = item1.Id;
            Transaction t2 = manager.newTransaction();
            t2.save(item2);
            t2.commit();

            // try to commit the original transaction
            try
            {
                t1.commit();
                fail();
            }
            catch (TransactionRolledBackException)
            {
            }
            assertItemNotLocked(INTEG_HASH_TABLE_NAME, item1.Key, item1.ExpectedValues, true);

            t1.delete(long.MaxValue);
            t2.delete(long.MaxValue);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void deleteVersionedItemWithOutOfDateVersion()
        public virtual void deleteVersionedItemWithOutOfDateVersion()
        {
            ExampleVersionedHashKeyItem item1 = newVersionedItem();

            // establish the item with version 1
            Transaction t1 = manager.newTransaction();
            t1.save(item1);
            t1.commit();

            // update the item to version 2
            Transaction t2 = manager.newTransaction();
            ExampleVersionedHashKeyItem item2 = t2.load(item1);
            t2.save(item2);
            t2.commit();

            // try to delete with an outdated view of the item
            Transaction t3 = manager.newTransaction();
            try
            {
                t3.delete(item1);
                fail();
            }
            catch (ConditionalCheckFailedException)
            {
                t3.rollback();
            }
            assertItemNotLocked(INTEG_HASH_TABLE_NAME, item2.Key, item2.ExpectedValues, true);

            t1.delete(long.MaxValue);
            t2.delete(long.MaxValue);
            t3.delete(long.MaxValue);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void reusingMapperInstanceWithOutOfDateVersionThrowsOnSave()
        public virtual void reusingMapperInstanceWithOutOfDateVersionThrowsOnSave()
        {
            ExampleVersionedHashKeyItem item1 = newVersionedItem();

            // establish the item with version 1
            Transaction t1 = manager.newTransaction();
            t1.save(item1);
            t1.commit();

            // update the item to version 2 and save
            Transaction t2 = manager.newTransaction();
            ExampleVersionedHashKeyItem item2 = t2.load(item1);
            t2.save(item2);
            t2.commit();

            Transaction t3 = manager.newTransaction();
            t3.load(item1);
            try
            {
                t3.save(item1);
                fail();
            }
            catch (ConditionalCheckFailedException)
            {
                t3.rollback();
            }
            assertItemNotLocked(INTEG_HASH_TABLE_NAME, item2.Key, item2.ExpectedValues, true);

            t1.delete(long.MaxValue);
            t2.delete(long.MaxValue);
            t3.delete(long.MaxValue);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void deleteVersionedItemInConflictingTransaction()
        public virtual void deleteVersionedItemInConflictingTransaction()
        {
            ExampleVersionedHashKeyItem item1 = newVersionedItem();

            // establish the item with version 1
            Transaction t1 = manager.newTransaction();
            t1.save(item1);
            t1.commit();

            // start to delete the item
            Transaction t2 = manager.newTransaction();
            t2.delete(item1);

            // update the item to version 2
            Transaction t3 = manager.newTransaction();
            ExampleVersionedHashKeyItem item2 = t3.load(item1);
            t3.save(item2);
            t3.commit();

            // try to commit the delete
            try
            {
                t2.commit();
                fail();
            }
            catch (TransactionRolledBackException)
            {
            }

            assertItemNotLocked(INTEG_HASH_TABLE_NAME, item2.Key, item2.ExpectedValues, true);

            t1.delete(long.MaxValue);
            t2.delete(long.MaxValue);
            t3.delete(long.MaxValue);
        }

        private ExampleVersionedHashKeyItem newVersionedItem()
        {
            ExampleVersionedHashKeyItem item1 = new ExampleVersionedHashKeyItem();
            item1.Id = UUID.randomUUID().ToString();
            return item1;
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void readCommitted()
        public virtual void readCommitted()
        {
            ExampleHashKeyItem item1 = newItem();
            item1.Something = "example";

            Transaction t1 = manager.newTransaction();
            t1.save(item1);
            assertItemLocked(INTEG_HASH_TABLE_NAME, item1.Key, t1.Id, true, true);

            ExampleHashKeyItem loadedItem1 = manager.load(item1, Transaction.IsolationLevel.COMMITTED);
            assertNull(loadedItem1);

            t1.commit();
            assertItemNotLocked(INTEG_HASH_TABLE_NAME, item1.Key, true);

            ExampleHashKeyItem loadedItem2 = manager.load(item1, Transaction.IsolationLevel.COMMITTED);
            assertNotNull(loadedItem2);
            assertEquals(item1.ExpectedValues, loadedItem2.ExpectedValues);

            t1.delete(long.MaxValue);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void readUncommitted()
        public virtual void readUncommitted()
        {
            ExampleHashKeyItem item1 = newItem();
            item1.Something = "example";

            Transaction t1 = manager.newTransaction();
            t1.save(item1);
            assertItemLocked(INTEG_HASH_TABLE_NAME, item1.Key, t1.Id, true, true);

            ExampleHashKeyItem loadedItem1 = manager.load(item1, Transaction.IsolationLevel.UNCOMMITTED);
            assertNotNull(loadedItem1);
            assertEquals(item1.ExpectedValues, loadedItem1.ExpectedValues);

            t1.rollback();
            assertItemNotLocked(INTEG_HASH_TABLE_NAME, item1.Key, false);

            ExampleHashKeyItem loadedItem2 = manager.load(item1, Transaction.IsolationLevel.COMMITTED);
            assertNull(loadedItem2);

            t1.delete(long.MaxValue);
        }
    }
}