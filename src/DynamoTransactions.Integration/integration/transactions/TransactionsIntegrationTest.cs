using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using com.amazonaws.services.dynamodbv2.transactions.exceptions;

using DynamoTransactions;
using Xunit;
using static DynamoTransactions.Integration.AssertStatic;

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
namespace com.amazonaws.services.dynamodbv2.transactions
{

    public class TransactionsIntegrationTest : IntegrationTest
    {

        private const int MaxItemSizeBytes = 1024 * 400; // 400 KB

        internal static readonly Dictionary<string, AttributeValue> JsonMAttrVal = new Dictionary<string, AttributeValue>();

        static TransactionsIntegrationTest()
        {
            JsonMAttrVal["attr_s"] = (new AttributeValue()).WithS("s");
            JsonMAttrVal["attr_n"] = (new AttributeValue()).WithN("1");
            JsonMAttrVal["attr_b"] = (new AttributeValue()).WithB(new MemoryStream(Encoding.ASCII.GetBytes("asdf")));
            JsonMAttrVal["attr_ss"] = (new AttributeValue()).WithSs(new List<string> { "a", "b" });
            JsonMAttrVal["attr_ns"] = (new AttributeValue()).WithNs(new List<string> { "1", "2" });
            JsonMAttrVal["attr_bs"] = (new AttributeValue()).WithBs(new List<MemoryStream>
            {
                new MemoryStream(Encoding.ASCII.GetBytes("asdf")),
                new MemoryStream(Encoding.ASCII.GetBytes("ghjk"))
            });
            JsonMAttrVal["attr_bool"] = new AttributeValue
            {
                BOOL = true,

            };
            JsonMAttrVal["attr_l"] = (new AttributeValue())
                .WithL(new List<AttributeValue> {
                new AttributeValue().WithS("s"),
                new AttributeValue().WithN("1"),
                new AttributeValue().WithB(new MemoryStream(Encoding.ASCII.GetBytes("asdf"))),
                new AttributeValue
                {
                    BOOL = true,

                }, new AttributeValue
                {
                    NULL = true,

                }});
            JsonMAttrVal["attr_null"] = new AttributeValue
            {
                NULL = true,

            };
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public TransactionsIntegrationTest() throws java.io.IOException
        public TransactionsIntegrationTest() : base()
        {
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Before public void setup()
        public virtual void Setup()
        {
            Dynamodb.Reset();
            Transaction t = Manager.NewTransaction();
            Key0 = NewKey(IntegHashTableName);
            Item0 = new Dictionary<string, AttributeValue>(Key0);
            Item0.Add("s_someattr", new AttributeValue("val"));
            Item0.Add("ss_otherattr", (new AttributeValue()).WithSs(new List<string>
            {
                "one",
                "two"
            }));
            Dictionary<string, AttributeValue> putResponse = t.PutItemAsync(new PutItemRequest
            {
                TableName = IntegHashTableName,
                Item = Item0,
                ReturnValues = ReturnValue.ALL_OLD,

            }).Result.Attributes;
            AssertNull(putResponse);
            t.CommitAsync().Wait();
            AssertItemNotLocked(IntegHashTableName, Key0, Item0, true);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @After public void teardown()
        public virtual void Teardown()
        {
            Dynamodb.Reset();
            Transaction t = Manager.NewTransaction();
            t.DeleteItemAsync(new DeleteItemRequest
            {
                TableName = IntegHashTableName,
                Key = Key0,

            }).Wait();
            t.CommitAsync().Wait();
            AssertItemNotLocked(IntegHashTableName, Key0, false);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void phantomItemFromDelete()
        public virtual void PhantomItemFromDelete()
        {
            Dictionary<string, AttributeValue> key1 = NewKey(IntegHashTableName);
            Transaction transaction = Manager.NewTransaction();
            DeleteItemRequest deleteRequest = new DeleteItemRequest
            {
                TableName = IntegHashTableName,
                Key = key1,

            };
            transaction.DeleteItemAsync(deleteRequest).Wait();
            AssertItemLocked(IntegHashTableName, key1, transaction.Id, true, false);
            transaction.RollbackAsync().Wait();
            AssertItemNotLocked(IntegHashTableName, key1, false);
            transaction.DeleteAsync(long.MaxValue).Wait();
        }

        /*
		 * GetItem tests
		 */

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void lockItem()
        public virtual void LockItem()
        {
            Dictionary<string, AttributeValue> key1 = NewKey(IntegHashTableName);
            Transaction t1 = Manager.NewTransaction();
            Transaction t2 = Manager.NewTransaction();

            DeleteItemRequest deleteRequest = new DeleteItemRequest
            {
                TableName = IntegHashTableName,
                Key = key1,

            };

            GetItemRequest lockRequest = new GetItemRequest
            {
                TableName = IntegHashTableName,
                Key = key1,

            };

            Dictionary<string, AttributeValue> getResponse = t1.GetItemAsync(lockRequest).Result.Item;

            AssertItemLocked(IntegHashTableName, key1, t1.Id, true, false); // we're not applying locks
            AssertNull(getResponse);

            Dictionary<string, AttributeValue> deleteResponse = t2.DeleteItemAsync(deleteRequest).Result.Attributes;
            AssertItemLocked(IntegHashTableName, key1, t2.Id, true, false); // we're not applying deletes either
            AssertNull(deleteResponse); // return values is null in the request

            t2.CommitAsync().Wait();

            try
            {
                t1.CommitAsync().Wait();
                Fail();
            }
            catch (TransactionRolledBackException)
            {
            }

            t1.DeleteAsync(long.MaxValue).Wait();
            t2.DeleteAsync(long.MaxValue).Wait();

            AssertItemNotLocked(IntegHashTableName, key1, false);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void lock2Items()
        public virtual void Lock2Items()
        {
            Dictionary<string, AttributeValue> key1 = NewKey(IntegHashTableName);
            Dictionary<string, AttributeValue> key2 = NewKey(IntegHashTableName);

            Transaction t0 = Manager.NewTransaction();
            Dictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue>(key1);
            item1["something"] = new AttributeValue("val");
            Dictionary<string, AttributeValue> putResponse = t0.PutItemAsync(new PutItemRequest
            {
                TableName = IntegHashTableName,
                Item = item1,
                ReturnValues = ReturnValue.ALL_OLD,

            }).Result.Attributes;
            AssertNull(putResponse);

            t0.CommitAsync().Wait();

            Transaction t1 = Manager.NewTransaction();

            Dictionary<string, AttributeValue> getResult1 = t1.GetItemAsync(new GetItemRequest
            {
                TableName = IntegHashTableName,
                Key = key1,

            }).Result.Item;
            AssertItemLocked(IntegHashTableName, key1, item1, t1.Id, false, false);
            AssertEquals(item1, getResult1);

            Dictionary<string, AttributeValue> getResult2 = t1.GetItemAsync(new GetItemRequest
            {
                TableName = IntegHashTableName,
                Key = key2,

            }).Result.Item;
            AssertItemLocked(IntegHashTableName, key1, item1, t1.Id, false, false);
            AssertItemLocked(IntegHashTableName, key2, t1.Id, true, false);
            AssertNull(getResult2);

            t1.CommitAsync().Wait();
            t1.DeleteAsync(long.MaxValue).Wait();

            AssertItemNotLocked(IntegHashTableName, key1, item1, true);
            AssertItemNotLocked(IntegHashTableName, key2, false);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void getItemWithDelete()
        public virtual void GetItemWithDelete()
        {
            Transaction t1 = Manager.NewTransaction();
            Dictionary<string, AttributeValue> getResult1 = t1.GetItemAsync(new GetItemRequest
            {
                TableName = IntegHashTableName,
                Key = Key0,

            }).Result.Item;
            AssertEquals(getResult1, Item0);
            AssertItemLocked(IntegHashTableName, Key0, Item0, t1.Id, false, false);

            t1.DeleteItemAsync(new DeleteItemRequest
            {
                TableName = IntegHashTableName,
                Key = Key0,

            }).Wait();
            AssertItemLocked(IntegHashTableName, Key0, Item0, t1.Id, false, false);

            Dictionary<string, AttributeValue> getResult2 = t1.GetItemAsync(new GetItemRequest
            {
                TableName = IntegHashTableName,
                Key = Key0,

            }).Result.Item;
            AssertNull(getResult2);
            AssertItemLocked(IntegHashTableName, Key0, Item0, t1.Id, false, false);

            t1.CommitAsync().Wait();
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void getFilterAttributesToGet()
        public virtual void GetFilterAttributesToGet()
        {
            Transaction t1 = Manager.NewTransaction();

            Dictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue>();
            item1["s_someattr"] = Item0[("s_someattr")];

            Dictionary<string, AttributeValue> getResult1 = t1.GetItemAsync(new GetItemRequest
            {
                TableName = IntegHashTableName,
                AttributesToGet = { "s_someattr", "notexists" },
                Key = Key0
            }).Result.Item;
            AssertEquals(item1, getResult1);
            AssertItemLocked(IntegHashTableName, Key0, t1.Id, false, false);

            t1.CommitAsync().Wait();

            AssertItemNotLocked(IntegHashTableName, Key0, Item0, true);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void getItemNotExists()
        public virtual void GetItemNotExists()
        {
            Transaction t1 = Manager.NewTransaction();
            Dictionary<string, AttributeValue> key1 = NewKey(IntegHashTableName);

            Dictionary<string, AttributeValue> getResult1 = t1.GetItemAsync(new GetItemRequest
            {
                TableName = IntegHashTableName,
                Key = key1,

            }).Result.Item;
            AssertNull(getResult1);
            AssertItemLocked(IntegHashTableName, key1, t1.Id, true, false);

            Dictionary<string, AttributeValue> getResult2 = t1.GetItemAsync(new GetItemRequest
            {
                TableName = IntegHashTableName,
                Key = key1,

            }).Result.Item;
            AssertNull(getResult2);
            AssertItemLocked(IntegHashTableName, key1, t1.Id, true, false);

            t1.CommitAsync().Wait();

            AssertItemNotLocked(IntegHashTableName, key1, false);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void getItemAfterPutItemInsert()
        public virtual void GetItemAfterPutItemInsert()
        {
            Transaction t1 = Manager.NewTransaction();
            Dictionary<string, AttributeValue> key1 = NewKey(IntegHashTableName);
            Dictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue>(key1);
            item1["asdf"] = new AttributeValue("wef");

            Dictionary<string, AttributeValue> getResult1 = t1.GetItemAsync(new GetItemRequest
            {
                TableName = IntegHashTableName,
                Key = key1,

            }).Result.Item;
            AssertNull(getResult1);
            AssertItemLocked(IntegHashTableName, key1, t1.Id, true, false);

            Dictionary<string, AttributeValue> putResult1 = t1.PutItemAsync(new PutItemRequest
            {
                TableName = IntegHashTableName,
                Item = item1,
                ReturnValues = ReturnValue.ALL_OLD,

            }).Result.Attributes;
            AssertItemLocked(IntegHashTableName, key1, item1, t1.Id, true, true);
            AssertNull(putResult1);

            Dictionary<string, AttributeValue> getResult2 = t1.GetItemAsync(new GetItemRequest
            {
                TableName = IntegHashTableName,
                Key = key1,

            }).Result.Item;
            AssertEquals(getResult2, item1);
            AssertItemLocked(IntegHashTableName, key1, item1, t1.Id, true, true);

            t1.CommitAsync().Wait();

            AssertItemNotLocked(IntegHashTableName, key1, item1, true);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void getItemAfterPutItemOverwrite()
        public virtual void GetItemAfterPutItemOverwrite()
        {
            Transaction t1 = Manager.NewTransaction();
            Dictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue>(Item0);
            item1["asdf"] = new AttributeValue("wef");

            Dictionary<string, AttributeValue> getResult1 = t1.GetItemAsync(new GetItemRequest
            {
                TableName = IntegHashTableName,
                Key = Key0,

            }).Result.Item;
            AssertEquals(getResult1, Item0);
            AssertItemLocked(IntegHashTableName, Key0, Item0, t1.Id, false, false);

            Dictionary<string, AttributeValue> putResult1 = t1.PutItemAsync(new PutItemRequest
            {
                TableName = IntegHashTableName,
                Item = item1,
                ReturnValues = ReturnValue.ALL_OLD,

            }).Result.Attributes;
            AssertItemLocked(IntegHashTableName, Key0, item1, t1.Id, false, true);
            AssertEquals(putResult1, Item0);

            Dictionary<string, AttributeValue> getResult2 = t1.GetItemAsync(new GetItemRequest
            {
                TableName = IntegHashTableName,
                Key = Key0,

            }).Result.Item;
            AssertEquals(getResult2, item1);
            AssertItemLocked(IntegHashTableName, Key0, item1, t1.Id, false, true);

            t1.CommitAsync().Wait();

            AssertItemNotLocked(IntegHashTableName, Key0, item1, true);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void getItemAfterPutItemInsertInResumedTx()
        public virtual void GetItemAfterPutItemInsertInResumedTx()
        {
            Transaction t1 = new TransactionAnonymousInnerClass(this, Guid.NewGuid().ToString(), Manager);

            Transaction t2 = Manager.ResumeTransaction(t1.Id);

            Dictionary<string, AttributeValue> key1 = NewKey(IntegHashTableName);
            Dictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue>(key1);
            item1["asdf"] = new AttributeValue("wef");

            try
            {
                // This Put needs to fail in apply
                t1.PutItemAsync(new PutItemRequest
                {
                    TableName = IntegHashTableName,
                    Item = item1,
                    ReturnValues = ReturnValue.ALL_OLD,

                }).Wait();
                Fail();
            }
            catch (FailingAmazonDynamoDbClient.FailedYourRequestException)
            {
            }
            AssertItemLocked(IntegHashTableName, key1, t1.Id, true, false);

            // second copy of same tx
            Dictionary<string, AttributeValue> getResult1 = t2.GetItemAsync(new GetItemRequest
            {
                TableName = IntegHashTableName,
                Key = key1,

            }).Result.Item;
            AssertEquals(getResult1, item1);
            AssertItemLocked(IntegHashTableName, key1, item1, t1.Id, true, true);

            t2.CommitAsync().Wait();

            AssertItemNotLocked(IntegHashTableName, key1, item1, true);
        }

        private class TransactionAnonymousInnerClass : Transaction
        {
            private readonly TransactionsIntegrationTest _outerInstance;

            public TransactionAnonymousInnerClass(TransactionsIntegrationTest outerInstance, string toString, TransactionManager manager) : base(toString, manager, true)
            {
                this._outerInstance = outerInstance;
            }

            protected internal override Task<Dictionary<string, AttributeValue>> ApplyAndKeepLockAsync(Request request, Dictionary<string, AttributeValue> lockedItem)
            {
                throw new FailingAmazonDynamoDbClient.FailedYourRequestException();
            }
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void getItemThenPutItemInResumedTxThenGetItem()
        public virtual void GetItemThenPutItemInResumedTxThenGetItem()
        {
            Transaction t1 = new TransactionAnonymousInnerClass2(this, Guid.NewGuid().ToString(), Manager);

            Transaction t2 = Manager.ResumeTransaction(t1.Id);

            Dictionary<string, AttributeValue> key1 = NewKey(IntegHashTableName);
            Dictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue>(key1);
            item1["asdf"] = new AttributeValue("wef");

            // Get a read lock in t2
            Dictionary<string, AttributeValue> getResult1 = t2.GetItemAsync(new GetItemRequest
            {
                TableName = IntegHashTableName,
                Key = key1,

            }).Result.Item;
            AssertNull(getResult1);
            AssertItemLocked(IntegHashTableName, key1, null, t1.Id, true, false);

            // Begin a PutItem in t1, but fail apply
            try
            {
                t1.PutItemAsync(new PutItemRequest
                {
                    TableName = IntegHashTableName,
                    Item = item1,
                    ReturnValues = ReturnValue.ALL_OLD,

                }).Wait();
                Fail();
            }
            catch (FailingAmazonDynamoDbClient.FailedYourRequestException)
            {
            }
            AssertItemLocked(IntegHashTableName, key1, t1.Id, true, false);

            // Read again in the non-failing copy of the transaction
            Dictionary<string, AttributeValue> getResult2 = t2.GetItemAsync(new GetItemRequest
            {
                TableName = IntegHashTableName,
                Key = key1,

            }).Result.Item;
            AssertItemLocked(IntegHashTableName, key1, item1, t1.Id, true, true);
            t2.CommitAsync().Wait();
            AssertEquals(item1, getResult2);

            AssertItemNotLocked(IntegHashTableName, key1, item1, true);
        }

        private class TransactionAnonymousInnerClass2 : Transaction
        {
            private readonly TransactionsIntegrationTest _outerInstance;

            public TransactionAnonymousInnerClass2(TransactionsIntegrationTest outerInstance, string toString, TransactionManager manager) : base(toString, manager, true)
            {
                this._outerInstance = outerInstance;
            }

            protected internal override async Task<Dictionary<string, AttributeValue>> ApplyAndKeepLockAsync(Request request, Dictionary<string, AttributeValue> lockedItem)
            {
                if (request is Request.GetItem || request is Request.DeleteItem)
                {
                    return await base.ApplyAndKeepLockAsync(request, lockedItem);
                }
                throw new FailingAmazonDynamoDbClient.FailedYourRequestException();
            }
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void getThenUpdateNewItem()
        public virtual void GetThenUpdateNewItem()
        {
            Transaction t1 = Manager.NewTransaction();
            Dictionary<string, AttributeValue> key1 = NewKey(IntegHashTableName);

            Dictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue>(key1);
            item1["asdf"] = new AttributeValue("didn't exist");

            Dictionary<string, AttributeValueUpdate> updates1 = new Dictionary<string, AttributeValueUpdate>();
            updates1["asdf"] = new AttributeValueUpdate(new AttributeValue("didn't exist"), AttributeAction.PUT);

            Dictionary<string, AttributeValue> getResponse = t1.GetItemAsync(new GetItemRequest
            {
                TableName = IntegHashTableName,
                Key = key1,

            }).Result.Item;
            AssertItemLocked(IntegHashTableName, key1, t1.Id, true, false);
            AssertNull(getResponse);

            Dictionary<string, AttributeValue> updateResponse = t1.UpdateItemAsync(new UpdateItemRequest
            {
                TableName = IntegHashTableName,
                Key = key1,
                AttributeUpdates = updates1,
                ReturnValues = ReturnValue.ALL_NEW,

            }).Result.Attributes;
            AssertItemLocked(IntegHashTableName, key1, item1, t1.Id, true, true);
            AssertEquals(item1, updateResponse);

            t1.CommitAsync().Wait();

            AssertItemNotLocked(IntegHashTableName, key1, item1, true);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void getThenUpdateExistingItem()
        public virtual void GetThenUpdateExistingItem()
        {
            Transaction t1 = Manager.NewTransaction();

            Dictionary<string, AttributeValue> item0A = new Dictionary<string, AttributeValue>(Item0);
            item0A["wef"] = new AttributeValue("new attr");

            Dictionary<string, AttributeValueUpdate> updates1 = new Dictionary<string, AttributeValueUpdate>();
            updates1["wef"] = new AttributeValueUpdate(new AttributeValue("new attr"), AttributeAction.PUT);

            Dictionary<string, AttributeValue> getResponse = t1.GetItemAsync(new GetItemRequest
            {
                TableName = IntegHashTableName,
                Key = Key0,

            }).Result.Item;
            AssertItemLocked(IntegHashTableName, Key0, Item0, t1.Id, false, false);
            AssertEquals(Item0, getResponse);

            Dictionary<string, AttributeValue> updateResponse = t1.UpdateItemAsync(new UpdateItemRequest
            {
                TableName = IntegHashTableName,
                Key = Key0,
                AttributeUpdates = updates1,
                ReturnValues = ReturnValue.ALL_NEW,

            }).Result.Attributes;
            AssertItemLocked(IntegHashTableName, Key0, item0A, t1.Id, false, true);
            AssertEquals(item0A, updateResponse);

            t1.CommitAsync().Wait();

            AssertItemNotLocked(IntegHashTableName, Key0, item0A, true);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void getItemUncommittedInsert()
        public virtual void GetItemUncommittedInsert()
        {
            Transaction t1 = Manager.NewTransaction();

            Dictionary<string, AttributeValue> key1 = NewKey(IntegHashTableName);
            Dictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue>(key1);
            item1["asdf"] = new AttributeValue("wef");

            t1.PutItemAsync(new PutItemRequest
            {
                TableName = IntegHashTableName,
                Item = item1,

            });

            AssertItemLocked(IntegHashTableName, key1, item1, t1.Id, true, true);

            Dictionary<string, AttributeValue> item = Manager.GetItemAsync(new GetItemRequest
            {
                TableName = IntegHashTableName,
                Key = key1,

            }, Transaction.IsolationLevel.Uncommitted).Result.Item;
            AssertNoSpecialAttributes(item);
            AssertEquals(item1, item);
            AssertItemLocked(IntegHashTableName, key1, item1, t1.Id, true, true);

            t1.RollbackAsync().Wait();
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void getItemUncommittedDeleted()
        public virtual void GetItemUncommittedDeleted()
        {
            Transaction t1 = Manager.NewTransaction();

            t1.DeleteItemAsync(new DeleteItemRequest
            {
                TableName = IntegHashTableName,
                Key = Key0,

            }).Wait();

            AssertItemLocked(IntegHashTableName, Key0, Item0, t1.Id, false, false);

            Dictionary<string, AttributeValue> item = Manager.GetItemAsync(new GetItemRequest
            {
                TableName = IntegHashTableName,
                Key = Key0,

            }, Transaction.IsolationLevel.Uncommitted).Result.Item;
            AssertNoSpecialAttributes(item);
            AssertEquals(Item0, item);
            AssertItemLocked(IntegHashTableName, Key0, Item0, t1.Id, false, false);

            t1.RollbackAsync().Wait();
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void getItemCommittedInsert()
        public virtual void GetItemCommittedInsert()
        {
            Transaction t1 = Manager.NewTransaction();

            Dictionary<string, AttributeValue> key1 = NewKey(IntegHashTableName);
            Dictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue>(key1);
            item1["asdf"] = new AttributeValue("wef");

            t1.PutItemAsync(new PutItemRequest
            {
                TableName = IntegHashTableName,
                Item = item1,

            }).Wait();

            AssertItemLocked(IntegHashTableName, key1, item1, t1.Id, true, true);

            Dictionary<string, AttributeValue> item = Manager.GetItemAsync(new GetItemRequest
            {
                TableName = IntegHashTableName,
                Key = key1,

            }, Transaction.IsolationLevel.Committed).Result.Item;
            AssertNull(item);
            AssertItemLocked(IntegHashTableName, key1, item1, t1.Id, true, true);

            t1.RollbackAsync().Wait();
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void getItemCommittedDeleted()
        public virtual void GetItemCommittedDeleted()
        {
            Transaction t1 = Manager.NewTransaction();

            t1.DeleteItemAsync(new DeleteItemRequest
            {
                TableName = IntegHashTableName,
                Key = Key0,

            }).Wait();

            AssertItemLocked(IntegHashTableName, Key0, Item0, t1.Id, false, false);

            Dictionary<string, AttributeValue> item = Manager.GetItemAsync(new GetItemRequest
            {
                TableName = IntegHashTableName,
                Key = Key0,

            }, Transaction.IsolationLevel.Committed).Result.Item;
            AssertNoSpecialAttributes(item);
            AssertEquals(Item0, item);
            AssertItemLocked(IntegHashTableName, Key0, Item0, t1.Id, false, false);

            t1.RollbackAsync().Wait();
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void getItemCommittedUpdated()
        public virtual void GetItemCommittedUpdated()
        {
            Transaction t1 = Manager.NewTransaction();

            Dictionary<string, AttributeValueUpdate> updates = new Dictionary<string, AttributeValueUpdate>();
            updates["asdf"] = new AttributeValueUpdate
            {
                Action = AttributeAction.PUT,
                Value = new AttributeValue("asdf")
            };
            Dictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue>(Item0);
            item1["asdf"] = new AttributeValue("asdf");

            t1.UpdateItemAsync(new UpdateItemRequest
            {
                TableName = IntegHashTableName,
                AttributeUpdates = updates,
                Key = Key0,

            }).Wait();

            AssertItemLocked(IntegHashTableName, Key0, item1, t1.Id, false, true);

            Dictionary<string, AttributeValue> item = Manager.GetItemAsync(new GetItemRequest
            {
                TableName = IntegHashTableName,
                Key = Key0,

            }, Transaction.IsolationLevel.Committed).Result.Item;
            AssertNoSpecialAttributes(item);
            AssertEquals(Item0, item);
            AssertItemLocked(IntegHashTableName, Key0, item1, t1.Id, false, true);

            t1.CommitAsync().Wait();

            item = Manager.GetItemAsync(new GetItemRequest
            {
                TableName = IntegHashTableName,
                Key = Key0,

            }, Transaction.IsolationLevel.Committed).Result.Item;
            AssertNoSpecialAttributes(item);
            AssertEquals(item1, item);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void getItemCommittedUpdatedAndApplied()
        public virtual void GetItemCommittedUpdatedAndApplied()
        {
            Transaction t1 = new TransactionAnonymousInnerClass3(this, Guid.NewGuid().ToString(), Manager);

            Dictionary<string, AttributeValueUpdate> updates = new Dictionary<string, AttributeValueUpdate>();
            updates["asdf"] = new AttributeValueUpdate
            {
                Action = AttributeAction.PUT,
                Value = new AttributeValue("asdf")
            };
            Dictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue>(Item0);
            item1["asdf"] = new AttributeValue("asdf");

            t1.UpdateItemAsync(new UpdateItemRequest
            {
                TableName = IntegHashTableName,
                AttributeUpdates = updates,
                Key = Key0,

            }).Wait();

            AssertItemLocked(IntegHashTableName, Key0, item1, t1.Id, false, true);

            t1.CommitAsync().Wait();

            Dictionary<string, AttributeValue> item = Manager.GetItemAsync(new GetItemRequest
            {
                TableName = IntegHashTableName,
                Key = Key0,

            }, Transaction.IsolationLevel.Committed).Result.Item;
            AssertNoSpecialAttributes(item);
            AssertEquals(item1, item);
            AssertItemLocked(IntegHashTableName, Key0, item1, t1.Id, false, true);
        }

        private class TransactionAnonymousInnerClass3 : Transaction
        {
            private readonly TransactionsIntegrationTest _outerInstance;

            public TransactionAnonymousInnerClass3(TransactionsIntegrationTest outerInstance, string toString, TransactionManager manager) : base(toString, manager, true)
            {
                this._outerInstance = outerInstance;
            }

            protected internal override async Task DoCommitAsync()
            {
                //Skip cleaning up the transaction so we can validate reading.
            }
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void getItemCommittedMissingImage()
        public virtual void GetItemCommittedMissingImage()
        {
            Transaction t1 = Manager.NewTransaction();
            Dictionary<string, AttributeValueUpdate> updates = new Dictionary<string, AttributeValueUpdate>();
            updates["asdf"] = new AttributeValueUpdate
            {
                Action = AttributeAction.PUT,

                Value = new AttributeValue("asdf")
            };
            Dictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue>(Item0);
            item1["asdf"] = new AttributeValue("asdf");

            t1.UpdateItemAsync(new UpdateItemRequest
            {
                TableName = IntegHashTableName,
                AttributeUpdates = updates,
                Key = Key0,

            }).Wait();

            AssertItemLocked(IntegHashTableName, Key0, item1, t1.Id, false, true);

            GetItemRequest deletedGetRequests = new GetItemRequest
            {
                TableName = Manager.ItemImageTableName,
                ConsistentRead = true
            };
            deletedGetRequests.Key.Add(Transaction.AttributeName.ImageId.ToString(),
                new AttributeValue(t1.TxItem.TxId + "#" + 1));

            Dynamodb.GetRequestsToTreatAsDeleted.Add(deletedGetRequests);

            try
            {
                Manager.GetItemAsync(new GetItemRequest
                {
                    TableName = IntegHashTableName,
                    Key = Key0,
                }, Transaction.IsolationLevel.Committed).Wait();
                Fail("Should have thrown an exception.");
            }
            catch (TransactionException e)
            {
                AssertEquals("null - Ran out of attempts to get a committed image of the item", e.Message);
            }
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void getItemCommittedConcurrentCommit()
        public virtual void GetItemCommittedConcurrentCommit()
        {
            //Test reading an item while simulating another transaction committing concurrently.
            //To do this we skip cleanup, make the item image appear to be deleted,
            //and then make the reader get the uncommitted version of the transaction 
            //row for the first read and then actual updated version for later reads.

            Transaction t1 = new TransactionAnonymousInnerClass4(this, Guid.NewGuid().ToString(), Manager);
            Dictionary<string, AttributeValueUpdate> updates = new Dictionary<string, AttributeValueUpdate>();
            updates["asdf"] = new AttributeValueUpdate
            {
                Action = AttributeAction.PUT,

                Value = new AttributeValue("asdf")
            };
            Dictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue>(Item0);
            item1["asdf"] = new AttributeValue("asdf");

            t1.UpdateItemAsync(new UpdateItemRequest
            {
                TableName = IntegHashTableName,
                AttributeUpdates = updates,
                Key = Key0,

            }).Wait();

            AssertItemLocked(IntegHashTableName, Key0, item1, t1.Id, false, true);

            GetItemRequest txItemRequest = new GetItemRequest
            {
                TableName = Manager.TransactionTableName,
                ConsistentRead = true
            };
            txItemRequest.Key.Add(Transaction.AttributeName.Txid.ToString(), new AttributeValue(t1.TxItem.TxId));

            //Save the copy of the transaction before commit. 
            GetItemResponse uncommittedTransaction = Dynamodb.GetItemAsync(txItemRequest).Result;

            t1.CommitAsync().Wait();
            AssertItemLocked(IntegHashTableName, Key0, item1, t1.Id, false, true);

            Dynamodb.GetRequestsToStub.Add(txItemRequest, new LinkedList<GetItemResponse>(Collections.SingletonList(uncommittedTransaction)));
            //Stub out the image so it appears deleted
            var getItemRequest = new GetItemRequest
            {
                TableName = Manager.ItemImageTableName,
                ConsistentRead = true
            };
            getItemRequest.Key.Add(Transaction.AttributeName.ImageId.ToString(), new AttributeValue(t1.TxItem.TxId + "#" + 1));

            Dynamodb.GetRequestsToTreatAsDeleted.Add(getItemRequest);

            Dictionary<string, AttributeValue> item = Manager.GetItemAsync(new GetItemRequest
            {
                TableName = IntegHashTableName,
                Key = Key0,

            }, Transaction.IsolationLevel.Committed).Result.Item;
            AssertNoSpecialAttributes(item);
            AssertEquals(item1, item);
        }

        private class TransactionAnonymousInnerClass4 : Transaction
        {
            private readonly TransactionsIntegrationTest _outerInstance;

            public TransactionAnonymousInnerClass4(TransactionsIntegrationTest outerInstance, string toString, TransactionManager manager) : base(toString, manager, true)
            {
                this._outerInstance = outerInstance;
            }

            protected internal override async Task DoCommitAsync()
            {
                //Skip cleaning up the transaction so we can validate reading.
            }
        }

        /*
		 * ReturnValues tests
		 */

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void putItemAllOldInsert()
        public virtual void PutItemAllOldInsert()
        {
            Transaction t1 = Manager.NewTransaction();
            Dictionary<string, AttributeValue> key1 = NewKey(IntegHashTableName);
            Dictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue>(key1);
            item1["asdf"] = new AttributeValue("wef");

            Dictionary<string, AttributeValue> putResult1 = t1.PutItemAsync(new PutItemRequest
            {
                TableName = IntegHashTableName,
                Item = item1,
                ReturnValues = ReturnValue.ALL_OLD,

            }).Result.Attributes;
            AssertItemLocked(IntegHashTableName, key1, item1, t1.Id, true, true);
            AssertNull(putResult1);

            t1.CommitAsync().Wait();

            AssertItemNotLocked(IntegHashTableName, key1, item1, true);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void putItemAllOldOverwrite()
        public virtual void PutItemAllOldOverwrite()
        {
            Transaction t1 = Manager.NewTransaction();
            Dictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue>(Item0);
            item1["asdf"] = new AttributeValue("wef");

            Dictionary<string, AttributeValue> putResult1 = t1.PutItemAsync(new PutItemRequest
            {
                TableName = IntegHashTableName,
                Item = item1,
                ReturnValues = ReturnValue.ALL_OLD,

            }).Result.Attributes;
            AssertItemLocked(IntegHashTableName, Key0, item1, t1.Id, false, true);
            AssertEquals(putResult1, Item0);

            t1.CommitAsync().Wait();

            AssertItemNotLocked(IntegHashTableName, Key0, item1, true);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void updateItemAllOldInsert()
        public virtual void UpdateItemAllOldInsert()
        {
            Transaction t1 = Manager.NewTransaction();
            Dictionary<string, AttributeValue> key1 = NewKey(IntegHashTableName);
            Dictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue>(key1);
            item1["asdf"] = new AttributeValue("wef");
            Dictionary<string, AttributeValueUpdate> updates = new Dictionary<string, AttributeValueUpdate>();
            updates["asdf"] = new AttributeValueUpdate
            {
                Action = AttributeAction.PUT,
                Value = new AttributeValue("wef")
            };

            Dictionary<string, AttributeValue> result1 = t1.UpdateItemAsync(new UpdateItemRequest
            {
                TableName = IntegHashTableName,
                Key = key1,
                AttributeUpdates = updates,
                ReturnValues = ReturnValue.ALL_OLD,

            }).Result.Attributes;
            AssertItemLocked(IntegHashTableName, key1, item1, t1.Id, true, true);
            AssertNull(result1);

            t1.CommitAsync().Wait();

            AssertItemNotLocked(IntegHashTableName, key1, item1, true);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void updateItemAllOldOverwrite()
        public virtual void UpdateItemAllOldOverwrite()
        {
            Transaction t1 = Manager.NewTransaction();
            Dictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue>(Item0);
            item1["asdf"] = new AttributeValue("wef");
            Dictionary<string, AttributeValueUpdate> updates = new Dictionary<string, AttributeValueUpdate>();
            updates["asdf"] = new AttributeValueUpdate
            {
                Action = AttributeAction.PUT,
                Value = new AttributeValue("wef")
            };

            Dictionary<string, AttributeValue> result1 = t1.UpdateItemAsync(new UpdateItemRequest
            {
                TableName = IntegHashTableName,
                Key = Key0,
                AttributeUpdates = updates,
                ReturnValues = ReturnValue.ALL_OLD,

            }).Result.Attributes;
            AssertItemLocked(IntegHashTableName, Key0, item1, t1.Id, false, true);
            AssertEquals(result1, Item0);

            t1.CommitAsync().Wait();

            AssertItemNotLocked(IntegHashTableName, Key0, item1, true);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void updateItemAllNewInsert()
        public virtual void UpdateItemAllNewInsert()
        {
            Transaction t1 = Manager.NewTransaction();
            Dictionary<string, AttributeValue> key1 = NewKey(IntegHashTableName);
            Dictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue>(key1);
            item1["asdf"] = new AttributeValue("wef");
            Dictionary<string, AttributeValueUpdate> updates = new Dictionary<string, AttributeValueUpdate>();
            updates["asdf"] = new AttributeValueUpdate
            {
                Action = AttributeAction.PUT,
                Value = new AttributeValue("wef")
            };

            Dictionary<string, AttributeValue> result1 = t1.UpdateItemAsync(new UpdateItemRequest
            {
                TableName = IntegHashTableName,
                Key = key1,
                AttributeUpdates = updates,
                ReturnValues = ReturnValue.ALL_NEW,

            }).Result.Attributes;
            AssertItemLocked(IntegHashTableName, key1, item1, t1.Id, true, true);
            AssertEquals(result1, item1);

            t1.CommitAsync().Wait();

            AssertItemNotLocked(IntegHashTableName, key1, item1, true);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void updateItemAllNewOverwrite()
        public virtual void UpdateItemAllNewOverwrite()
        {
            Transaction t1 = Manager.NewTransaction();
            Dictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue>(Item0);
            item1["asdf"] = new AttributeValue("wef");
            Dictionary<string, AttributeValueUpdate> updates = new Dictionary<string, AttributeValueUpdate>();
            updates["asdf"] = new AttributeValueUpdate
            {
                Action = AttributeAction.PUT,
                Value = new AttributeValue("wef")
            };

            Dictionary<string, AttributeValue> result1 = t1.UpdateItemAsync(new UpdateItemRequest
            {
                TableName = IntegHashTableName,
                Key = Key0,
                AttributeUpdates = updates,
                ReturnValues = ReturnValue.ALL_NEW,

            }).Result.Attributes;
            AssertItemLocked(IntegHashTableName, Key0, item1, t1.Id, false, true);
            AssertEquals(result1, item1);

            t1.CommitAsync().Wait();

            AssertItemNotLocked(IntegHashTableName, Key0, item1, true);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void deleteItemAllOldNotExists()
        public virtual void DeleteItemAllOldNotExists()
        {
            Transaction t1 = Manager.NewTransaction();
            Dictionary<string, AttributeValue> key1 = NewKey(IntegHashTableName);

            Dictionary<string, AttributeValue> result1 = t1.DeleteItemAsync(new DeleteItemRequest
            {
                TableName = IntegHashTableName,
                Key = key1,
                ReturnValues = ReturnValue.ALL_OLD,

            }).Result.Attributes;
            AssertItemLocked(IntegHashTableName, key1, key1, t1.Id, true, false);
            AssertNull(result1);

            t1.CommitAsync().Wait();

            AssertItemNotLocked(IntegHashTableName, key1, false);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void deleteItemAllOldExists()
        public virtual void DeleteItemAllOldExists()
        {
            Transaction t1 = Manager.NewTransaction();

            Dictionary<string, AttributeValue> result1 = t1.DeleteItemAsync(new DeleteItemRequest
            {
                TableName = IntegHashTableName,
                Key = Key0,
                ReturnValues = ReturnValue.ALL_OLD,

            }).Result.Attributes;
            AssertItemLocked(IntegHashTableName, Key0, Item0, t1.Id, false, false);
            AssertEquals(Item0, result1);

            t1.CommitAsync().Wait();

            AssertItemNotLocked(IntegHashTableName, Key0, false);
        }

        /*
		 * Transaction isolation and error tests
		 */

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void conflictingWrites()
        public virtual void ConflictingWrites()
        {
            Dictionary<string, AttributeValue> key1 = NewKey(IntegHashTableName);
            Transaction t1 = Manager.NewTransaction();
            Transaction t2 = Manager.NewTransaction();
            Transaction t3 = Manager.NewTransaction();

            // Finish t1 
            Dictionary<string, AttributeValue> t1Item = new Dictionary<string, AttributeValue>(key1);
            t1Item["whoami"] = new AttributeValue("t1");

            t1.PutItemAsync(new PutItemRequest
            {
                TableName = IntegHashTableName,
                Item = new Dictionary<string, AttributeValue>(t1Item)
            }).Wait();
            AssertItemLocked(IntegHashTableName, key1, t1Item, t1.Id, true, true);

            t1.CommitAsync().Wait();
            AssertItemNotLocked(IntegHashTableName, key1, t1Item, true);

            // Begin t2
            Dictionary<string, AttributeValue> t2Item = new Dictionary<string, AttributeValue>(key1);
            t2Item["whoami"] = new AttributeValue("t2");
            t2Item["t2stuff"] = new AttributeValue("extra");

            t2.PutItemAsync(new PutItemRequest
            {
                TableName = IntegHashTableName,
                Item = new Dictionary<string, AttributeValue>(t2Item)
            }).Wait();
            AssertItemLocked(IntegHashTableName, key1, t2Item, t2.Id, false, true);

            // Begin and finish t3
            Dictionary<string, AttributeValue> t3Item = new Dictionary<string, AttributeValue>(key1);
            t3Item["whoami"] = new AttributeValue("t3");
            t3Item["t3stuff"] = new AttributeValue("things");

            t3.PutItemAsync(new PutItemRequest
            {
                TableName = IntegHashTableName,
                Item = new Dictionary<string, AttributeValue>(t3Item)
            }).Wait();
            AssertItemLocked(IntegHashTableName, key1, t3Item, t3.Id, false, true);

            t3.CommitAsync().Wait();

            AssertItemNotLocked(IntegHashTableName, key1, t3Item, true);

            // Ensure t2 rolled back
            try
            {
                t2.CommitAsync().Wait();
                Fail();
            }
            catch (TransactionRolledBackException)
            {
            }

            t1.DeleteAsync(long.MaxValue).Wait();
            t2.DeleteAsync(long.MaxValue).Wait();
            t3.DeleteAsync(long.MaxValue).Wait();

            AssertItemNotLocked(IntegHashTableName, key1, t3Item, true);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void failValidationInApply()
        public virtual void FailValidationInApply()
        {
            Dictionary<string, AttributeValue> key = NewKey(IntegHashTableName);
            Dictionary<string, AttributeValueUpdate> updates = new Dictionary<string, AttributeValueUpdate>();
            updates["FooAttribute"] = new AttributeValueUpdate
            {
                Action = AttributeAction.PUT,
                Value = new AttributeValue("Bar")
            };

            Transaction t1 = Manager.NewTransaction();
            Transaction t2 = Manager.NewTransaction();

            t1.UpdateItemAsync(new UpdateItemRequest
            {
                TableName = IntegHashTableName,
                Key = key,
                AttributeUpdates = updates,

            }).Wait();

            AssertItemLocked(IntegHashTableName, key, t1.Id, true, true);

            t1.CommitAsync().Wait();

            AssertItemNotLocked(IntegHashTableName, key, true);

            updates["FooAttribute"] = new AttributeValueUpdate
            {
                Action = AttributeAction.ADD,
                Value = new AttributeValue { N = "1" }
            };

            try
            {
                t2.UpdateItemAsync(new UpdateItemRequest
                {
                    TableName = IntegHashTableName,
                    Key = key,
                    AttributeUpdates = updates,

                });
                Fail();
            }
            catch (AmazonServiceException e)
            {
                AssertEquals("ValidationException", e.ErrorCode);
                AssertTrue(e.Message.Contains("Type mismatch for attribute"));
            }

            AssertItemLocked(IntegHashTableName, key, t2.Id, false, false);

            try
            {
                t2.CommitAsync().Wait();
                Fail();
            }
            catch (AmazonServiceException e)
            {
                AssertEquals("ValidationException", e.ErrorCode);
                AssertTrue(e.Message.Contains("Type mismatch for attribute"));
            }

            AssertItemLocked(IntegHashTableName, key, t2.Id, false, false);

            t2.RollbackAsync().Wait();

            AssertItemNotLocked(IntegHashTableName, key, true);

            t1.DeleteAsync(long.MaxValue).Wait();
            t2.DeleteAsync(long.MaxValue).Wait();
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void useCommittedTransaction()
        public virtual void UseCommittedTransaction()
        {
            Dictionary<string, AttributeValue> key1 = NewKey(IntegHashTableName);
            Transaction t1 = Manager.NewTransaction();
            t1.CommitAsync().Wait();

            DeleteItemRequest deleteRequest = new DeleteItemRequest
            {
                TableName = IntegHashTableName,
                Key = key1,

            };

            try
            {
                t1.DeleteItemAsync(deleteRequest).Wait();
                Fail();
            }
            catch (TransactionCommittedException)
            {
            }

            AssertItemNotLocked(IntegHashTableName, key1, false);

            Transaction t2 = Manager.ResumeTransaction(t1.Id);

            try
            {
                t1.DeleteItemAsync(deleteRequest).Wait();
                Fail();
            }
            catch (TransactionCommittedException)
            {
            }

            AssertItemNotLocked(IntegHashTableName, key1, false);

            try
            {
                t2.RollbackAsync().Wait();
                Fail();
            }
            catch (TransactionCommittedException)
            {
            }

            t2.DeleteAsync(long.MaxValue).Wait();
            t1.DeleteAsync(long.MaxValue).Wait();
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void useRolledBackTransaction()
        public virtual void UseRolledBackTransaction()
        {
            Dictionary<string, AttributeValue> key1 = NewKey(IntegHashTableName);
            Transaction t1 = Manager.NewTransaction();
            t1.RollbackAsync().Wait();

            DeleteItemRequest deleteRequest = new DeleteItemRequest
            {
                TableName = IntegHashTableName,
                Key = key1,

            };

            try
            {
                t1.DeleteItemAsync(deleteRequest).Wait();
                Fail();
            }
            catch (TransactionRolledBackException)
            {
            }

            AssertItemNotLocked(IntegHashTableName, key1, false);

            Transaction t2 = Manager.ResumeTransaction(t1.Id);

            try
            {
                t1.DeleteItemAsync(deleteRequest).Wait();
                Fail();
            }
            catch (TransactionRolledBackException)
            {
            }

            AssertItemNotLocked(IntegHashTableName, key1, false);

            try
            {
                t2.CommitAsync().Wait();
                Fail();
            }
            catch (TransactionRolledBackException)
            {
            }

            AssertItemNotLocked(IntegHashTableName, key1, false);

            Transaction t3 = Manager.ResumeTransaction(t1.Id);
            t3.RollbackAsync().Wait();

            Transaction t4 = Manager.ResumeTransaction(t1.Id);

            t2.DeleteAsync(long.MaxValue).Wait();
            t1.DeleteAsync(long.MaxValue).Wait();

            try
            {
                t4.DeleteItemAsync(deleteRequest).Wait();
                Fail();
            }
            catch (TransactionNotFoundException)
            {
            }

            AssertItemNotLocked(IntegHashTableName, key1, false);

            t3.DeleteAsync(long.MaxValue).Wait();
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void useDeletedTransaction()
        public virtual void UseDeletedTransaction()
        {
            Transaction t1 = Manager.NewTransaction();
            Transaction t2 = Manager.ResumeTransaction(t1.Id);
            t1.CommitAsync().Wait();
            t1.DeleteAsync(long.MaxValue).Wait();

            try
            {
                t2.CommitAsync().Wait();
                Fail();
            }
            catch (UnknownCompletedTransactionException)
            {
            }

            t2.DeleteAsync(long.MaxValue).Wait();

        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void driveCommit()
        public virtual void DriveCommit()
        {
            Transaction t1 = Manager.NewTransaction();
            Dictionary<string, AttributeValue> key1 = NewKey(IntegHashTableName);
            Dictionary<string, AttributeValue> key2 = NewKey(IntegHashTableName);
            Dictionary<string, AttributeValue> item = new Dictionary<string, AttributeValue>(key1);
            item["attr"] = new AttributeValue("original");

            t1.PutItemAsync(new PutItemRequest
            {
                TableName = IntegHashTableName,
                Item = item,

            }).Wait();

            t1.CommitAsync().Wait();
            t1.DeleteAsync(long.MaxValue).Wait();

            AssertItemNotLocked(IntegHashTableName, key1, item, true);
            AssertItemNotLocked(IntegHashTableName, key2, false);

            Transaction t2 = Manager.NewTransaction();

            item["attr2"] = new AttributeValue("new");
            t2.PutItemAsync(new PutItemRequest
            {
                TableName = IntegHashTableName,
                Item = item,

            }).Wait();

            t2.GetItemAsync(new GetItemRequest
            {
                TableName = IntegHashTableName,
                Key = key2,

            }).Wait();

            AssertItemLocked(IntegHashTableName, key1, item, t2.Id, false, true);
            AssertItemLocked(IntegHashTableName, key2, key2, t2.Id, true, false);

            Transaction commitFailingTransaction = new TransactionAnonymousInnerClass1(this, t2.Id, Manager);

            try
            {
                commitFailingTransaction.CommitAsync().Wait();
                Fail();
            }
            catch (FailingAmazonDynamoDbClient.FailedYourRequestException)
            {
            }

            AssertItemLocked(IntegHashTableName, key1, item, t2.Id, false, true);
            AssertItemLocked(IntegHashTableName, key2, key2, t2.Id, true, false);

            t2.CommitAsync().Wait();

            AssertItemNotLocked(IntegHashTableName, key1, item, true);
            AssertItemNotLocked(IntegHashTableName, key2, false);

            commitFailingTransaction.CommitAsync().Wait();

            t2.DeleteAsync(long.MaxValue).Wait();
        }

        private class TransactionAnonymousInnerClass1 : Transaction
        {
            private readonly TransactionsIntegrationTest _outerInstance;

            public TransactionAnonymousInnerClass1(TransactionsIntegrationTest outerInstance, string getId, TransactionManager manager) : base(getId, manager, false)
            {
                this._outerInstance = outerInstance;
            }

            protected internal override Task UnlockItemAfterCommitAsync(Request request)
            {
                throw new FailingAmazonDynamoDbClient.FailedYourRequestException();
            }
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void driveRollback()
        public virtual void DriveRollback()
        {
            Transaction t1 = Manager.NewTransaction();
            Dictionary<string, AttributeValue> key1 = NewKey(IntegHashTableName);
            Dictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue>(key1);
            item1["attr1"] = new AttributeValue("original1");

            Dictionary<string, AttributeValue> key2 = NewKey(IntegHashTableName);
            Dictionary<string, AttributeValue> item2 = new Dictionary<string, AttributeValue>(key2);
            item1["attr2"] = new AttributeValue("original2");

            Dictionary<string, AttributeValue> key3 = NewKey(IntegHashTableName);

            t1.PutItemAsync(new PutItemRequest
            {
                TableName = IntegHashTableName,
                Item = item1,
            }).Wait();

            t1.PutItemAsync(new PutItemRequest
            {
                TableName = IntegHashTableName,
                Item = item2,
            }).Wait();

            t1.CommitAsync().Wait();
            t1.DeleteAsync(long.MaxValue).Wait();

            AssertItemNotLocked(IntegHashTableName, key1, item1, true);
            AssertItemNotLocked(IntegHashTableName, key2, item2, true);

            Transaction t2 = Manager.NewTransaction();

            Dictionary<string, AttributeValue> item1A = new Dictionary<string, AttributeValue>(item1);
            item1A["attr1"] = new AttributeValue("new1");
            item1A["attr2"] = new AttributeValue("new1");

            t2.PutItemAsync(new PutItemRequest
            {
                TableName = IntegHashTableName,
                Item = item1A,

            }).Wait();

            t2.GetItemAsync(new GetItemRequest
            {
                TableName = IntegHashTableName,
                Key = key2,

            }).Wait();

            t2.GetItemAsync(new GetItemRequest
            {
                TableName = IntegHashTableName,
                Key = key3,

            }).Wait();

            AssertItemLocked(IntegHashTableName, key1, item1A, t2.Id, false, true);
            AssertItemLocked(IntegHashTableName, key2, item2, t2.Id, false, false);
            AssertItemLocked(IntegHashTableName, key3, key3, t2.Id, true, false);

            Transaction rollbackFailingTransaction = new TransactionAnonymousInnerClass22(this, t2.Id, Manager);

            try
            {
                rollbackFailingTransaction.RollbackAsync().Wait();
                Fail();
            }
            catch (FailingAmazonDynamoDbClient.FailedYourRequestException)
            {
            }

            AssertItemLocked(IntegHashTableName, key1, item1A, t2.Id, false, true);
            AssertItemLocked(IntegHashTableName, key2, item2, t2.Id, false, false);
            AssertItemLocked(IntegHashTableName, key3, key3, t2.Id, true, false);

            t2.RollbackAsync().Wait();

            AssertItemNotLocked(IntegHashTableName, key1, item1, true);
            AssertItemNotLocked(IntegHashTableName, key2, item2, true);
            AssertItemNotLocked(IntegHashTableName, key3, false);

            rollbackFailingTransaction.RollbackAsync().Wait();

            t2.DeleteAsync(long.MaxValue).Wait();
        }

        private class TransactionAnonymousInnerClass22 : Transaction
        {
            private readonly TransactionsIntegrationTest _outerInstance;

            public TransactionAnonymousInnerClass22(TransactionsIntegrationTest outerInstance, string getId, TransactionManager manager) : base(getId, manager, false)
            {
                this._outerInstance = outerInstance;
            }

            protected internal override Task RollbackItemAndReleaseLockAsync(Request request)
            {
                throw new FailingAmazonDynamoDbClient.FailedYourRequestException();
            }
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void rollbackCompletedTransaction()
        public virtual void RollbackCompletedTransaction()
        {
            Transaction t1 = Manager.NewTransaction();
            Transaction rollbackFailingTransaction = new TransactionAnonymousInnerClass33(this, t1.Id, Manager);

            Dictionary<string, AttributeValue> key1 = NewKey(IntegHashTableName);
            t1.PutItemAsync(new PutItemRequest
            {
                TableName = IntegHashTableName,
                Item = key1,

            }).Wait();
            AssertItemLocked(IntegHashTableName, key1, key1, t1.Id, true, true);

            t1.RollbackAsync().Wait();
            rollbackFailingTransaction.RollbackAsync().Wait();
        }

        private class TransactionAnonymousInnerClass33 : Transaction
        {
            private readonly TransactionsIntegrationTest _outerInstance;

            public TransactionAnonymousInnerClass33(TransactionsIntegrationTest outerInstance, string getId, TransactionManager manager) : base(getId, manager, false)
            {
                this._outerInstance = outerInstance;
            }

            protected internal override Task DoRollbackAsync()
            {
                throw new FailingAmazonDynamoDbClient.FailedYourRequestException();
            }
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void commitCompletedTransaction()
        public virtual void CommitCompletedTransaction()
        {
            Transaction t1 = Manager.NewTransaction();
            Transaction commitFailingTransaction = new TransactionAnonymousInnerClass44(this, t1.Id, Manager);

            Dictionary<string, AttributeValue> key1 = NewKey(IntegHashTableName);
            t1.PutItemAsync(new PutItemRequest
            {
                TableName = IntegHashTableName,
                Item = key1,

            }).Wait();
            AssertItemLocked(IntegHashTableName, key1, key1, t1.Id, true, true);

            t1.CommitAsync().Wait();
            commitFailingTransaction.CommitAsync().Wait();
        }

        private class TransactionAnonymousInnerClass44 : Transaction
        {
            private readonly TransactionsIntegrationTest _outerInstance;

            public TransactionAnonymousInnerClass44(TransactionsIntegrationTest outerInstance, string getId, TransactionManager manager) : base(getId, manager, false)
            {
                this._outerInstance = outerInstance;
            }

            protected internal override Task DoCommitAsync()
            {
                throw new FailingAmazonDynamoDbClient.FailedYourRequestException();
            }
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void resumePendingTransaction()
        public virtual void ResumePendingTransaction()
        {
            Transaction t1 = Manager.NewTransaction();

            Dictionary<string, AttributeValue> key1 = NewKey(IntegHashTableName);
            Dictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue>(key1);
            item1["attr1"] = new AttributeValue("original1");

            Dictionary<string, AttributeValue> key2 = NewKey(IntegHashTableName);

            t1.PutItemAsync(new PutItemRequest
            {
                TableName = IntegHashTableName,
                Item = item1,

            }).Wait();

            AssertItemLocked(IntegHashTableName, key1, item1, t1.Id, true, true);
            AssertOldItemImage(t1.Id, IntegHashTableName, key1, key1, false);

            Transaction t2 = Manager.ResumeTransaction(t1.Id);

            t2.GetItemAsync(new GetItemRequest
            {
                TableName = IntegHashTableName,
                Key = key2,

            }).Wait();

            AssertItemLocked(IntegHashTableName, key1, item1, t1.Id, true, true);
            AssertOldItemImage(t1.Id, IntegHashTableName, key1, key1, false);
            AssertItemLocked(IntegHashTableName, key2, t1.Id, true, false);
            AssertOldItemImage(t1.Id, IntegHashTableName, key2, null, false);

            t2.CommitAsync().Wait();

            AssertItemNotLocked(IntegHashTableName, key1, true);
            AssertItemNotLocked(IntegHashTableName, key2, false);

            AssertOldItemImage(t1.Id, IntegHashTableName, key1, null, false);
            AssertOldItemImage(t1.Id, IntegHashTableName, key2, null, false);

            t2.DeleteAsync(long.MaxValue).Wait();
            AssertTransactionDeleted(t2);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void resumeAndCommitAfterTransientApplyFailure()
        public virtual void ResumeAndCommitAfterTransientApplyFailure()
        {
            Transaction t1 = new TransactionAnonymousInnerClass5(this, Guid.NewGuid().ToString(), Manager);

            Dictionary<string, AttributeValue> key1 = NewKey(IntegHashTableName);
            Dictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue>(key1);
            item1["attr1"] = new AttributeValue("original1");

            Dictionary<string, AttributeValue> key2 = NewKey(IntegHashTableName);

            try
            {
                t1.PutItemAsync(new PutItemRequest
                {
                    TableName = IntegHashTableName,
                    Item = item1,

                }).Wait();
                Fail();
            }
            catch (FailingAmazonDynamoDbClient.FailedYourRequestException)
            {
            }

            AssertItemLocked(IntegHashTableName, key1, key1, t1.Id, true, false);
            AssertOldItemImage(t1.Id, IntegHashTableName, key1, key1, false);

            Transaction t2 = Manager.ResumeTransaction(t1.Id);

            t2.GetItemAsync(new GetItemRequest
            {
                TableName = IntegHashTableName,
                Key = key2,

            }).Wait();

            AssertItemLocked(IntegHashTableName, key1, item1, t1.Id, true, true);
            AssertOldItemImage(t1.Id, IntegHashTableName, key1, key1, false);
            AssertItemLocked(IntegHashTableName, key2, t1.Id, true, false);
            AssertOldItemImage(t1.Id, IntegHashTableName, key2, null, false);

            Transaction t3 = Manager.ResumeTransaction(t1.Id);
            t3.CommitAsync().Wait();

            AssertItemNotLocked(IntegHashTableName, key1, item1, true);
            AssertItemNotLocked(IntegHashTableName, key2, false);

            AssertOldItemImage(t1.Id, IntegHashTableName, key1, null, false);
            AssertOldItemImage(t1.Id, IntegHashTableName, key2, null, false);

            t3.CommitAsync().Wait();

            t3.DeleteAsync(long.MaxValue).Wait();
            AssertTransactionDeleted(t2);
        }

        private class TransactionAnonymousInnerClass5 : Transaction
        {
            private readonly TransactionsIntegrationTest _outerInstance;

            public TransactionAnonymousInnerClass5(TransactionsIntegrationTest outerInstance, string toString, TransactionManager manager) : base(toString, manager, true)
            {
                this._outerInstance = outerInstance;
            }

            protected internal override Task<Dictionary<string, AttributeValue>> ApplyAndKeepLockAsync(Request request, Dictionary<string, AttributeValue> lockedItem)
            {
                throw new FailingAmazonDynamoDbClient.FailedYourRequestException();
            }
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void applyOnlyOnce()
        public virtual void ApplyOnlyOnce()
        {
            Transaction t1 = new TransactionAnonymousInnerClass6(this, Guid.NewGuid().ToString(), Manager);

            Dictionary<string, AttributeValue> key1 = NewKey(IntegHashTableName);
            Dictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue>(key1);
            item1["attr1"] = (new AttributeValue()).WithN("1");

            Dictionary<string, AttributeValue> key2 = NewKey(IntegHashTableName);

            Dictionary<string, AttributeValueUpdate> updates = new Dictionary<string, AttributeValueUpdate>();
            updates["attr1"] = new AttributeValueUpdate
            {
                Action = AttributeAction.ADD,
                Value = (new AttributeValue()).WithN("1")
            };

            UpdateItemRequest update = new UpdateItemRequest
            {
                TableName = IntegHashTableName,
                AttributeUpdates = updates,
                Key = key1,

            };

            try
            {
                t1.UpdateItemAsync(update).Wait();
                Fail();
            }
            catch (FailingAmazonDynamoDbClient.FailedYourRequestException)
            {
            }

            AssertItemLocked(IntegHashTableName, key1, item1, t1.Id, true, true);
            AssertOldItemImage(t1.Id, IntegHashTableName, key1, key1, false);

            Transaction t2 = Manager.ResumeTransaction(t1.Id);

            t2.GetItemAsync(new GetItemRequest
            {
                TableName = IntegHashTableName,
                Key = key2,

            }).Wait();

            AssertItemLocked(IntegHashTableName, key1, item1, t1.Id, true, true);
            AssertOldItemImage(t1.Id, IntegHashTableName, key1, key1, false);
            AssertItemLocked(IntegHashTableName, key2, t1.Id, true, false);
            AssertOldItemImage(t1.Id, IntegHashTableName, key2, null, false);

            t2.CommitAsync().Wait();

            AssertItemNotLocked(IntegHashTableName, key1, item1, true);
            AssertItemNotLocked(IntegHashTableName, key2, false);

            AssertOldItemImage(t1.Id, IntegHashTableName, key1, null, false);
            AssertOldItemImage(t1.Id, IntegHashTableName, key2, null, false);

            t2.DeleteAsync(long.MaxValue).Wait();
            AssertTransactionDeleted(t2);
        }

        private class TransactionAnonymousInnerClass6 : Transaction
        {
            private readonly TransactionsIntegrationTest _outerInstance;

            public TransactionAnonymousInnerClass6(TransactionsIntegrationTest outerInstance, string toString, TransactionManager manager) : base(toString, manager, true)
            {
                this._outerInstance = outerInstance;
            }

            protected internal override async Task<Dictionary<string, AttributeValue>> ApplyAndKeepLockAsync(Request request, Dictionary<string, AttributeValue> lockedItem)
            {
                await base.ApplyAndKeepLockAsync(request, lockedItem);
                throw new FailingAmazonDynamoDbClient.FailedYourRequestException();
            }
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void resumeRollbackAfterTransientApplyFailure()
        public virtual void ResumeRollbackAfterTransientApplyFailure()
        {
            Transaction t1 = new TransactionAnonymousInnerClass7(this, Guid.NewGuid().ToString(), Manager);

            Dictionary<string, AttributeValue> key1 = NewKey(IntegHashTableName);
            Dictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue>(key1);
            item1["attr1"] = new AttributeValue("original1");

            Dictionary<string, AttributeValue> key2 = NewKey(IntegHashTableName);

            try
            {
                t1.PutItemAsync(new PutItemRequest
                {
                    TableName = IntegHashTableName,
                    Item = item1,

                }).Wait();
                Fail();
            }
            catch (FailingAmazonDynamoDbClient.FailedYourRequestException)
            {
            }

            AssertItemLocked(IntegHashTableName, key1, key1, t1.Id, true, false);
            AssertOldItemImage(t1.Id, IntegHashTableName, key1, key1, false);

            Transaction t2 = Manager.ResumeTransaction(t1.Id);

            t2.GetItemAsync(new GetItemRequest
            {
                TableName = IntegHashTableName,
                Key = key2,

            });

            AssertItemLocked(IntegHashTableName, key1, item1, t1.Id, true, true);
            AssertOldItemImage(t1.Id, IntegHashTableName, key1, key1, false);
            AssertItemLocked(IntegHashTableName, key2, t1.Id, true, false);
            AssertOldItemImage(t1.Id, IntegHashTableName, key2, null, false);

            Transaction t3 = Manager.ResumeTransaction(t1.Id);
            t3.RollbackAsync().Wait();

            AssertItemNotLocked(IntegHashTableName, key1, false);
            AssertItemNotLocked(IntegHashTableName, key2, false);

            AssertOldItemImage(t1.Id, IntegHashTableName, key1, null, false);
            AssertOldItemImage(t1.Id, IntegHashTableName, key2, null, false);

            t3.DeleteAsync(long.MaxValue).Wait();
            AssertTransactionDeleted(t2);
        }

        private class TransactionAnonymousInnerClass7 : Transaction
        {
            private readonly TransactionsIntegrationTest _outerInstance;

            public TransactionAnonymousInnerClass7(TransactionsIntegrationTest outerInstance, string toString, TransactionManager manager) : base(toString, manager, true)
            {
                this._outerInstance = outerInstance;
            }

            protected internal override Task<Dictionary<string, AttributeValue>> ApplyAndKeepLockAsync(Request request, Dictionary<string, AttributeValue> lockedItem)
            {
                throw new FailingAmazonDynamoDbClient.FailedYourRequestException();
            }
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void unlockInRollbackIfNoItemImageSaved() throws InterruptedException
        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        public virtual void UnlockInRollbackIfNoItemImageSaved()
        {
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final Transaction t1 = new Transaction(java.util.Guid.NewGuid().toString(), manager, true)
            Transaction t1 = new TransactionAnonymousInnerClass8(this, Guid.NewGuid().ToString(), Manager);

            // Change the existing item key0, failing when trying to saveAsync away the item image
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> item0a = new java.util.HashMap<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> (item0);
            Dictionary<string, AttributeValue> item0A = new Dictionary<string, AttributeValue>(Item0);
            item0A["attr1"] = new AttributeValue("original1");

            try
            {
                t1.PutItemAsync(new PutItemRequest
                {
                    TableName = IntegHashTableName,
                    Item = item0A,

                }).Wait();
                Fail();
            }
            catch (FailingAmazonDynamoDbClient.FailedYourRequestException)
            {
            }

            AssertItemLocked(IntegHashTableName, Key0, Item0, t1.Id, false, false);

            // Roll back, and ensure the item was reverted correctly
            t1.RollbackAsync().Wait();

            AssertItemNotLocked(IntegHashTableName, Key0, Item0, true);
        }

        private class TransactionAnonymousInnerClass8 : Transaction
        {
            private readonly TransactionsIntegrationTest _outerInstance;

            public TransactionAnonymousInnerClass8(TransactionsIntegrationTest outerInstance, string toString, TransactionManager manager) : base(toString, manager, true)
            {
                this._outerInstance = outerInstance;
            }

            protected internal override void SaveItemImage(Request callerRequest, Dictionary<string, AttributeValue> item)
            {
                throw new FailingAmazonDynamoDbClient.FailedYourRequestException();
            }
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void shouldNotApplyAfterRollback() throws InterruptedException
        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        public virtual void ShouldNotApplyAfterRollback()
        {
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final java.util.concurrent.SemaphoreSlim barrier = new java.util.concurrent.SemaphoreSlim(0);
            SemaphoreSlim barrier = new SemaphoreSlim(0);
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final Transaction t1 = new Transaction(java.util.Guid.NewGuid().toString(), manager, true)
            Transaction t1 = new TransactionAnonymousInnerClass9(this, Guid.NewGuid().ToString(), Manager, barrier);

            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> key1 = newKey(INTEG_HASH_TABLE_NAME);
            Dictionary<string, AttributeValue> key1 = NewKey(IntegHashTableName);
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> item1 = new java.util.HashMap<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> (key1);
            Dictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue>(key1);
            item1["attr1"] = new AttributeValue("original1");

            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final java.util.concurrent.SemaphoreSlim caughtRolledBackException = new java.util.concurrent.SemaphoreSlim(0);
            SemaphoreSlim caughtRolledBackException = new SemaphoreSlim(0);

            Thread thread = new Thread(() =>
        {
            try
            {
                t1.PutItemAsync(new PutItemRequest
                {
                    TableName = IntegHashTableName,
                    Item = item1,

                }).Wait();
            }
            catch (TransactionRolledBackException)
            {
                caughtRolledBackException.Release();
            }
        });

            thread.Start();

            AssertItemNotLocked(IntegHashTableName, key1, false);
            Transaction t2 = Manager.ResumeTransaction(t1.Id);
            t2.RollbackAsync().Wait();
            AssertItemNotLocked(IntegHashTableName, key1, false);

            barrier.Release(100);

            thread.Join();

            AssertEquals(1, caughtRolledBackException.CurrentCount);

            AssertItemNotLocked(IntegHashTableName, key1, false);
            AssertTrue(t1.DeleteAsync(long.MinValue).Result);

            // Now start a new transaction involving key1 and make sure it will complete
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> item1a = new java.util.HashMap<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> (key1);
            Dictionary<string, AttributeValue> item1A = new Dictionary<string, AttributeValue>(key1);
            item1A["attr1"] = new AttributeValue("new");

            Transaction t3 = Manager.NewTransaction();
            t3.PutItemAsync(new PutItemRequest
            {
                TableName = IntegHashTableName,
                Item = item1A,

            });
            AssertItemLocked(IntegHashTableName, key1, item1A, t3.Id, true, true);
            t3.CommitAsync().Wait();
            AssertItemNotLocked(IntegHashTableName, key1, item1A, true);
        }

        private class TransactionAnonymousInnerClass9 : Transaction
        {
            private readonly TransactionsIntegrationTest _outerInstance;

            private SemaphoreSlim _barrier;

            public TransactionAnonymousInnerClass9(TransactionsIntegrationTest outerInstance, string toString, TransactionManager manager, SemaphoreSlim barrier) : base(toString, manager, true)
            {
                this._outerInstance = outerInstance;
                this._barrier = barrier;
            }

            //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
            //ORIGINAL LINE: @Override protected java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> lockItem(Request callerRequest, boolean expectExists, int attempts) throws com.amazonaws.services.dynamodbv2.transactions.exceptions.TransactionException
            protected internal override async Task<Dictionary<string, AttributeValue>> LockItemAsync(Request callerRequest, bool expectExists, int attempts)
            {
                //try
                //{
                _barrier.Wait();
                //}
                //catch (InterruptedException e)
                //{
                //    throw new Exception(e);
                //}
                return await base.LockItemAsync(callerRequest, expectExists, attempts);
            }
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void shouldNotApplyAfterRollbackAndDeleted() throws InterruptedException
        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        public virtual void ShouldNotApplyAfterRollbackAndDeleted()
        {
            // Very similar to "shouldNotApplyAfterRollback" except the transaction is rolled back and then deleted.
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final java.util.concurrent.SemaphoreSlim barrier = new java.util.concurrent.SemaphoreSlim(0);
            SemaphoreSlim barrier = new SemaphoreSlim(0);
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final Transaction t1 = new Transaction(java.util.Guid.NewGuid().toString(), manager, true)
            Transaction t1 = new TransactionAnonymousInnerClass10(this, Guid.NewGuid().ToString(), Manager, barrier);

            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> key1 = newKey(INTEG_HASH_TABLE_NAME);
            Dictionary<string, AttributeValue> key1 = NewKey(IntegHashTableName);
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> item1 = new java.util.HashMap<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> (key1);
            Dictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue>(key1);
            item1["attr1"] = new AttributeValue("original1");

            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final java.util.concurrent.SemaphoreSlim caughtTransactionNotFoundException = new java.util.concurrent.SemaphoreSlim(0);
            SemaphoreSlim caughtTransactionNotFoundException = new SemaphoreSlim(0);

            Thread thread = new Thread(() =>
        {
            try
            {
                t1.PutItemAsync(new PutItemRequest
                {
                    TableName = IntegHashTableName,
                    Item = item1,

                }).Wait();
            }
            catch (TransactionNotFoundException)
            {
                caughtTransactionNotFoundException.Release();
            }
        });

            thread.Start();

            AssertItemNotLocked(IntegHashTableName, key1, false);
            Transaction t2 = Manager.ResumeTransaction(t1.Id);
            t2.RollbackAsync().Wait();
            AssertItemNotLocked(IntegHashTableName, key1, false);
            AssertTrue(t2.DeleteAsync(long.MinValue).Result); // This is the key difference with shouldNotApplyAfterRollback

            barrier.Release(100);

            thread.Join();

            AssertEquals(1, caughtTransactionNotFoundException.CurrentCount);

            AssertItemNotLocked(IntegHashTableName, key1, false);

            // Now start a new transaction involving key1 and make sure it will complete
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> item1a = new java.util.HashMap<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> (key1);
            Dictionary<string, AttributeValue> item1A = new Dictionary<string, AttributeValue>(key1);
            item1A["attr1"] = new AttributeValue("new");

            Transaction t3 = Manager.NewTransaction();
            t3.PutItemAsync(new PutItemRequest
            {
                TableName = IntegHashTableName,
                Item = item1A,

            });
            AssertItemLocked(IntegHashTableName, key1, item1A, t3.Id, true, true);
            t3.CommitAsync().Wait();
            AssertItemNotLocked(IntegHashTableName, key1, item1A, true);
        }

        private class TransactionAnonymousInnerClass10 : Transaction
        {
            private readonly TransactionsIntegrationTest _outerInstance;

            private SemaphoreSlim _barrier;

            public TransactionAnonymousInnerClass10(TransactionsIntegrationTest outerInstance, string toString, TransactionManager manager, SemaphoreSlim barrier) : base(toString, manager, true)
            {
                this._outerInstance = outerInstance;
                this._barrier = barrier;
            }

            //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
            //ORIGINAL LINE: @Override protected java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> lockItem(Request callerRequest, boolean expectExists, int attempts) throws com.amazonaws.services.dynamodbv2.transactions.exceptions.TransactionException
            protected internal override async Task<Dictionary<string, AttributeValue>> LockItemAsync(Request callerRequest, bool expectExists, int attempts)
            {
                //try
                //{
                _barrier.Wait();
                //}
                //catch (InterruptedException e)
                //{
                //    throw new Exception(e);
                //}
                return await base.LockItemAsync(callerRequest, expectExists, attempts);
            }
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void shouldNotApplyAfterRollbackAndDeletedAndLeftLocked() throws InterruptedException
        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        public virtual void ShouldNotApplyAfterRollbackAndDeletedAndLeftLocked()
        {

            // Very similar to "shouldNotApplyAfterRollbackAndDeleted" except the lock is broken by a new transaction, not t1
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final java.util.concurrent.SemaphoreSlim barrier = new java.util.concurrent.SemaphoreSlim(0);
            SemaphoreSlim barrier = new SemaphoreSlim(0);
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final Transaction t1 = new Transaction(java.util.Guid.NewGuid().toString(), manager, true)
            Transaction t1 = new TransactionAnonymousInnerClass11(this, Guid.NewGuid().ToString(), Manager, barrier);

            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> key1 = newKey(INTEG_HASH_TABLE_NAME);
            Dictionary<string, AttributeValue> key1 = NewKey(IntegHashTableName);
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> item1 = new java.util.HashMap<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> (key1);
            Dictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue>(key1);
            item1["attr1"] = new AttributeValue("original1");

            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final java.util.concurrent.SemaphoreSlim caughtFailedYourRequestException = new java.util.concurrent.SemaphoreSlim(0);
            SemaphoreSlim caughtFailedYourRequestException = new SemaphoreSlim(0);

            Thread thread = new Thread(() =>
        {
            try
            {
                t1.PutItemAsync(new PutItemRequest
                {
                    TableName = IntegHashTableName,
                    Item = item1,

                }).Wait();
            }
            catch (FailingAmazonDynamoDbClient.FailedYourRequestException)
            {
                caughtFailedYourRequestException.Release();
            }
        });

            thread.Start();

            AssertItemNotLocked(IntegHashTableName, key1, false);
            Transaction t2 = Manager.ResumeTransaction(t1.Id);
            t2.RollbackAsync().Wait();
            AssertItemNotLocked(IntegHashTableName, key1, false);
            AssertTrue(t2.DeleteAsync(long.MinValue).Result);

            barrier.Release(100);

            thread.Join();

            AssertEquals(1, caughtFailedYourRequestException.CurrentCount);

            AssertItemLocked(IntegHashTableName, key1, null, t1.Id, true, false, false); // locked and "null", but don't check the tx item

            // Now start a new transaction involving key1 and make sure it will complete
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> item1a = new java.util.HashMap<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> (key1);
            Dictionary<string, AttributeValue> item1A = new Dictionary<string, AttributeValue>(key1);
            item1A["attr1"] = new AttributeValue("new");

            Transaction t3 = Manager.NewTransaction();
            t3.PutItemAsync(new PutItemRequest
            {
                TableName = IntegHashTableName,
                Item = item1A,

            }).Wait();
            AssertItemLocked(IntegHashTableName, key1, item1A, t3.Id, true, true);
            t3.CommitAsync().Wait();
            AssertItemNotLocked(IntegHashTableName, key1, item1A, true);
        }

        private class TransactionAnonymousInnerClass11 : Transaction
        {
            private readonly TransactionsIntegrationTest _outerInstance;

            private SemaphoreSlim _barrier;

            public TransactionAnonymousInnerClass11(TransactionsIntegrationTest outerInstance, string toString, TransactionManager manager, SemaphoreSlim barrier) : base(toString, manager, true)
            {
                this._outerInstance = outerInstance;
                this._barrier = barrier;
            }

            //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
            //ORIGINAL LINE: @Override protected java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> lockItem(Request callerRequest, boolean expectExists, int attempts) throws com.amazonaws.services.dynamodbv2.transactions.exceptions.TransactionException
            protected internal override async Task<Dictionary<string, AttributeValue>> LockItemAsync(Request callerRequest, bool expectExists, int attempts)
            {
                //try
                //{
                _barrier.Wait();
                //}
                //catch (InterruptedException e)
                //{
                //    throw new Exception(e);
                //}
                return await base.LockItemAsync(callerRequest, expectExists, attempts);
            }

            protected internal override Task ReleaseReadLockAsync(string tableName, Dictionary<string, AttributeValue> key)
            {
                throw new FailingAmazonDynamoDbClient.FailedYourRequestException();
            }
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void rollbackAfterReadLockUpgradeAttempt() throws InterruptedException
        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        public virtual void RollbackAfterReadLockUpgradeAttempt()
        {
            // After getting a read lock, attempt to write an update to the item. 
            // This will succeed in apply to the item, but will fail when trying to update the transaction item.
            // Scenario:
            // p1                    t1        i1           t2                 p2
            // ---- insert ---------->
            // --add get i1  -------->
            // --read lock+transient----------->
            //                                               <---------insert---
            //                                               <-----add get i1---
            //                                 <--------------------read i1 ----  (conflict detected)
            //                       <------------------------------read t1 ----  (going to roll it back)
            // -- add update i1 ----->
            // ---update i1  ------------------>
            //                       <------------------------------rollback t1-
            //
            //      Everything so far is fine, but this sets the stage for where the bug was
            //
            //                                X <-------------release read lock-
            //      This is where the bug used to be. p2 assumed t1 had a read lock
            //      on i1 and tried to do an optimized unlock, resulting in i1
            //      being stuck with a lock until manual lock busting.
            //      The correct behavior is for p2 not to assume that t1 has a read
            //      lock and always follow the right rollback procedures.

            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final java.util.concurrent.atomic.AtomicBoolean shouldThrowAfterApply = new java.util.concurrent.atomic.AtomicBoolean(false);
            AtomicBoolean shouldThrowAfterApply = new AtomicBoolean(false);

            // Now start another transaction that is going to try to read that same item,
            // but stop after you read the competing transaction record (don't try to roll it back yet)

            // t2 waits on this for the main thread to signal it.
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final java.util.concurrent.SemaphoreSlim waitAfterResumeTransaction = new java.util.concurrent.SemaphoreSlim(0);
            SemaphoreSlim waitAfterResumeTransaction = new SemaphoreSlim(0);

            // the main thread waits on this for t2 to signal that it's ready
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final java.util.concurrent.SemaphoreSlim resumedTransaction = new java.util.concurrent.SemaphoreSlim(0);
            SemaphoreSlim resumedTransaction = new SemaphoreSlim(0);

            // the main thread waits on this for t2 to finish with its rollback of t1
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final java.util.concurrent.SemaphoreSlim rolledBackT1 = new java.util.concurrent.SemaphoreSlim(0);
            SemaphoreSlim rolledBackT1 = new SemaphoreSlim(0);

            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final TransactionManager manager = new TransactionManager(dynamodb, INTEG_LOCK_TABLE_NAME, INTEG_IMAGES_TABLE_NAME)
            TransactionManager manager = new TransactionManagerAnonymousInnerClass(this, Dynamodb, IntegLockTableName, IntegImagesTableName, waitAfterResumeTransaction, resumedTransaction);

            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final Transaction t1 = new Transaction(java.util.Guid.NewGuid().toString(), manager, true)
            Transaction t1 = new TransactionAnonymousInnerClass12(this, Guid.NewGuid().ToString(), manager, shouldThrowAfterApply);

            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> key1 = newKey(INTEG_HASH_TABLE_NAME);
            Dictionary<string, AttributeValue> key1 = NewKey(IntegHashTableName);
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> key2 = newKey(INTEG_HASH_TABLE_NAME);
            Dictionary<string, AttributeValue> key2 = NewKey(IntegHashTableName);

            // Read an item that doesn't exist to get its read lock
            Dictionary<string, AttributeValue> item1Returned = t1.GetItemAsync(new GetItemRequest(IntegHashTableName, key1, true)).Result.Item;
            AssertNull(item1Returned);
            AssertItemLocked(IntegHashTableName, key1, t1.Id, true, false);

            Thread thread = new Thread(() =>
            {
                Transaction t2 = new TransactionAnonymousInnerClass13(this, Guid.NewGuid().ToString(), manager);
                // This will stop pause on waitAfterResumeTransaction once it finds that key1 is already locked by t1.
                Dictionary<string, AttributeValue> item1Returned2 = t2.GetItemAsync(new GetItemRequest(IntegHashTableName, key1, true)).Result.Item;
                AssertNull(item1Returned2);
                rolledBackT1.Release();
            });
            thread.Start();

            // Wait for t2 to get to the point where it loaded the t1 tx record.
            resumedTransaction.Wait();

            // Now change that getItem to an updateItemAsync in t1
            Dictionary<string, AttributeValueUpdate> updates = new Dictionary<string, AttributeValueUpdate>();
            updates["asdf"] = new AttributeValueUpdate(new AttributeValue("wef"), AttributeAction.PUT);
            t1.UpdateItemAsync(new UpdateItemRequest(IntegHashTableName, key1, updates)).Wait();

            // Now let t2 continue on and roll back t1
            waitAfterResumeTransaction.Release();

            // Wait for t2 to finish rolling back t1
            rolledBackT1.Wait();

            // T1 should be rolled back now and unable to do stuff
            try
            {
                t1.GetItemAsync(new GetItemRequest(IntegHashTableName, key2, true)).Wait();
                Fail();
            }
            catch (TransactionRolledBackException)
            {
                // expected
            }
        }

        private class TransactionAnonymousInnerClass12 : Transaction
        {
            private readonly TransactionsIntegrationTest _outerInstance;

            private AtomicBoolean _shouldThrowAfterApply;

            public TransactionAnonymousInnerClass12(TransactionsIntegrationTest outerInstance, string toString, TransactionManager manager, AtomicBoolean shouldThrowAfterApply) : base(toString, manager, true)
            {
                this._outerInstance = outerInstance;
                this._shouldThrowAfterApply = shouldThrowAfterApply;
            }

            protected internal override async Task<Dictionary<string, AttributeValue>> ApplyAndKeepLockAsync(Request request, Dictionary<string, AttributeValue> lockedItem)
            {
                Dictionary<string, AttributeValue> toReturn = await base.ApplyAndKeepLockAsync(request, lockedItem);
                if (_shouldThrowAfterApply.Value)
                {
                    throw new Exception("throwing as desired");
                }
                return toReturn;
            }
        }

        private class TransactionManagerAnonymousInnerClass : TransactionManager
        {
            private readonly TransactionsIntegrationTest _outerInstance;

            private SemaphoreSlim _waitAfterResumeTransaction;
            private SemaphoreSlim _resumedTransaction;

            public TransactionManagerAnonymousInnerClass(TransactionsIntegrationTest outerInstance, AmazonDynamoDBClient dynamodb, string integLockTableName, string integImagesTableName, SemaphoreSlim waitAfterResumeTransaction, SemaphoreSlim resumedTransaction) : base(dynamodb, integLockTableName, integImagesTableName)
            {
                this._outerInstance = outerInstance;
                this._waitAfterResumeTransaction = waitAfterResumeTransaction;
                this._resumedTransaction = resumedTransaction;
            }

            public override Transaction ResumeTransaction(string txId)
            {
                Transaction t = base.ResumeTransaction(txId);

                // Signal to the main thread that t2 has loaded the tx record.
                _resumedTransaction.Release();

                //try
                //{
                // Wait for the main thread to upgrade key1 to a write lock (but we won't know about it)
                _waitAfterResumeTransaction.Wait();
                //}
                //catch (InterruptedException e)
                //{
                //    throw new Exception(e);
                //}
                return t;
            }

        }

        private class TransactionAnonymousInnerClass13 : Transaction
        {
            private readonly TransactionsIntegrationTest _outerInstance;

            public TransactionAnonymousInnerClass13(TransactionsIntegrationTest outerInstance, string toString, TransactionManager manager) : base(toString, manager, true)
            {
                this._outerInstance = outerInstance;
            }

        }

        // TODO same as shouldNotLockAndApplyAfterRollbackAndDeleted except make t3 do the unlock, not t1.

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void basicNewItemRollback()
        public virtual void BasicNewItemRollback()
        {
            Transaction t1 = Manager.NewTransaction();
            Dictionary<string, AttributeValue> key1 = NewKey(IntegHashTableName);

            t1.UpdateItemAsync(new UpdateItemRequest
            {
                TableName = IntegHashTableName,
                Key = key1,

            }).Wait();
            AssertItemLocked(IntegHashTableName, key1, t1.Id, true, true);

            t1.RollbackAsync().Wait();
            AssertItemNotLocked(IntegHashTableName, key1, false);

            t1.DeleteAsync(long.MaxValue).Wait();
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void basicNewItemCommit()
        public virtual void BasicNewItemCommit()
        {
            Transaction t1 = Manager.NewTransaction();
            Dictionary<string, AttributeValue> key1 = NewKey(IntegHashTableName);

            t1.UpdateItemAsync(new UpdateItemRequest
            {
                TableName = IntegHashTableName,
                Key = key1,

            }).Wait();
            AssertItemLocked(IntegHashTableName, key1, t1.Id, true, true);

            t1.CommitAsync().Wait();
            AssertItemNotLocked(IntegHashTableName, key1, key1, true);
            t1.DeleteAsync(long.MaxValue).Wait();
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void missingTableName()
        public virtual void MissingTableName()
        {
            Transaction t1 = Manager.NewTransaction();
            Dictionary<string, AttributeValue> key1 = NewKey(IntegHashTableName);

            try
            {
                t1.UpdateItemAsync(new UpdateItemRequest
                {
                    Key = key1,

                }).Wait();
                Fail();
            }
            catch (InvalidRequestException e)
            {
                AssertTrue(e.Message, e.Message.Contains("TableName must not be null"));
            }
            AssertItemNotLocked(IntegHashTableName, key1, false);
            t1.RollbackAsync().Wait();
            AssertItemNotLocked(IntegHashTableName, key1, false);
            t1.DeleteAsync(long.MaxValue).Wait();
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void emptyTransaction()
        public virtual void EmptyTransaction()
        {
            Transaction t1 = Manager.NewTransaction();
            t1.CommitAsync().Wait();
            t1.DeleteAsync(long.MaxValue).Wait();
            AssertTransactionDeleted(t1);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void missingKey()
        public virtual void MissingKey()
        {
            Transaction t1 = Manager.NewTransaction();
            try
            {
                t1.UpdateItemAsync(new UpdateItemRequest
                {
                    TableName = IntegHashTableName,

                }).Wait();
                Fail();
            }
            catch (InvalidRequestException e)
            {
                AssertTrue(e.Message, e.Message.Contains("The request key cannot be empty"));
            }
            t1.RollbackAsync().Wait();
            t1.DeleteAsync(long.MaxValue).Wait();
        }

        /// <summary>
        /// This test makes a transaction with two large items, each of which are just below
        /// the DynamoDB item size limit (currently 400 KB).
        /// </summary>
        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void tooMuchDataInTransaction() throws Exception
        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        public virtual void TooMuchDataInTransaction()
        {
            Transaction t1 = Manager.NewTransaction();
            Transaction t2 = Manager.NewTransaction();
            Dictionary<string, AttributeValue> key1 = NewKey(IntegHashTableName);
            Dictionary<string, AttributeValue> key2 = NewKey(IntegHashTableName);

            // Write item 1 as a starting point
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < (MaxItemSizeBytes / 1.5); i++)
            {
                sb.Append("a");
            }
            string bigString = sb.ToString();

            Dictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue>(key1);
            item1["bigattr"] = new AttributeValue("little");
            t1.PutItemAsync(new PutItemRequest
            {
                TableName = IntegHashTableName,
                Item = item1,

            }).Wait();

            AssertItemLocked(IntegHashTableName, key1, item1, t1.Id, true, true);

            t1.CommitAsync().Wait();

            AssertItemNotLocked(IntegHashTableName, key1, item1, true);

            Dictionary<string, AttributeValue> item1A = new Dictionary<string, AttributeValue>(key1);
            item1A["bigattr"] = new AttributeValue(bigString);

            t2.PutItemAsync(new PutItemRequest
            {
                TableName = IntegHashTableName,
                Item = item1A,

            }).Wait();

            AssertItemLocked(IntegHashTableName, key1, item1A, t2.Id, false, true);

            Dictionary<string, AttributeValue> item2 = new Dictionary<string, AttributeValue>(key2);
            item2["bigattr"] = new AttributeValue(bigString);

            try
            {
                t2.PutItemAsync(new PutItemRequest
                {
                    TableName = IntegHashTableName,
                    Item = item2,

                }).Wait();
                Fail();
            }
            catch (InvalidRequestException)
            {
            }

            AssertItemNotLocked(IntegHashTableName, key2, false);
            AssertItemLocked(IntegHashTableName, key1, item1A, t2.Id, false, true);

            item2["bigattr"] = new AttributeValue("fitsThisTime");
            t2.PutItemAsync(new PutItemRequest
            {
                TableName = IntegHashTableName,
                Item = item2,

            }).Wait();

            AssertItemLocked(IntegHashTableName, key2, item2, t2.Id, true, true);

            t2.CommitAsync().Wait();

            AssertItemNotLocked(IntegHashTableName, key1, item1A, true);
            AssertItemNotLocked(IntegHashTableName, key2, item2, true);

            t1.DeleteAsync(long.MaxValue).Wait();
            t2.DeleteAsync(long.MaxValue).Wait();
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void containsBinaryAttributes()
        public virtual void ContainsBinaryAttributes()
        {
            Transaction t1 = Manager.NewTransaction();
            Dictionary<string, AttributeValue> key = NewKey(IntegHashTableName);
            Dictionary<string, AttributeValue> item = new Dictionary<string, AttributeValue>(key);

            item["attr_b"] = (new AttributeValue()).WithB(new MemoryStream(Encoding.ASCII.GetBytes("asdf\n\t\u0123")));
            item["attr_bs"] = (new AttributeValue()).WithBs(new List<MemoryStream> {
                new MemoryStream(Encoding.ASCII.GetBytes("asdf\n\t\u0123")),
                new MemoryStream(Encoding.ASCII.GetBytes("wef"))
            });

            t1.PutItemAsync(new PutItemRequest
            {
                TableName = IntegHashTableName,
                Item = item,

            }).Wait();

            AssertItemLocked(IntegHashTableName, key, item, t1.Id, true, true);

            t1.CommitAsync().Wait();

            AssertItemNotLocked(IntegHashTableName, key, item, true);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void containsJSONAttributes()
        public virtual void ContainsJsonAttributes()
        {
            Transaction t1 = Manager.NewTransaction();
            Dictionary<string, AttributeValue> key = NewKey(IntegHashTableName);
            Dictionary<string, AttributeValue> item = new Dictionary<string, AttributeValue>(key);

            item["attr_json"] = new AttributeValue
            {
                M = JsonMAttrVal,

            };

            t1.PutItemAsync(new PutItemRequest
            {
                TableName = IntegHashTableName,
                Item = item,

            }).Wait();

            AssertItemLocked(IntegHashTableName, key, item, t1.Id, true, true);

            t1.CommitAsync().Wait();

            AssertItemNotLocked(IntegHashTableName, key, item, true);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void containsSpecialAttributes()
        public virtual void ContainsSpecialAttributes()
        {
            Transaction t1 = Manager.NewTransaction();
            Dictionary<string, AttributeValue> key = NewKey(IntegHashTableName);
            Dictionary<string, AttributeValue> item = new Dictionary<string, AttributeValue>(key);
            item[Transaction.AttributeName.Txid.ToString()] = new AttributeValue("asdf");

            try
            {
                t1.PutItemAsync(new PutItemRequest
                {
                    TableName = IntegHashTableName,
                    Item = item,

                }).Wait();
                Fail();
            }
            catch (InvalidRequestException e)
            {
                AssertTrue(e.Message.Contains("must not contain the reserved"));
            }

            item[Transaction.AttributeName.Transient.ToString()] = new AttributeValue("asdf");
            item.Remove(Transaction.AttributeName.Txid.ToString());

            try
            {
                t1.PutItemAsync(new PutItemRequest
                {
                    TableName = IntegHashTableName,
                    Item = item,

                });
                Fail();
            }
            catch (InvalidRequestException e)
            {
                AssertTrue(e.Message.Contains("must not contain the reserved"));
            }

            item[Transaction.AttributeName.Applied.ToString()] = new AttributeValue("asdf");
            item.Remove(Transaction.AttributeName.Transient.ToString());

            try
            {
                t1.PutItemAsync(new PutItemRequest
                {
                    TableName = IntegHashTableName,
                    Item = item,

                }).Wait();
                Fail();
            }
            catch (InvalidRequestException e)
            {
                AssertTrue(e.Message.Contains("must not contain the reserved"));
            }
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void itemTooLargeToLock()
        public virtual void ItemTooLargeToLock()
        {

        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void itemTooLargeToApply()
        public virtual void ItemTooLargeToApply()
        {

        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void itemTooLargeToSavePreviousVersion()
        public virtual void ItemTooLargeToSavePreviousVersion()
        {

        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void failover() throws Exception
        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        public virtual void Failover()
        {
            Transaction t1 = new TransactionAnonymousInnerClass14(this, Guid.NewGuid().ToString(), Manager);

            // prepare a request
            UpdateItemRequest callerRequest = new UpdateItemRequest
            {
                TableName = IntegHashTableName,
                Key = NewKey(IntegHashTableName),

            };

            try
            {
                t1.UpdateItemAsync(callerRequest).Wait();
                Fail();
            }
            catch (FailingAmazonDynamoDbClient.FailedYourRequestException)
            {
            }
            AssertItemNotLocked(IntegHashTableName, callerRequest.Key, false);

            // The non-failing manager
            Transaction t2 = Manager.ResumeTransaction(t1.Id);
            t2.CommitAsync().Wait();

            AssertItemNotLocked(IntegHashTableName, callerRequest.Key, true);

            // If this attempted to apply again, this would fail because of the failing client
            t1.CommitAsync().Wait();

            AssertItemNotLocked(IntegHashTableName, callerRequest.Key, true);

            t1.DeleteAsync(long.MaxValue).Wait();
            t2.DeleteAsync(long.MaxValue).Wait();
        }

        private class TransactionAnonymousInnerClass14 : Transaction
        {
            private readonly TransactionsIntegrationTest _outerInstance;

            public TransactionAnonymousInnerClass14(TransactionsIntegrationTest outerInstance, string toString, TransactionManager manager) : base(toString, manager, true)
            {
                this._outerInstance = outerInstance;
            }

            //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
            //ORIGINAL LINE: @Override protected java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> lockItem(Request callerRequest, boolean expectExists, int attempts) throws com.amazonaws.services.dynamodbv2.transactions.exceptions.TransactionException
            protected internal override Task<Dictionary<string, AttributeValue>> LockItemAsync(Request callerRequest, bool expectExists, int attempts)
            {

                throw new FailingAmazonDynamoDbClient.FailedYourRequestException();
            }
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void oneTransactionPerItem()
        public virtual void OneTransactionPerItem()
        {
            Transaction transaction = Manager.NewTransaction();
            Dictionary<string, AttributeValue> key = NewKey(IntegHashTableName);

            transaction.PutItemAsync(new PutItemRequest
            {
                TableName = IntegHashTableName,
                Item = key,

            }).Wait();
            try
            {
                transaction.PutItemAsync(new PutItemRequest
                {
                    TableName = IntegHashTableName,
                    Item = key,

                }).Wait();
                Fail();
            }
            catch (DuplicateRequestException)
            {
                transaction.RollbackAsync().Wait();
            }
            AssertItemNotLocked(IntegHashTableName, key, false);
            transaction.DeleteAsync(long.MaxValue).Wait();
        }
    }
}