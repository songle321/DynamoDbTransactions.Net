﻿using System;
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

        private const int MAX_ITEM_SIZE_BYTES = 1024 * 400; // 400 KB

        internal static readonly Dictionary<string, AttributeValue> JSON_M_ATTR_VAL = new Dictionary<string, AttributeValue>();

        static TransactionsIntegrationTest()
        {
            JSON_M_ATTR_VAL["attr_s"] = (new AttributeValue()).withS("s");
            JSON_M_ATTR_VAL["attr_n"] = (new AttributeValue()).withN("1");
            JSON_M_ATTR_VAL["attr_b"] = (new AttributeValue()).withB(new MemoryStream(Encoding.ASCII.GetBytes("asdf")));
            JSON_M_ATTR_VAL["attr_ss"] = (new AttributeValue()).withSS(new List<string> { "a", "b" });
            JSON_M_ATTR_VAL["attr_ns"] = (new AttributeValue()).withNS(new List<string> { "1", "2" });
            JSON_M_ATTR_VAL["attr_bs"] = (new AttributeValue()).withBS(new List<MemoryStream>
            {
                new MemoryStream(Encoding.ASCII.GetBytes("asdf")),
                new MemoryStream(Encoding.ASCII.GetBytes("ghjk"))
            });
            JSON_M_ATTR_VAL["attr_bool"] = new AttributeValue
            {
                BOOL = true,

            };
            JSON_M_ATTR_VAL["attr_l"] = (new AttributeValue())
                .withL(new List<AttributeValue> {
                new AttributeValue().withS("s"),
                new AttributeValue().withN("1"),
                new AttributeValue().withB(new MemoryStream(Encoding.ASCII.GetBytes("asdf"))),
                new AttributeValue
                {
                    BOOL = true,

                }, new AttributeValue
                {
                    NULL = true,

                }});
            JSON_M_ATTR_VAL["attr_null"] = new AttributeValue
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
        public virtual void setup()
        {
            dynamodb.reset();
            Transaction t = manager.newTransaction();
            key0 = newKey(INTEG_HASH_TABLE_NAME);
            item0 = new Dictionary<string, AttributeValue>(key0);
            item0.Add("s_someattr", new AttributeValue("val"));
            item0.Add("ss_otherattr", (new AttributeValue()).withSS(new List<string>
            {
                "one",
                "two"
            }));
            Dictionary<string, AttributeValue> putResponse = t.putItemAsync(new PutItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Item = item0,
                ReturnValues = ReturnValue.ALL_OLD,

            }).Result.Attributes;
            assertNull(putResponse);
            t.commitAsync().Wait();
            assertItemNotLocked(INTEG_HASH_TABLE_NAME, key0, item0, true);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @After public void teardown()
        public virtual void teardown()
        {
            dynamodb.reset();
            Transaction t = manager.newTransaction();
            t.deleteItemAsync(new DeleteItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Key = key0,

            }).Wait();
            t.commitAsync().Wait();
            assertItemNotLocked(INTEG_HASH_TABLE_NAME, key0, false);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void phantomItemFromDelete()
        public virtual void phantomItemFromDelete()
        {
            Dictionary<string, AttributeValue> key1 = newKey(INTEG_HASH_TABLE_NAME);
            Transaction transaction = manager.newTransaction();
            DeleteItemRequest deleteRequest = new DeleteItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Key = key1,

            };
            transaction.deleteItemAsync(deleteRequest).Wait();
            assertItemLocked(INTEG_HASH_TABLE_NAME, key1, transaction.Id, true, false);
            transaction.rollbackAsync().Wait();
            assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, false);
            transaction.deleteAsync(long.MaxValue).Wait();
        }

        /*
		 * GetItem tests
		 */

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void lockItem()
        public virtual void lockItem()
        {
            Dictionary<string, AttributeValue> key1 = newKey(INTEG_HASH_TABLE_NAME);
            Transaction t1 = manager.newTransaction();
            Transaction t2 = manager.newTransaction();

            DeleteItemRequest deleteRequest = new DeleteItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Key = key1,

            };

            GetItemRequest lockRequest = new GetItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Key = key1,

            };

            Dictionary<string, AttributeValue> getResponse = t1.getItemAsync(lockRequest).Result.Item;

            assertItemLocked(INTEG_HASH_TABLE_NAME, key1, t1.Id, true, false); // we're not applying locks
            assertNull(getResponse);

            Dictionary<string, AttributeValue> deleteResponse = t2.deleteItemAsync(deleteRequest).Result.Attributes;
            assertItemLocked(INTEG_HASH_TABLE_NAME, key1, t2.Id, true, false); // we're not applying deletes either
            assertNull(deleteResponse); // return values is null in the request

            t2.commitAsync().Wait();

            try
            {
                t1.commitAsync().Wait();
                fail();
            }
            catch (TransactionRolledBackException)
            {
            }

            t1.deleteAsync(long.MaxValue).Wait();
            t2.deleteAsync(long.MaxValue).Wait();

            assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, false);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void lock2Items()
        public virtual void lock2Items()
        {
            Dictionary<string, AttributeValue> key1 = newKey(INTEG_HASH_TABLE_NAME);
            Dictionary<string, AttributeValue> key2 = newKey(INTEG_HASH_TABLE_NAME);

            Transaction t0 = manager.newTransaction();
            Dictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue>(key1);
            item1["something"] = new AttributeValue("val");
            Dictionary<string, AttributeValue> putResponse = t0.putItemAsync(new PutItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Item = item1,
                ReturnValues = ReturnValue.ALL_OLD,

            }).Result.Attributes;
            assertNull(putResponse);

            t0.commitAsync().Wait();

            Transaction t1 = manager.newTransaction();

            Dictionary<string, AttributeValue> getResult1 = t1.getItemAsync(new GetItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Key = key1,

            }).Result.Item;
            assertItemLocked(INTEG_HASH_TABLE_NAME, key1, item1, t1.Id, false, false);
            assertEquals(item1, getResult1);

            Dictionary<string, AttributeValue> getResult2 = t1.getItemAsync(new GetItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Key = key2,

            }).Result.Item;
            assertItemLocked(INTEG_HASH_TABLE_NAME, key1, item1, t1.Id, false, false);
            assertItemLocked(INTEG_HASH_TABLE_NAME, key2, t1.Id, true, false);
            assertNull(getResult2);

            t1.commitAsync().Wait();
            t1.deleteAsync(long.MaxValue).Wait();

            assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, item1, true);
            assertItemNotLocked(INTEG_HASH_TABLE_NAME, key2, false);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void getItemWithDelete()
        public virtual void getItemWithDelete()
        {
            Transaction t1 = manager.newTransaction();
            Dictionary<string, AttributeValue> getResult1 = t1.getItemAsync(new GetItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Key = key0,

            }).Result.Item;
            assertEquals(getResult1, item0);
            assertItemLocked(INTEG_HASH_TABLE_NAME, key0, item0, t1.Id, false, false);

            t1.deleteItemAsync(new DeleteItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Key = key0,

            }).Wait();
            assertItemLocked(INTEG_HASH_TABLE_NAME, key0, item0, t1.Id, false, false);

            Dictionary<string, AttributeValue> getResult2 = t1.getItemAsync(new GetItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Key = key0,

            }).Result.Item;
            assertNull(getResult2);
            assertItemLocked(INTEG_HASH_TABLE_NAME, key0, item0, t1.Id, false, false);

            t1.commitAsync().Wait();
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void getFilterAttributesToGet()
        public virtual void getFilterAttributesToGet()
        {
            Transaction t1 = manager.newTransaction();

            Dictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue>();
            item1["s_someattr"] = item0[("s_someattr")];

            Dictionary<string, AttributeValue> getResult1 = t1.getItemAsync(new GetItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                AttributesToGet = { "s_someattr", "notexists" },
                Key = key0
            }).Result.Item;
            assertEquals(item1, getResult1);
            assertItemLocked(INTEG_HASH_TABLE_NAME, key0, t1.Id, false, false);

            t1.commitAsync().Wait();

            assertItemNotLocked(INTEG_HASH_TABLE_NAME, key0, item0, true);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void getItemNotExists()
        public virtual void getItemNotExists()
        {
            Transaction t1 = manager.newTransaction();
            Dictionary<string, AttributeValue> key1 = newKey(INTEG_HASH_TABLE_NAME);

            Dictionary<string, AttributeValue> getResult1 = t1.getItemAsync(new GetItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Key = key1,

            }).Result.Item;
            assertNull(getResult1);
            assertItemLocked(INTEG_HASH_TABLE_NAME, key1, t1.Id, true, false);

            Dictionary<string, AttributeValue> getResult2 = t1.getItemAsync(new GetItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Key = key1,

            }).Result.Item;
            assertNull(getResult2);
            assertItemLocked(INTEG_HASH_TABLE_NAME, key1, t1.Id, true, false);

            t1.commitAsync().Wait();

            assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, false);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void getItemAfterPutItemInsert()
        public virtual void getItemAfterPutItemInsert()
        {
            Transaction t1 = manager.newTransaction();
            Dictionary<string, AttributeValue> key1 = newKey(INTEG_HASH_TABLE_NAME);
            Dictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue>(key1);
            item1["asdf"] = new AttributeValue("wef");

            Dictionary<string, AttributeValue> getResult1 = t1.getItemAsync(new GetItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Key = key1,

            }).Result.Item;
            assertNull(getResult1);
            assertItemLocked(INTEG_HASH_TABLE_NAME, key1, t1.Id, true, false);

            Dictionary<string, AttributeValue> putResult1 = t1.putItemAsync(new PutItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Item = item1,
                ReturnValues = ReturnValue.ALL_OLD,

            }).Result.Attributes;
            assertItemLocked(INTEG_HASH_TABLE_NAME, key1, item1, t1.Id, true, true);
            assertNull(putResult1);

            Dictionary<string, AttributeValue> getResult2 = t1.getItemAsync(new GetItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Key = key1,

            }).Result.Item;
            assertEquals(getResult2, item1);
            assertItemLocked(INTEG_HASH_TABLE_NAME, key1, item1, t1.Id, true, true);

            t1.commitAsync().Wait();

            assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, item1, true);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void getItemAfterPutItemOverwrite()
        public virtual void getItemAfterPutItemOverwrite()
        {
            Transaction t1 = manager.newTransaction();
            Dictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue>(item0);
            item1["asdf"] = new AttributeValue("wef");

            Dictionary<string, AttributeValue> getResult1 = t1.getItemAsync(new GetItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Key = key0,

            }).Result.Item;
            assertEquals(getResult1, item0);
            assertItemLocked(INTEG_HASH_TABLE_NAME, key0, item0, t1.Id, false, false);

            Dictionary<string, AttributeValue> putResult1 = t1.putItemAsync(new PutItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Item = item1,
                ReturnValues = ReturnValue.ALL_OLD,

            }).Result.Attributes;
            assertItemLocked(INTEG_HASH_TABLE_NAME, key0, item1, t1.Id, false, true);
            assertEquals(putResult1, item0);

            Dictionary<string, AttributeValue> getResult2 = t1.getItemAsync(new GetItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Key = key0,

            }).Result.Item;
            assertEquals(getResult2, item1);
            assertItemLocked(INTEG_HASH_TABLE_NAME, key0, item1, t1.Id, false, true);

            t1.commitAsync().Wait();

            assertItemNotLocked(INTEG_HASH_TABLE_NAME, key0, item1, true);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void getItemAfterPutItemInsertInResumedTx()
        public virtual void getItemAfterPutItemInsertInResumedTx()
        {
            Transaction t1 = new TransactionAnonymousInnerClass(this, Guid.NewGuid().ToString(), manager);

            Transaction t2 = manager.resumeTransaction(t1.Id);

            Dictionary<string, AttributeValue> key1 = newKey(INTEG_HASH_TABLE_NAME);
            Dictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue>(key1);
            item1["asdf"] = new AttributeValue("wef");

            try
            {
                // This Put needs to fail in apply
                t1.putItemAsync(new PutItemRequest
                {
                    TableName = INTEG_HASH_TABLE_NAME,
                    Item = item1,
                    ReturnValues = ReturnValue.ALL_OLD,

                }).Wait();
                fail();
            }
            catch (FailingAmazonDynamoDBClient.FailedYourRequestException)
            {
            }
            assertItemLocked(INTEG_HASH_TABLE_NAME, key1, t1.Id, true, false);

            // second copy of same tx
            Dictionary<string, AttributeValue> getResult1 = t2.getItemAsync(new GetItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Key = key1,

            }).Result.Item;
            assertEquals(getResult1, item1);
            assertItemLocked(INTEG_HASH_TABLE_NAME, key1, item1, t1.Id, true, true);

            t2.commitAsync().Wait();

            assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, item1, true);
        }

        private class TransactionAnonymousInnerClass : Transaction
        {
            private readonly TransactionsIntegrationTest outerInstance;

            public TransactionAnonymousInnerClass(TransactionsIntegrationTest outerInstance, string toString, TransactionManager manager) : base(toString, manager, true)
            {
                this.outerInstance = outerInstance;
            }

            protected internal override Task<Dictionary<string, AttributeValue>> applyAndKeepLockAsync(Request request, Dictionary<string, AttributeValue> lockedItem)
            {
                throw new FailingAmazonDynamoDBClient.FailedYourRequestException();
            }
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void getItemThenPutItemInResumedTxThenGetItem()
        public virtual void getItemThenPutItemInResumedTxThenGetItem()
        {
            Transaction t1 = new TransactionAnonymousInnerClass2(this, Guid.NewGuid().ToString(), manager);

            Transaction t2 = manager.resumeTransaction(t1.Id);

            Dictionary<string, AttributeValue> key1 = newKey(INTEG_HASH_TABLE_NAME);
            Dictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue>(key1);
            item1["asdf"] = new AttributeValue("wef");

            // Get a read lock in t2
            Dictionary<string, AttributeValue> getResult1 = t2.getItemAsync(new GetItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Key = key1,

            }).Result.Item;
            assertNull(getResult1);
            assertItemLocked(INTEG_HASH_TABLE_NAME, key1, null, t1.Id, true, false);

            // Begin a PutItem in t1, but fail apply
            try
            {
                t1.putItemAsync(new PutItemRequest
                {
                    TableName = INTEG_HASH_TABLE_NAME,
                    Item = item1,
                    ReturnValues = ReturnValue.ALL_OLD,

                }).Wait();
                fail();
            }
            catch (FailingAmazonDynamoDBClient.FailedYourRequestException)
            {
            }
            assertItemLocked(INTEG_HASH_TABLE_NAME, key1, t1.Id, true, false);

            // Read again in the non-failing copy of the transaction
            Dictionary<string, AttributeValue> getResult2 = t2.getItemAsync(new GetItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Key = key1,

            }).Result.Item;
            assertItemLocked(INTEG_HASH_TABLE_NAME, key1, item1, t1.Id, true, true);
            t2.commitAsync().Wait();
            assertEquals(item1, getResult2);

            assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, item1, true);
        }

        private class TransactionAnonymousInnerClass2 : Transaction
        {
            private readonly TransactionsIntegrationTest outerInstance;

            public TransactionAnonymousInnerClass2(TransactionsIntegrationTest outerInstance, string toString, TransactionManager manager) : base(toString, manager, true)
            {
                this.outerInstance = outerInstance;
            }

            protected internal override async Task<Dictionary<string, AttributeValue>> applyAndKeepLockAsync(Request request, Dictionary<string, AttributeValue> lockedItem)
            {
                if (request is Request.GetItem || request is Request.DeleteItem)
                {
                    return await base.applyAndKeepLockAsync(request, lockedItem);
                }
                throw new FailingAmazonDynamoDBClient.FailedYourRequestException();
            }
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void getThenUpdateNewItem()
        public virtual void getThenUpdateNewItem()
        {
            Transaction t1 = manager.newTransaction();
            Dictionary<string, AttributeValue> key1 = newKey(INTEG_HASH_TABLE_NAME);

            Dictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue>(key1);
            item1["asdf"] = new AttributeValue("didn't exist");

            Dictionary<string, AttributeValueUpdate> updates1 = new Dictionary<string, AttributeValueUpdate>();
            updates1["asdf"] = new AttributeValueUpdate(new AttributeValue("didn't exist"), AttributeAction.PUT);

            Dictionary<string, AttributeValue> getResponse = t1.getItemAsync(new GetItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Key = key1,

            }).Result.Item;
            assertItemLocked(INTEG_HASH_TABLE_NAME, key1, t1.Id, true, false);
            assertNull(getResponse);

            Dictionary<string, AttributeValue> updateResponse = t1.updateItemAsync(new UpdateItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Key = key1,
                AttributeUpdates = updates1,
                ReturnValues = ReturnValue.ALL_NEW,

            }).Result.Attributes;
            assertItemLocked(INTEG_HASH_TABLE_NAME, key1, item1, t1.Id, true, true);
            assertEquals(item1, updateResponse);

            t1.commitAsync().Wait();

            assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, item1, true);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void getThenUpdateExistingItem()
        public virtual void getThenUpdateExistingItem()
        {
            Transaction t1 = manager.newTransaction();

            Dictionary<string, AttributeValue> item0a = new Dictionary<string, AttributeValue>(item0);
            item0a["wef"] = new AttributeValue("new attr");

            Dictionary<string, AttributeValueUpdate> updates1 = new Dictionary<string, AttributeValueUpdate>();
            updates1["wef"] = new AttributeValueUpdate(new AttributeValue("new attr"), AttributeAction.PUT);

            Dictionary<string, AttributeValue> getResponse = t1.getItemAsync(new GetItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Key = key0,

            }).Result.Item;
            assertItemLocked(INTEG_HASH_TABLE_NAME, key0, item0, t1.Id, false, false);
            assertEquals(item0, getResponse);

            Dictionary<string, AttributeValue> updateResponse = t1.updateItemAsync(new UpdateItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Key = key0,
                AttributeUpdates = updates1,
                ReturnValues = ReturnValue.ALL_NEW,

            }).Result.Attributes;
            assertItemLocked(INTEG_HASH_TABLE_NAME, key0, item0a, t1.Id, false, true);
            assertEquals(item0a, updateResponse);

            t1.commitAsync().Wait();

            assertItemNotLocked(INTEG_HASH_TABLE_NAME, key0, item0a, true);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void getItemUncommittedInsert()
        public virtual void getItemUncommittedInsert()
        {
            Transaction t1 = manager.newTransaction();

            Dictionary<string, AttributeValue> key1 = newKey(INTEG_HASH_TABLE_NAME);
            Dictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue>(key1);
            item1["asdf"] = new AttributeValue("wef");

            t1.putItemAsync(new PutItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Item = item1,

            });

            assertItemLocked(INTEG_HASH_TABLE_NAME, key1, item1, t1.Id, true, true);

            Dictionary<string, AttributeValue> item = manager.GetItemAsync(new GetItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Key = key1,

            }, Transaction.IsolationLevel.UNCOMMITTED).Result.Item;
            assertNoSpecialAttributes(item);
            assertEquals(item1, item);
            assertItemLocked(INTEG_HASH_TABLE_NAME, key1, item1, t1.Id, true, true);

            t1.rollbackAsync().Wait();
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void getItemUncommittedDeleted()
        public virtual void getItemUncommittedDeleted()
        {
            Transaction t1 = manager.newTransaction();

            t1.deleteItemAsync(new DeleteItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Key = key0,

            }).Wait();

            assertItemLocked(INTEG_HASH_TABLE_NAME, key0, item0, t1.Id, false, false);

            Dictionary<string, AttributeValue> item = manager.GetItemAsync(new GetItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Key = key0,

            }, Transaction.IsolationLevel.UNCOMMITTED).Result.Item;
            assertNoSpecialAttributes(item);
            assertEquals(item0, item);
            assertItemLocked(INTEG_HASH_TABLE_NAME, key0, item0, t1.Id, false, false);

            t1.rollbackAsync().Wait();
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void getItemCommittedInsert()
        public virtual void getItemCommittedInsert()
        {
            Transaction t1 = manager.newTransaction();

            Dictionary<string, AttributeValue> key1 = newKey(INTEG_HASH_TABLE_NAME);
            Dictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue>(key1);
            item1["asdf"] = new AttributeValue("wef");

            t1.putItemAsync(new PutItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Item = item1,

            }).Wait();

            assertItemLocked(INTEG_HASH_TABLE_NAME, key1, item1, t1.Id, true, true);

            Dictionary<string, AttributeValue> item = manager.GetItemAsync(new GetItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Key = key1,

            }, Transaction.IsolationLevel.COMMITTED).Result.Item;
            assertNull(item);
            assertItemLocked(INTEG_HASH_TABLE_NAME, key1, item1, t1.Id, true, true);

            t1.rollbackAsync().Wait();
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void getItemCommittedDeleted()
        public virtual void getItemCommittedDeleted()
        {
            Transaction t1 = manager.newTransaction();

            t1.deleteItemAsync(new DeleteItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Key = key0,

            }).Wait();

            assertItemLocked(INTEG_HASH_TABLE_NAME, key0, item0, t1.Id, false, false);

            Dictionary<string, AttributeValue> item = manager.GetItemAsync(new GetItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Key = key0,

            }, Transaction.IsolationLevel.COMMITTED).Result.Item;
            assertNoSpecialAttributes(item);
            assertEquals(item0, item);
            assertItemLocked(INTEG_HASH_TABLE_NAME, key0, item0, t1.Id, false, false);

            t1.rollbackAsync().Wait();
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void getItemCommittedUpdated()
        public virtual void getItemCommittedUpdated()
        {
            Transaction t1 = manager.newTransaction();

            Dictionary<string, AttributeValueUpdate> updates = new Dictionary<string, AttributeValueUpdate>();
            updates["asdf"] = new AttributeValueUpdate
            {
                Action = AttributeAction.PUT,
                Value = new AttributeValue("asdf")
            };
            Dictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue>(item0);
            item1["asdf"] = new AttributeValue("asdf");

            t1.updateItemAsync(new UpdateItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                AttributeUpdates = updates,
                Key = key0,

            }).Wait();

            assertItemLocked(INTEG_HASH_TABLE_NAME, key0, item1, t1.Id, false, true);

            Dictionary<string, AttributeValue> item = manager.GetItemAsync(new GetItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Key = key0,

            }, Transaction.IsolationLevel.COMMITTED).Result.Item;
            assertNoSpecialAttributes(item);
            assertEquals(item0, item);
            assertItemLocked(INTEG_HASH_TABLE_NAME, key0, item1, t1.Id, false, true);

            t1.commitAsync().Wait();

            item = manager.GetItemAsync(new GetItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Key = key0,

            }, Transaction.IsolationLevel.COMMITTED).Result.Item;
            assertNoSpecialAttributes(item);
            assertEquals(item1, item);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void getItemCommittedUpdatedAndApplied()
        public virtual void getItemCommittedUpdatedAndApplied()
        {
            Transaction t1 = new TransactionAnonymousInnerClass3(this, Guid.NewGuid().ToString(), manager);

            Dictionary<string, AttributeValueUpdate> updates = new Dictionary<string, AttributeValueUpdate>();
            updates["asdf"] = new AttributeValueUpdate
            {
                Action = AttributeAction.PUT,
                Value = new AttributeValue("asdf")
            };
            Dictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue>(item0);
            item1["asdf"] = new AttributeValue("asdf");

            t1.updateItemAsync(new UpdateItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                AttributeUpdates = updates,
                Key = key0,

            }).Wait();

            assertItemLocked(INTEG_HASH_TABLE_NAME, key0, item1, t1.Id, false, true);

            t1.commitAsync().Wait();

            Dictionary<string, AttributeValue> item = manager.GetItemAsync(new GetItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Key = key0,

            }, Transaction.IsolationLevel.COMMITTED).Result.Item;
            assertNoSpecialAttributes(item);
            assertEquals(item1, item);
            assertItemLocked(INTEG_HASH_TABLE_NAME, key0, item1, t1.Id, false, true);
        }

        private class TransactionAnonymousInnerClass3 : Transaction
        {
            private readonly TransactionsIntegrationTest outerInstance;

            public TransactionAnonymousInnerClass3(TransactionsIntegrationTest outerInstance, string toString, TransactionManager manager) : base(toString, manager, true)
            {
                this.outerInstance = outerInstance;
            }

            protected internal override async Task doCommitAsync()
            {
                //Skip cleaning up the transaction so we can validate reading.
            }
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void getItemCommittedMissingImage()
        public virtual void getItemCommittedMissingImage()
        {
            Transaction t1 = manager.newTransaction();
            Dictionary<string, AttributeValueUpdate> updates = new Dictionary<string, AttributeValueUpdate>();
            updates["asdf"] = new AttributeValueUpdate
            {
                Action = AttributeAction.PUT,

                Value = new AttributeValue("asdf")
            };
            Dictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue>(item0);
            item1["asdf"] = new AttributeValue("asdf");

            t1.updateItemAsync(new UpdateItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                AttributeUpdates = updates,
                Key = key0,

            }).Wait();

            assertItemLocked(INTEG_HASH_TABLE_NAME, key0, item1, t1.Id, false, true);

            GetItemRequest deletedGetRequests = new GetItemRequest
            {
                TableName = manager.ItemImageTableName,
                ConsistentRead = true
            };
            deletedGetRequests.Key.Add(Transaction.AttributeName.IMAGE_ID.ToString(),
                new AttributeValue(t1.TxItem.txId + "#" + 1));

            dynamodb.getRequestsToTreatAsDeleted.Add(deletedGetRequests);

            try
            {
                manager.GetItemAsync(new GetItemRequest
                {
                    TableName = INTEG_HASH_TABLE_NAME,
                    Key = key0,
                }, Transaction.IsolationLevel.COMMITTED).Wait();
                fail("Should have thrown an exception.");
            }
            catch (TransactionException e)
            {
                assertEquals("null - Ran out of attempts to get a committed image of the item", e.Message);
            }
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void getItemCommittedConcurrentCommit()
        public virtual void getItemCommittedConcurrentCommit()
        {
            //Test reading an item while simulating another transaction committing concurrently.
            //To do this we skip cleanup, make the item image appear to be deleted,
            //and then make the reader get the uncommitted version of the transaction 
            //row for the first read and then actual updated version for later reads.

            Transaction t1 = new TransactionAnonymousInnerClass4(this, Guid.NewGuid().ToString(), manager);
            Dictionary<string, AttributeValueUpdate> updates = new Dictionary<string, AttributeValueUpdate>();
            updates["asdf"] = new AttributeValueUpdate
            {
                Action = AttributeAction.PUT,

                Value = new AttributeValue("asdf")
            };
            Dictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue>(item0);
            item1["asdf"] = new AttributeValue("asdf");

            t1.updateItemAsync(new UpdateItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                AttributeUpdates = updates,
                Key = key0,

            }).Wait();

            assertItemLocked(INTEG_HASH_TABLE_NAME, key0, item1, t1.Id, false, true);

            GetItemRequest txItemRequest = new GetItemRequest
            {
                TableName = manager.TransactionTableName,
                ConsistentRead = true
            };
            txItemRequest.Key.Add(Transaction.AttributeName.TXID.ToString(), new AttributeValue(t1.TxItem.txId));

            //Save the copy of the transaction before commit. 
            GetItemResponse uncommittedTransaction = dynamodb.GetItemAsync(txItemRequest).Result;

            t1.commitAsync().Wait();
            assertItemLocked(INTEG_HASH_TABLE_NAME, key0, item1, t1.Id, false, true);

            dynamodb.getRequestsToStub.Add(txItemRequest, new LinkedList<GetItemResponse>(Collections.singletonList(uncommittedTransaction)));
            //Stub out the image so it appears deleted
            var getItemRequest = new GetItemRequest
            {
                TableName = manager.ItemImageTableName,
                ConsistentRead = true
            };
            getItemRequest.Key.Add(Transaction.AttributeName.IMAGE_ID.ToString(), new AttributeValue(t1.TxItem.txId + "#" + 1));

            dynamodb.getRequestsToTreatAsDeleted.Add(getItemRequest);

            Dictionary<string, AttributeValue> item = manager.GetItemAsync(new GetItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Key = key0,

            }, Transaction.IsolationLevel.COMMITTED).Result.Item;
            assertNoSpecialAttributes(item);
            assertEquals(item1, item);
        }

        private class TransactionAnonymousInnerClass4 : Transaction
        {
            private readonly TransactionsIntegrationTest outerInstance;

            public TransactionAnonymousInnerClass4(TransactionsIntegrationTest outerInstance, string toString, TransactionManager manager) : base(toString, manager, true)
            {
                this.outerInstance = outerInstance;
            }

            protected internal override async Task doCommitAsync()
            {
                //Skip cleaning up the transaction so we can validate reading.
            }
        }

        /*
		 * ReturnValues tests
		 */

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void putItemAllOldInsert()
        public virtual void putItemAllOldInsert()
        {
            Transaction t1 = manager.newTransaction();
            Dictionary<string, AttributeValue> key1 = newKey(INTEG_HASH_TABLE_NAME);
            Dictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue>(key1);
            item1["asdf"] = new AttributeValue("wef");

            Dictionary<string, AttributeValue> putResult1 = t1.putItemAsync(new PutItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Item = item1,
                ReturnValues = ReturnValue.ALL_OLD,

            }).Result.Attributes;
            assertItemLocked(INTEG_HASH_TABLE_NAME, key1, item1, t1.Id, true, true);
            assertNull(putResult1);

            t1.commitAsync().Wait();

            assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, item1, true);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void putItemAllOldOverwrite()
        public virtual void putItemAllOldOverwrite()
        {
            Transaction t1 = manager.newTransaction();
            Dictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue>(item0);
            item1["asdf"] = new AttributeValue("wef");

            Dictionary<string, AttributeValue> putResult1 = t1.putItemAsync(new PutItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Item = item1,
                ReturnValues = ReturnValue.ALL_OLD,

            }).Result.Attributes;
            assertItemLocked(INTEG_HASH_TABLE_NAME, key0, item1, t1.Id, false, true);
            assertEquals(putResult1, item0);

            t1.commitAsync().Wait();

            assertItemNotLocked(INTEG_HASH_TABLE_NAME, key0, item1, true);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void updateItemAllOldInsert()
        public virtual void updateItemAllOldInsert()
        {
            Transaction t1 = manager.newTransaction();
            Dictionary<string, AttributeValue> key1 = newKey(INTEG_HASH_TABLE_NAME);
            Dictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue>(key1);
            item1["asdf"] = new AttributeValue("wef");
            Dictionary<string, AttributeValueUpdate> updates = new Dictionary<string, AttributeValueUpdate>();
            updates["asdf"] = new AttributeValueUpdate
            {
                Action = AttributeAction.PUT,
                Value = new AttributeValue("wef")
            };

            Dictionary<string, AttributeValue> result1 = t1.updateItemAsync(new UpdateItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Key = key1,
                AttributeUpdates = updates,
                ReturnValues = ReturnValue.ALL_OLD,

            }).Result.Attributes;
            assertItemLocked(INTEG_HASH_TABLE_NAME, key1, item1, t1.Id, true, true);
            assertNull(result1);

            t1.commitAsync().Wait();

            assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, item1, true);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void updateItemAllOldOverwrite()
        public virtual void updateItemAllOldOverwrite()
        {
            Transaction t1 = manager.newTransaction();
            Dictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue>(item0);
            item1["asdf"] = new AttributeValue("wef");
            Dictionary<string, AttributeValueUpdate> updates = new Dictionary<string, AttributeValueUpdate>();
            updates["asdf"] = new AttributeValueUpdate
            {
                Action = AttributeAction.PUT,
                Value = new AttributeValue("wef")
            };

            Dictionary<string, AttributeValue> result1 = t1.updateItemAsync(new UpdateItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Key = key0,
                AttributeUpdates = updates,
                ReturnValues = ReturnValue.ALL_OLD,

            }).Result.Attributes;
            assertItemLocked(INTEG_HASH_TABLE_NAME, key0, item1, t1.Id, false, true);
            assertEquals(result1, item0);

            t1.commitAsync().Wait();

            assertItemNotLocked(INTEG_HASH_TABLE_NAME, key0, item1, true);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void updateItemAllNewInsert()
        public virtual void updateItemAllNewInsert()
        {
            Transaction t1 = manager.newTransaction();
            Dictionary<string, AttributeValue> key1 = newKey(INTEG_HASH_TABLE_NAME);
            Dictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue>(key1);
            item1["asdf"] = new AttributeValue("wef");
            Dictionary<string, AttributeValueUpdate> updates = new Dictionary<string, AttributeValueUpdate>();
            updates["asdf"] = new AttributeValueUpdate
            {
                Action = AttributeAction.PUT,
                Value = new AttributeValue("wef")
            };

            Dictionary<string, AttributeValue> result1 = t1.updateItemAsync(new UpdateItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Key = key1,
                AttributeUpdates = updates,
                ReturnValues = ReturnValue.ALL_NEW,

            }).Result.Attributes;
            assertItemLocked(INTEG_HASH_TABLE_NAME, key1, item1, t1.Id, true, true);
            assertEquals(result1, item1);

            t1.commitAsync().Wait();

            assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, item1, true);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void updateItemAllNewOverwrite()
        public virtual void updateItemAllNewOverwrite()
        {
            Transaction t1 = manager.newTransaction();
            Dictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue>(item0);
            item1["asdf"] = new AttributeValue("wef");
            Dictionary<string, AttributeValueUpdate> updates = new Dictionary<string, AttributeValueUpdate>();
            updates["asdf"] = new AttributeValueUpdate
            {
                Action = AttributeAction.PUT,
                Value = new AttributeValue("wef")
            };

            Dictionary<string, AttributeValue> result1 = t1.updateItemAsync(new UpdateItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Key = key0,
                AttributeUpdates = updates,
                ReturnValues = ReturnValue.ALL_NEW,

            }).Result.Attributes;
            assertItemLocked(INTEG_HASH_TABLE_NAME, key0, item1, t1.Id, false, true);
            assertEquals(result1, item1);

            t1.commitAsync().Wait();

            assertItemNotLocked(INTEG_HASH_TABLE_NAME, key0, item1, true);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void deleteItemAllOldNotExists()
        public virtual void deleteItemAllOldNotExists()
        {
            Transaction t1 = manager.newTransaction();
            Dictionary<string, AttributeValue> key1 = newKey(INTEG_HASH_TABLE_NAME);

            Dictionary<string, AttributeValue> result1 = t1.deleteItemAsync(new DeleteItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Key = key1,
                ReturnValues = ReturnValue.ALL_OLD,

            }).Result.Attributes;
            assertItemLocked(INTEG_HASH_TABLE_NAME, key1, key1, t1.Id, true, false);
            assertNull(result1);

            t1.commitAsync().Wait();

            assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, false);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void deleteItemAllOldExists()
        public virtual void deleteItemAllOldExists()
        {
            Transaction t1 = manager.newTransaction();

            Dictionary<string, AttributeValue> result1 = t1.deleteItemAsync(new DeleteItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Key = key0,
                ReturnValues = ReturnValue.ALL_OLD,

            }).Result.Attributes;
            assertItemLocked(INTEG_HASH_TABLE_NAME, key0, item0, t1.Id, false, false);
            assertEquals(item0, result1);

            t1.commitAsync().Wait();

            assertItemNotLocked(INTEG_HASH_TABLE_NAME, key0, false);
        }

        /*
		 * Transaction isolation and error tests
		 */

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void conflictingWrites()
        public virtual void conflictingWrites()
        {
            Dictionary<string, AttributeValue> key1 = newKey(INTEG_HASH_TABLE_NAME);
            Transaction t1 = manager.newTransaction();
            Transaction t2 = manager.newTransaction();
            Transaction t3 = manager.newTransaction();

            // Finish t1 
            Dictionary<string, AttributeValue> t1Item = new Dictionary<string, AttributeValue>(key1);
            t1Item["whoami"] = new AttributeValue("t1");

            t1.putItemAsync(new PutItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Item = new Dictionary<string, AttributeValue>(t1Item)
            }).Wait();
            assertItemLocked(INTEG_HASH_TABLE_NAME, key1, t1Item, t1.Id, true, true);

            t1.commitAsync().Wait();
            assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, t1Item, true);

            // Begin t2
            Dictionary<string, AttributeValue> t2Item = new Dictionary<string, AttributeValue>(key1);
            t2Item["whoami"] = new AttributeValue("t2");
            t2Item["t2stuff"] = new AttributeValue("extra");

            t2.putItemAsync(new PutItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Item = new Dictionary<string, AttributeValue>(t2Item)
            }).Wait();
            assertItemLocked(INTEG_HASH_TABLE_NAME, key1, t2Item, t2.Id, false, true);

            // Begin and finish t3
            Dictionary<string, AttributeValue> t3Item = new Dictionary<string, AttributeValue>(key1);
            t3Item["whoami"] = new AttributeValue("t3");
            t3Item["t3stuff"] = new AttributeValue("things");

            t3.putItemAsync(new PutItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Item = new Dictionary<string, AttributeValue>(t3Item)
            }).Wait();
            assertItemLocked(INTEG_HASH_TABLE_NAME, key1, t3Item, t3.Id, false, true);

            t3.commitAsync().Wait();

            assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, t3Item, true);

            // Ensure t2 rolled back
            try
            {
                t2.commitAsync().Wait();
                fail();
            }
            catch (TransactionRolledBackException)
            {
            }

            t1.deleteAsync(long.MaxValue).Wait();
            t2.deleteAsync(long.MaxValue).Wait();
            t3.deleteAsync(long.MaxValue).Wait();

            assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, t3Item, true);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void failValidationInApply()
        public virtual void failValidationInApply()
        {
            Dictionary<string, AttributeValue> key = newKey(INTEG_HASH_TABLE_NAME);
            Dictionary<string, AttributeValueUpdate> updates = new Dictionary<string, AttributeValueUpdate>();
            updates["FooAttribute"] = new AttributeValueUpdate
            {
                Action = AttributeAction.PUT,
                Value = new AttributeValue("Bar")
            };

            Transaction t1 = manager.newTransaction();
            Transaction t2 = manager.newTransaction();

            t1.updateItemAsync(new UpdateItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Key = key,
                AttributeUpdates = updates,

            }).Wait();

            assertItemLocked(INTEG_HASH_TABLE_NAME, key, t1.Id, true, true);

            t1.commitAsync().Wait();

            assertItemNotLocked(INTEG_HASH_TABLE_NAME, key, true);

            updates["FooAttribute"] = new AttributeValueUpdate
            {
                Action = AttributeAction.ADD,
                Value = new AttributeValue { N = "1" }
            };

            try
            {
                t2.updateItemAsync(new UpdateItemRequest
                {
                    TableName = INTEG_HASH_TABLE_NAME,
                    Key = key,
                    AttributeUpdates = updates,

                });
                fail();
            }
            catch (AmazonServiceException e)
            {
                assertEquals("ValidationException", e.ErrorCode);
                assertTrue(e.Message.Contains("Type mismatch for attribute"));
            }

            assertItemLocked(INTEG_HASH_TABLE_NAME, key, t2.Id, false, false);

            try
            {
                t2.commitAsync().Wait();
                fail();
            }
            catch (AmazonServiceException e)
            {
                assertEquals("ValidationException", e.ErrorCode);
                assertTrue(e.Message.Contains("Type mismatch for attribute"));
            }

            assertItemLocked(INTEG_HASH_TABLE_NAME, key, t2.Id, false, false);

            t2.rollbackAsync().Wait();

            assertItemNotLocked(INTEG_HASH_TABLE_NAME, key, true);

            t1.deleteAsync(long.MaxValue).Wait();
            t2.deleteAsync(long.MaxValue).Wait();
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void useCommittedTransaction()
        public virtual void useCommittedTransaction()
        {
            Dictionary<string, AttributeValue> key1 = newKey(INTEG_HASH_TABLE_NAME);
            Transaction t1 = manager.newTransaction();
            t1.commitAsync().Wait();

            DeleteItemRequest deleteRequest = new DeleteItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Key = key1,

            };

            try
            {
                t1.deleteItemAsync(deleteRequest).Wait();
                fail();
            }
            catch (TransactionCommittedException)
            {
            }

            assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, false);

            Transaction t2 = manager.resumeTransaction(t1.Id);

            try
            {
                t1.deleteItemAsync(deleteRequest).Wait();
                fail();
            }
            catch (TransactionCommittedException)
            {
            }

            assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, false);

            try
            {
                t2.rollbackAsync().Wait();
                fail();
            }
            catch (TransactionCommittedException)
            {
            }

            t2.deleteAsync(long.MaxValue).Wait();
            t1.deleteAsync(long.MaxValue).Wait();
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void useRolledBackTransaction()
        public virtual void useRolledBackTransaction()
        {
            Dictionary<string, AttributeValue> key1 = newKey(INTEG_HASH_TABLE_NAME);
            Transaction t1 = manager.newTransaction();
            t1.rollbackAsync().Wait();

            DeleteItemRequest deleteRequest = new DeleteItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Key = key1,

            };

            try
            {
                t1.deleteItemAsync(deleteRequest).Wait();
                fail();
            }
            catch (TransactionRolledBackException)
            {
            }

            assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, false);

            Transaction t2 = manager.resumeTransaction(t1.Id);

            try
            {
                t1.deleteItemAsync(deleteRequest).Wait();
                fail();
            }
            catch (TransactionRolledBackException)
            {
            }

            assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, false);

            try
            {
                t2.commitAsync().Wait();
                fail();
            }
            catch (TransactionRolledBackException)
            {
            }

            assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, false);

            Transaction t3 = manager.resumeTransaction(t1.Id);
            t3.rollbackAsync().Wait();

            Transaction t4 = manager.resumeTransaction(t1.Id);

            t2.deleteAsync(long.MaxValue).Wait();
            t1.deleteAsync(long.MaxValue).Wait();

            try
            {
                t4.deleteItemAsync(deleteRequest).Wait();
                fail();
            }
            catch (TransactionNotFoundException)
            {
            }

            assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, false);

            t3.deleteAsync(long.MaxValue).Wait();
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void useDeletedTransaction()
        public virtual void useDeletedTransaction()
        {
            Transaction t1 = manager.newTransaction();
            Transaction t2 = manager.resumeTransaction(t1.Id);
            t1.commitAsync().Wait();
            t1.deleteAsync(long.MaxValue).Wait();

            try
            {
                t2.commitAsync().Wait();
                fail();
            }
            catch (UnknownCompletedTransactionException)
            {
            }

            t2.deleteAsync(long.MaxValue).Wait();

        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void driveCommit()
        public virtual void driveCommit()
        {
            Transaction t1 = manager.newTransaction();
            Dictionary<string, AttributeValue> key1 = newKey(INTEG_HASH_TABLE_NAME);
            Dictionary<string, AttributeValue> key2 = newKey(INTEG_HASH_TABLE_NAME);
            Dictionary<string, AttributeValue> item = new Dictionary<string, AttributeValue>(key1);
            item["attr"] = new AttributeValue("original");

            t1.putItemAsync(new PutItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Item = item,

            }).Wait();

            t1.commitAsync().Wait();
            t1.deleteAsync(long.MaxValue).Wait();

            assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, item, true);
            assertItemNotLocked(INTEG_HASH_TABLE_NAME, key2, false);

            Transaction t2 = manager.newTransaction();

            item["attr2"] = new AttributeValue("new");
            t2.putItemAsync(new PutItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Item = item,

            }).Wait();

            t2.getItemAsync(new GetItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Key = key2,

            }).Wait();

            assertItemLocked(INTEG_HASH_TABLE_NAME, key1, item, t2.Id, false, true);
            assertItemLocked(INTEG_HASH_TABLE_NAME, key2, key2, t2.Id, true, false);

            Transaction commitFailingTransaction = new TransactionAnonymousInnerClass1(this, t2.Id, manager);

            try
            {
                commitFailingTransaction.commitAsync().Wait();
                fail();
            }
            catch (FailingAmazonDynamoDBClient.FailedYourRequestException)
            {
            }

            assertItemLocked(INTEG_HASH_TABLE_NAME, key1, item, t2.Id, false, true);
            assertItemLocked(INTEG_HASH_TABLE_NAME, key2, key2, t2.Id, true, false);

            t2.commitAsync().Wait();

            assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, item, true);
            assertItemNotLocked(INTEG_HASH_TABLE_NAME, key2, false);

            commitFailingTransaction.commitAsync().Wait();

            t2.deleteAsync(long.MaxValue).Wait();
        }

        private class TransactionAnonymousInnerClass1 : Transaction
        {
            private readonly TransactionsIntegrationTest outerInstance;

            public TransactionAnonymousInnerClass1(TransactionsIntegrationTest outerInstance, string getId, TransactionManager manager) : base(getId, manager, false)
            {
                this.outerInstance = outerInstance;
            }

            protected internal override Task unlockItemAfterCommitAsync(Request request)
            {
                throw new FailingAmazonDynamoDBClient.FailedYourRequestException();
            }
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void driveRollback()
        public virtual void driveRollback()
        {
            Transaction t1 = manager.newTransaction();
            Dictionary<string, AttributeValue> key1 = newKey(INTEG_HASH_TABLE_NAME);
            Dictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue>(key1);
            item1["attr1"] = new AttributeValue("original1");

            Dictionary<string, AttributeValue> key2 = newKey(INTEG_HASH_TABLE_NAME);
            Dictionary<string, AttributeValue> item2 = new Dictionary<string, AttributeValue>(key2);
            item1["attr2"] = new AttributeValue("original2");

            Dictionary<string, AttributeValue> key3 = newKey(INTEG_HASH_TABLE_NAME);

            t1.putItemAsync(new PutItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Item = item1,
            }).Wait();

            t1.putItemAsync(new PutItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Item = item2,
            }).Wait();

            t1.commitAsync().Wait();
            t1.deleteAsync(long.MaxValue).Wait();

            assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, item1, true);
            assertItemNotLocked(INTEG_HASH_TABLE_NAME, key2, item2, true);

            Transaction t2 = manager.newTransaction();

            Dictionary<string, AttributeValue> item1a = new Dictionary<string, AttributeValue>(item1);
            item1a["attr1"] = new AttributeValue("new1");
            item1a["attr2"] = new AttributeValue("new1");

            t2.putItemAsync(new PutItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Item = item1a,

            }).Wait();

            t2.getItemAsync(new GetItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Key = key2,

            }).Wait();

            t2.getItemAsync(new GetItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Key = key3,

            }).Wait();

            assertItemLocked(INTEG_HASH_TABLE_NAME, key1, item1a, t2.Id, false, true);
            assertItemLocked(INTEG_HASH_TABLE_NAME, key2, item2, t2.Id, false, false);
            assertItemLocked(INTEG_HASH_TABLE_NAME, key3, key3, t2.Id, true, false);

            Transaction rollbackFailingTransaction = new TransactionAnonymousInnerClass22(this, t2.Id, manager);

            try
            {
                rollbackFailingTransaction.rollbackAsync().Wait();
                fail();
            }
            catch (FailingAmazonDynamoDBClient.FailedYourRequestException)
            {
            }

            assertItemLocked(INTEG_HASH_TABLE_NAME, key1, item1a, t2.Id, false, true);
            assertItemLocked(INTEG_HASH_TABLE_NAME, key2, item2, t2.Id, false, false);
            assertItemLocked(INTEG_HASH_TABLE_NAME, key3, key3, t2.Id, true, false);

            t2.rollbackAsync().Wait();

            assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, item1, true);
            assertItemNotLocked(INTEG_HASH_TABLE_NAME, key2, item2, true);
            assertItemNotLocked(INTEG_HASH_TABLE_NAME, key3, false);

            rollbackFailingTransaction.rollbackAsync().Wait();

            t2.deleteAsync(long.MaxValue).Wait();
        }

        private class TransactionAnonymousInnerClass22 : Transaction
        {
            private readonly TransactionsIntegrationTest outerInstance;

            public TransactionAnonymousInnerClass22(TransactionsIntegrationTest outerInstance, string getId, TransactionManager manager) : base(getId, manager, false)
            {
                this.outerInstance = outerInstance;
            }

            protected internal override Task rollbackItemAndReleaseLockAsync(Request request)
            {
                throw new FailingAmazonDynamoDBClient.FailedYourRequestException();
            }
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void rollbackCompletedTransaction()
        public virtual void rollbackCompletedTransaction()
        {
            Transaction t1 = manager.newTransaction();
            Transaction rollbackFailingTransaction = new TransactionAnonymousInnerClass33(this, t1.Id, manager);

            Dictionary<string, AttributeValue> key1 = newKey(INTEG_HASH_TABLE_NAME);
            t1.putItemAsync(new PutItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Item = key1,

            }).Wait();
            assertItemLocked(INTEG_HASH_TABLE_NAME, key1, key1, t1.Id, true, true);

            t1.rollbackAsync().Wait();
            rollbackFailingTransaction.rollbackAsync().Wait();
        }

        private class TransactionAnonymousInnerClass33 : Transaction
        {
            private readonly TransactionsIntegrationTest outerInstance;

            public TransactionAnonymousInnerClass33(TransactionsIntegrationTest outerInstance, string getId, TransactionManager manager) : base(getId, manager, false)
            {
                this.outerInstance = outerInstance;
            }

            protected internal override Task doRollbackAsync()
            {
                throw new FailingAmazonDynamoDBClient.FailedYourRequestException();
            }
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void commitCompletedTransaction()
        public virtual void commitCompletedTransaction()
        {
            Transaction t1 = manager.newTransaction();
            Transaction commitFailingTransaction = new TransactionAnonymousInnerClass44(this, t1.Id, manager);

            Dictionary<string, AttributeValue> key1 = newKey(INTEG_HASH_TABLE_NAME);
            t1.putItemAsync(new PutItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Item = key1,

            }).Wait();
            assertItemLocked(INTEG_HASH_TABLE_NAME, key1, key1, t1.Id, true, true);

            t1.commitAsync().Wait();
            commitFailingTransaction.commitAsync().Wait();
        }

        private class TransactionAnonymousInnerClass44 : Transaction
        {
            private readonly TransactionsIntegrationTest outerInstance;

            public TransactionAnonymousInnerClass44(TransactionsIntegrationTest outerInstance, string getId, TransactionManager manager) : base(getId, manager, false)
            {
                this.outerInstance = outerInstance;
            }

            protected internal override Task doCommitAsync()
            {
                throw new FailingAmazonDynamoDBClient.FailedYourRequestException();
            }
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void resumePendingTransaction()
        public virtual void resumePendingTransaction()
        {
            Transaction t1 = manager.newTransaction();

            Dictionary<string, AttributeValue> key1 = newKey(INTEG_HASH_TABLE_NAME);
            Dictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue>(key1);
            item1["attr1"] = new AttributeValue("original1");

            Dictionary<string, AttributeValue> key2 = newKey(INTEG_HASH_TABLE_NAME);

            t1.putItemAsync(new PutItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Item = item1,

            }).Wait();

            assertItemLocked(INTEG_HASH_TABLE_NAME, key1, item1, t1.Id, true, true);
            assertOldItemImage(t1.Id, INTEG_HASH_TABLE_NAME, key1, key1, false);

            Transaction t2 = manager.resumeTransaction(t1.Id);

            t2.getItemAsync(new GetItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Key = key2,

            }).Wait();

            assertItemLocked(INTEG_HASH_TABLE_NAME, key1, item1, t1.Id, true, true);
            assertOldItemImage(t1.Id, INTEG_HASH_TABLE_NAME, key1, key1, false);
            assertItemLocked(INTEG_HASH_TABLE_NAME, key2, t1.Id, true, false);
            assertOldItemImage(t1.Id, INTEG_HASH_TABLE_NAME, key2, null, false);

            t2.commitAsync().Wait();

            assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, true);
            assertItemNotLocked(INTEG_HASH_TABLE_NAME, key2, false);

            assertOldItemImage(t1.Id, INTEG_HASH_TABLE_NAME, key1, null, false);
            assertOldItemImage(t1.Id, INTEG_HASH_TABLE_NAME, key2, null, false);

            t2.deleteAsync(long.MaxValue).Wait();
            assertTransactionDeleted(t2);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void resumeAndCommitAfterTransientApplyFailure()
        public virtual void resumeAndCommitAfterTransientApplyFailure()
        {
            Transaction t1 = new TransactionAnonymousInnerClass5(this, Guid.NewGuid().ToString(), manager);

            Dictionary<string, AttributeValue> key1 = newKey(INTEG_HASH_TABLE_NAME);
            Dictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue>(key1);
            item1["attr1"] = new AttributeValue("original1");

            Dictionary<string, AttributeValue> key2 = newKey(INTEG_HASH_TABLE_NAME);

            try
            {
                t1.putItemAsync(new PutItemRequest
                {
                    TableName = INTEG_HASH_TABLE_NAME,
                    Item = item1,

                }).Wait();
                fail();
            }
            catch (FailingAmazonDynamoDBClient.FailedYourRequestException)
            {
            }

            assertItemLocked(INTEG_HASH_TABLE_NAME, key1, key1, t1.Id, true, false);
            assertOldItemImage(t1.Id, INTEG_HASH_TABLE_NAME, key1, key1, false);

            Transaction t2 = manager.resumeTransaction(t1.Id);

            t2.getItemAsync(new GetItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Key = key2,

            }).Wait();

            assertItemLocked(INTEG_HASH_TABLE_NAME, key1, item1, t1.Id, true, true);
            assertOldItemImage(t1.Id, INTEG_HASH_TABLE_NAME, key1, key1, false);
            assertItemLocked(INTEG_HASH_TABLE_NAME, key2, t1.Id, true, false);
            assertOldItemImage(t1.Id, INTEG_HASH_TABLE_NAME, key2, null, false);

            Transaction t3 = manager.resumeTransaction(t1.Id);
            t3.commitAsync().Wait();

            assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, item1, true);
            assertItemNotLocked(INTEG_HASH_TABLE_NAME, key2, false);

            assertOldItemImage(t1.Id, INTEG_HASH_TABLE_NAME, key1, null, false);
            assertOldItemImage(t1.Id, INTEG_HASH_TABLE_NAME, key2, null, false);

            t3.commitAsync().Wait();

            t3.deleteAsync(long.MaxValue).Wait();
            assertTransactionDeleted(t2);
        }

        private class TransactionAnonymousInnerClass5 : Transaction
        {
            private readonly TransactionsIntegrationTest outerInstance;

            public TransactionAnonymousInnerClass5(TransactionsIntegrationTest outerInstance, string toString, TransactionManager manager) : base(toString, manager, true)
            {
                this.outerInstance = outerInstance;
            }

            protected internal override Task<Dictionary<string, AttributeValue>> applyAndKeepLockAsync(Request request, Dictionary<string, AttributeValue> lockedItem)
            {
                throw new FailingAmazonDynamoDBClient.FailedYourRequestException();
            }
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void applyOnlyOnce()
        public virtual void applyOnlyOnce()
        {
            Transaction t1 = new TransactionAnonymousInnerClass6(this, Guid.NewGuid().ToString(), manager);

            Dictionary<string, AttributeValue> key1 = newKey(INTEG_HASH_TABLE_NAME);
            Dictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue>(key1);
            item1["attr1"] = (new AttributeValue()).withN("1");

            Dictionary<string, AttributeValue> key2 = newKey(INTEG_HASH_TABLE_NAME);

            Dictionary<string, AttributeValueUpdate> updates = new Dictionary<string, AttributeValueUpdate>();
            updates["attr1"] = new AttributeValueUpdate
            {
                Action = AttributeAction.ADD,
                Value = (new AttributeValue()).withN("1")
            };

            UpdateItemRequest update = new UpdateItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                AttributeUpdates = updates,
                Key = key1,

            };

            try
            {
                t1.updateItemAsync(update).Wait();
                fail();
            }
            catch (FailingAmazonDynamoDBClient.FailedYourRequestException)
            {
            }

            assertItemLocked(INTEG_HASH_TABLE_NAME, key1, item1, t1.Id, true, true);
            assertOldItemImage(t1.Id, INTEG_HASH_TABLE_NAME, key1, key1, false);

            Transaction t2 = manager.resumeTransaction(t1.Id);

            t2.getItemAsync(new GetItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Key = key2,

            }).Wait();

            assertItemLocked(INTEG_HASH_TABLE_NAME, key1, item1, t1.Id, true, true);
            assertOldItemImage(t1.Id, INTEG_HASH_TABLE_NAME, key1, key1, false);
            assertItemLocked(INTEG_HASH_TABLE_NAME, key2, t1.Id, true, false);
            assertOldItemImage(t1.Id, INTEG_HASH_TABLE_NAME, key2, null, false);

            t2.commitAsync().Wait();

            assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, item1, true);
            assertItemNotLocked(INTEG_HASH_TABLE_NAME, key2, false);

            assertOldItemImage(t1.Id, INTEG_HASH_TABLE_NAME, key1, null, false);
            assertOldItemImage(t1.Id, INTEG_HASH_TABLE_NAME, key2, null, false);

            t2.deleteAsync(long.MaxValue).Wait();
            assertTransactionDeleted(t2);
        }

        private class TransactionAnonymousInnerClass6 : Transaction
        {
            private readonly TransactionsIntegrationTest outerInstance;

            public TransactionAnonymousInnerClass6(TransactionsIntegrationTest outerInstance, string toString, TransactionManager manager) : base(toString, manager, true)
            {
                this.outerInstance = outerInstance;
            }

            protected internal override async Task<Dictionary<string, AttributeValue>> applyAndKeepLockAsync(Request request, Dictionary<string, AttributeValue> lockedItem)
            {
                await base.applyAndKeepLockAsync(request, lockedItem);
                throw new FailingAmazonDynamoDBClient.FailedYourRequestException();
            }
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void resumeRollbackAfterTransientApplyFailure()
        public virtual void resumeRollbackAfterTransientApplyFailure()
        {
            Transaction t1 = new TransactionAnonymousInnerClass7(this, Guid.NewGuid().ToString(), manager);

            Dictionary<string, AttributeValue> key1 = newKey(INTEG_HASH_TABLE_NAME);
            Dictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue>(key1);
            item1["attr1"] = new AttributeValue("original1");

            Dictionary<string, AttributeValue> key2 = newKey(INTEG_HASH_TABLE_NAME);

            try
            {
                t1.putItemAsync(new PutItemRequest
                {
                    TableName = INTEG_HASH_TABLE_NAME,
                    Item = item1,

                }).Wait();
                fail();
            }
            catch (FailingAmazonDynamoDBClient.FailedYourRequestException)
            {
            }

            assertItemLocked(INTEG_HASH_TABLE_NAME, key1, key1, t1.Id, true, false);
            assertOldItemImage(t1.Id, INTEG_HASH_TABLE_NAME, key1, key1, false);

            Transaction t2 = manager.resumeTransaction(t1.Id);

            t2.getItemAsync(new GetItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Key = key2,

            });

            assertItemLocked(INTEG_HASH_TABLE_NAME, key1, item1, t1.Id, true, true);
            assertOldItemImage(t1.Id, INTEG_HASH_TABLE_NAME, key1, key1, false);
            assertItemLocked(INTEG_HASH_TABLE_NAME, key2, t1.Id, true, false);
            assertOldItemImage(t1.Id, INTEG_HASH_TABLE_NAME, key2, null, false);

            Transaction t3 = manager.resumeTransaction(t1.Id);
            t3.rollbackAsync().Wait();

            assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, false);
            assertItemNotLocked(INTEG_HASH_TABLE_NAME, key2, false);

            assertOldItemImage(t1.Id, INTEG_HASH_TABLE_NAME, key1, null, false);
            assertOldItemImage(t1.Id, INTEG_HASH_TABLE_NAME, key2, null, false);

            t3.deleteAsync(long.MaxValue).Wait();
            assertTransactionDeleted(t2);
        }

        private class TransactionAnonymousInnerClass7 : Transaction
        {
            private readonly TransactionsIntegrationTest outerInstance;

            public TransactionAnonymousInnerClass7(TransactionsIntegrationTest outerInstance, string toString, TransactionManager manager) : base(toString, manager, true)
            {
                this.outerInstance = outerInstance;
            }

            protected internal override Task<Dictionary<string, AttributeValue>> applyAndKeepLockAsync(Request request, Dictionary<string, AttributeValue> lockedItem)
            {
                throw new FailingAmazonDynamoDBClient.FailedYourRequestException();
            }
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void unlockInRollbackIfNoItemImageSaved() throws InterruptedException
        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        public virtual void unlockInRollbackIfNoItemImageSaved()
        {
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final Transaction t1 = new Transaction(java.util.Guid.NewGuid().toString(), manager, true)
            Transaction t1 = new TransactionAnonymousInnerClass8(this, Guid.NewGuid().ToString(), manager);

            // Change the existing item key0, failing when trying to saveAsync away the item image
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> item0a = new java.util.HashMap<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> (item0);
            Dictionary<string, AttributeValue> item0a = new Dictionary<string, AttributeValue>(item0);
            item0a["attr1"] = new AttributeValue("original1");

            try
            {
                t1.putItemAsync(new PutItemRequest
                {
                    TableName = INTEG_HASH_TABLE_NAME,
                    Item = item0a,

                }).Wait();
                fail();
            }
            catch (FailingAmazonDynamoDBClient.FailedYourRequestException)
            {
            }

            assertItemLocked(INTEG_HASH_TABLE_NAME, key0, item0, t1.Id, false, false);

            // Roll back, and ensure the item was reverted correctly
            t1.rollbackAsync().Wait();

            assertItemNotLocked(INTEG_HASH_TABLE_NAME, key0, item0, true);
        }

        private class TransactionAnonymousInnerClass8 : Transaction
        {
            private readonly TransactionsIntegrationTest outerInstance;

            public TransactionAnonymousInnerClass8(TransactionsIntegrationTest outerInstance, string toString, TransactionManager manager) : base(toString, manager, true)
            {
                this.outerInstance = outerInstance;
            }

            protected internal override void saveItemImage(Request callerRequest, Dictionary<string, AttributeValue> item)
            {
                throw new FailingAmazonDynamoDBClient.FailedYourRequestException();
            }
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void shouldNotApplyAfterRollback() throws InterruptedException
        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        public virtual void shouldNotApplyAfterRollback()
        {
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final java.util.concurrent.SemaphoreSlim barrier = new java.util.concurrent.SemaphoreSlim(0);
            SemaphoreSlim barrier = new SemaphoreSlim(0);
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final Transaction t1 = new Transaction(java.util.Guid.NewGuid().toString(), manager, true)
            Transaction t1 = new TransactionAnonymousInnerClass9(this, Guid.NewGuid().ToString(), manager, barrier);

            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> key1 = newKey(INTEG_HASH_TABLE_NAME);
            Dictionary<string, AttributeValue> key1 = newKey(INTEG_HASH_TABLE_NAME);
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
                t1.putItemAsync(new PutItemRequest
                {
                    TableName = INTEG_HASH_TABLE_NAME,
                    Item = item1,

                }).Wait();
            }
            catch (TransactionRolledBackException)
            {
                caughtRolledBackException.Release();
            }
        });

            thread.Start();

            assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, false);
            Transaction t2 = manager.resumeTransaction(t1.Id);
            t2.rollbackAsync().Wait();
            assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, false);

            barrier.Release(100);

            thread.Join();

            assertEquals(1, caughtRolledBackException.CurrentCount);

            assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, false);
            assertTrue(t1.deleteAsync(long.MinValue).Result);

            // Now start a new transaction involving key1 and make sure it will complete
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> item1a = new java.util.HashMap<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> (key1);
            Dictionary<string, AttributeValue> item1a = new Dictionary<string, AttributeValue>(key1);
            item1a["attr1"] = new AttributeValue("new");

            Transaction t3 = manager.newTransaction();
            t3.putItemAsync(new PutItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Item = item1a,

            });
            assertItemLocked(INTEG_HASH_TABLE_NAME, key1, item1a, t3.Id, true, true);
            t3.commitAsync().Wait();
            assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, item1a, true);
        }

        private class TransactionAnonymousInnerClass9 : Transaction
        {
            private readonly TransactionsIntegrationTest outerInstance;

            private SemaphoreSlim barrier;

            public TransactionAnonymousInnerClass9(TransactionsIntegrationTest outerInstance, string toString, TransactionManager manager, SemaphoreSlim barrier) : base(toString, manager, true)
            {
                this.outerInstance = outerInstance;
                this.barrier = barrier;
            }

            //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
            //ORIGINAL LINE: @Override protected java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> lockItem(Request callerRequest, boolean expectExists, int attempts) throws com.amazonaws.services.dynamodbv2.transactions.exceptions.TransactionException
            protected internal override async Task<Dictionary<string, AttributeValue>> lockItemAsync(Request callerRequest, bool expectExists, int attempts)
            {
                //try
                //{
                barrier.Wait();
                //}
                //catch (InterruptedException e)
                //{
                //    throw new Exception(e);
                //}
                return await base.lockItemAsync(callerRequest, expectExists, attempts);
            }
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void shouldNotApplyAfterRollbackAndDeleted() throws InterruptedException
        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        public virtual void shouldNotApplyAfterRollbackAndDeleted()
        {
            // Very similar to "shouldNotApplyAfterRollback" except the transaction is rolled back and then deleted.
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final java.util.concurrent.SemaphoreSlim barrier = new java.util.concurrent.SemaphoreSlim(0);
            SemaphoreSlim barrier = new SemaphoreSlim(0);
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final Transaction t1 = new Transaction(java.util.Guid.NewGuid().toString(), manager, true)
            Transaction t1 = new TransactionAnonymousInnerClass10(this, Guid.NewGuid().ToString(), manager, barrier);

            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> key1 = newKey(INTEG_HASH_TABLE_NAME);
            Dictionary<string, AttributeValue> key1 = newKey(INTEG_HASH_TABLE_NAME);
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
                t1.putItemAsync(new PutItemRequest
                {
                    TableName = INTEG_HASH_TABLE_NAME,
                    Item = item1,

                }).Wait();
            }
            catch (TransactionNotFoundException)
            {
                caughtTransactionNotFoundException.Release();
            }
        });

            thread.Start();

            assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, false);
            Transaction t2 = manager.resumeTransaction(t1.Id);
            t2.rollbackAsync().Wait();
            assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, false);
            assertTrue(t2.deleteAsync(long.MinValue).Result); // This is the key difference with shouldNotApplyAfterRollback

            barrier.Release(100);

            thread.Join();

            assertEquals(1, caughtTransactionNotFoundException.CurrentCount);

            assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, false);

            // Now start a new transaction involving key1 and make sure it will complete
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> item1a = new java.util.HashMap<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> (key1);
            Dictionary<string, AttributeValue> item1a = new Dictionary<string, AttributeValue>(key1);
            item1a["attr1"] = new AttributeValue("new");

            Transaction t3 = manager.newTransaction();
            t3.putItemAsync(new PutItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Item = item1a,

            });
            assertItemLocked(INTEG_HASH_TABLE_NAME, key1, item1a, t3.Id, true, true);
            t3.commitAsync().Wait();
            assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, item1a, true);
        }

        private class TransactionAnonymousInnerClass10 : Transaction
        {
            private readonly TransactionsIntegrationTest outerInstance;

            private SemaphoreSlim barrier;

            public TransactionAnonymousInnerClass10(TransactionsIntegrationTest outerInstance, string toString, TransactionManager manager, SemaphoreSlim barrier) : base(toString, manager, true)
            {
                this.outerInstance = outerInstance;
                this.barrier = barrier;
            }

            //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
            //ORIGINAL LINE: @Override protected java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> lockItem(Request callerRequest, boolean expectExists, int attempts) throws com.amazonaws.services.dynamodbv2.transactions.exceptions.TransactionException
            protected internal override async Task<Dictionary<string, AttributeValue>> lockItemAsync(Request callerRequest, bool expectExists, int attempts)
            {
                //try
                //{
                barrier.Wait();
                //}
                //catch (InterruptedException e)
                //{
                //    throw new Exception(e);
                //}
                return await base.lockItemAsync(callerRequest, expectExists, attempts);
            }
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void shouldNotApplyAfterRollbackAndDeletedAndLeftLocked() throws InterruptedException
        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        public virtual void shouldNotApplyAfterRollbackAndDeletedAndLeftLocked()
        {

            // Very similar to "shouldNotApplyAfterRollbackAndDeleted" except the lock is broken by a new transaction, not t1
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final java.util.concurrent.SemaphoreSlim barrier = new java.util.concurrent.SemaphoreSlim(0);
            SemaphoreSlim barrier = new SemaphoreSlim(0);
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final Transaction t1 = new Transaction(java.util.Guid.NewGuid().toString(), manager, true)
            Transaction t1 = new TransactionAnonymousInnerClass11(this, Guid.NewGuid().ToString(), manager, barrier);

            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> key1 = newKey(INTEG_HASH_TABLE_NAME);
            Dictionary<string, AttributeValue> key1 = newKey(INTEG_HASH_TABLE_NAME);
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
                t1.putItemAsync(new PutItemRequest
                {
                    TableName = INTEG_HASH_TABLE_NAME,
                    Item = item1,

                }).Wait();
            }
            catch (FailingAmazonDynamoDBClient.FailedYourRequestException)
            {
                caughtFailedYourRequestException.Release();
            }
        });

            thread.Start();

            assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, false);
            Transaction t2 = manager.resumeTransaction(t1.Id);
            t2.rollbackAsync().Wait();
            assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, false);
            assertTrue(t2.deleteAsync(long.MinValue).Result);

            barrier.Release(100);

            thread.Join();

            assertEquals(1, caughtFailedYourRequestException.CurrentCount);

            assertItemLocked(INTEG_HASH_TABLE_NAME, key1, null, t1.Id, true, false, false); // locked and "null", but don't check the tx item

            // Now start a new transaction involving key1 and make sure it will complete
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> item1a = new java.util.HashMap<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> (key1);
            Dictionary<string, AttributeValue> item1a = new Dictionary<string, AttributeValue>(key1);
            item1a["attr1"] = new AttributeValue("new");

            Transaction t3 = manager.newTransaction();
            t3.putItemAsync(new PutItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Item = item1a,

            }).Wait();
            assertItemLocked(INTEG_HASH_TABLE_NAME, key1, item1a, t3.Id, true, true);
            t3.commitAsync().Wait();
            assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, item1a, true);
        }

        private class TransactionAnonymousInnerClass11 : Transaction
        {
            private readonly TransactionsIntegrationTest outerInstance;

            private SemaphoreSlim barrier;

            public TransactionAnonymousInnerClass11(TransactionsIntegrationTest outerInstance, string toString, TransactionManager manager, SemaphoreSlim barrier) : base(toString, manager, true)
            {
                this.outerInstance = outerInstance;
                this.barrier = barrier;
            }

            //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
            //ORIGINAL LINE: @Override protected java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> lockItem(Request callerRequest, boolean expectExists, int attempts) throws com.amazonaws.services.dynamodbv2.transactions.exceptions.TransactionException
            protected internal override async Task<Dictionary<string, AttributeValue>> lockItemAsync(Request callerRequest, bool expectExists, int attempts)
            {
                //try
                //{
                barrier.Wait();
                //}
                //catch (InterruptedException e)
                //{
                //    throw new Exception(e);
                //}
                return await base.lockItemAsync(callerRequest, expectExists, attempts);
            }

            protected internal override Task releaseReadLockAsync(string tableName, Dictionary<string, AttributeValue> key)
            {
                throw new FailingAmazonDynamoDBClient.FailedYourRequestException();
            }
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void rollbackAfterReadLockUpgradeAttempt() throws InterruptedException
        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        public virtual void rollbackAfterReadLockUpgradeAttempt()
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
            TransactionManager manager = new TransactionManagerAnonymousInnerClass(this, dynamodb, INTEG_LOCK_TABLE_NAME, INTEG_IMAGES_TABLE_NAME, waitAfterResumeTransaction, resumedTransaction);

            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final Transaction t1 = new Transaction(java.util.Guid.NewGuid().toString(), manager, true)
            Transaction t1 = new TransactionAnonymousInnerClass12(this, Guid.NewGuid().ToString(), manager, shouldThrowAfterApply);

            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> key1 = newKey(INTEG_HASH_TABLE_NAME);
            Dictionary<string, AttributeValue> key1 = newKey(INTEG_HASH_TABLE_NAME);
            //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
            //ORIGINAL LINE: final java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> key2 = newKey(INTEG_HASH_TABLE_NAME);
            Dictionary<string, AttributeValue> key2 = newKey(INTEG_HASH_TABLE_NAME);

            // Read an item that doesn't exist to get its read lock
            Dictionary<string, AttributeValue> item1Returned = t1.getItemAsync(new GetItemRequest(INTEG_HASH_TABLE_NAME, key1, true)).Result.Item;
            assertNull(item1Returned);
            assertItemLocked(INTEG_HASH_TABLE_NAME, key1, t1.Id, true, false);

            Thread thread = new Thread(() =>
            {
                Transaction t2 = new TransactionAnonymousInnerClass13(this, Guid.NewGuid().ToString(), manager);
                // This will stop pause on waitAfterResumeTransaction once it finds that key1 is already locked by t1.
                Dictionary<string, AttributeValue> item1Returned2 = t2.getItemAsync(new GetItemRequest(INTEG_HASH_TABLE_NAME, key1, true)).Result.Item;
                assertNull(item1Returned2);
                rolledBackT1.Release();
            });
            thread.Start();

            // Wait for t2 to get to the point where it loaded the t1 tx record.
            resumedTransaction.Wait();

            // Now change that getItem to an updateItemAsync in t1
            Dictionary<string, AttributeValueUpdate> updates = new Dictionary<string, AttributeValueUpdate>();
            updates["asdf"] = new AttributeValueUpdate(new AttributeValue("wef"), AttributeAction.PUT);
            t1.updateItemAsync(new UpdateItemRequest(INTEG_HASH_TABLE_NAME, key1, updates)).Wait();

            // Now let t2 continue on and roll back t1
            waitAfterResumeTransaction.Release();

            // Wait for t2 to finish rolling back t1
            rolledBackT1.Wait();

            // T1 should be rolled back now and unable to do stuff
            try
            {
                t1.getItemAsync(new GetItemRequest(INTEG_HASH_TABLE_NAME, key2, true)).Wait();
                fail();
            }
            catch (TransactionRolledBackException)
            {
                // expected
            }
        }

        private class TransactionAnonymousInnerClass12 : Transaction
        {
            private readonly TransactionsIntegrationTest outerInstance;

            private AtomicBoolean shouldThrowAfterApply;

            public TransactionAnonymousInnerClass12(TransactionsIntegrationTest outerInstance, string toString, TransactionManager manager, AtomicBoolean shouldThrowAfterApply) : base(toString, manager, true)
            {
                this.outerInstance = outerInstance;
                this.shouldThrowAfterApply = shouldThrowAfterApply;
            }

            protected internal override async Task<Dictionary<string, AttributeValue>> applyAndKeepLockAsync(Request request, Dictionary<string, AttributeValue> lockedItem)
            {
                Dictionary<string, AttributeValue> toReturn = await base.applyAndKeepLockAsync(request, lockedItem);
                if (shouldThrowAfterApply.Value)
                {
                    throw new Exception("throwing as desired");
                }
                return toReturn;
            }
        }

        private class TransactionManagerAnonymousInnerClass : TransactionManager
        {
            private readonly TransactionsIntegrationTest outerInstance;

            private SemaphoreSlim waitAfterResumeTransaction;
            private SemaphoreSlim resumedTransaction;

            public TransactionManagerAnonymousInnerClass(TransactionsIntegrationTest outerInstance, AmazonDynamoDBClient dynamodb, string INTEG_LOCK_TABLE_NAME, string INTEG_IMAGES_TABLE_NAME, SemaphoreSlim waitAfterResumeTransaction, SemaphoreSlim resumedTransaction) : base(dynamodb, INTEG_LOCK_TABLE_NAME, INTEG_IMAGES_TABLE_NAME)
            {
                this.outerInstance = outerInstance;
                this.waitAfterResumeTransaction = waitAfterResumeTransaction;
                this.resumedTransaction = resumedTransaction;
            }

            public override Transaction resumeTransaction(string txId)
            {
                Transaction t = base.resumeTransaction(txId);

                // Signal to the main thread that t2 has loaded the tx record.
                resumedTransaction.Release();

                //try
                //{
                // Wait for the main thread to upgrade key1 to a write lock (but we won't know about it)
                waitAfterResumeTransaction.Wait();
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
            private readonly TransactionsIntegrationTest outerInstance;

            public TransactionAnonymousInnerClass13(TransactionsIntegrationTest outerInstance, string toString, TransactionManager manager) : base(toString, manager, true)
            {
                this.outerInstance = outerInstance;
            }

        }

        // TODO same as shouldNotLockAndApplyAfterRollbackAndDeleted except make t3 do the unlock, not t1.

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void basicNewItemRollback()
        public virtual void basicNewItemRollback()
        {
            Transaction t1 = manager.newTransaction();
            Dictionary<string, AttributeValue> key1 = newKey(INTEG_HASH_TABLE_NAME);

            t1.updateItemAsync(new UpdateItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Key = key1,

            }).Wait();
            assertItemLocked(INTEG_HASH_TABLE_NAME, key1, t1.Id, true, true);

            t1.rollbackAsync().Wait();
            assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, false);

            t1.deleteAsync(long.MaxValue).Wait();
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void basicNewItemCommit()
        public virtual void basicNewItemCommit()
        {
            Transaction t1 = manager.newTransaction();
            Dictionary<string, AttributeValue> key1 = newKey(INTEG_HASH_TABLE_NAME);

            t1.updateItemAsync(new UpdateItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Key = key1,

            }).Wait();
            assertItemLocked(INTEG_HASH_TABLE_NAME, key1, t1.Id, true, true);

            t1.commitAsync().Wait();
            assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, key1, true);
            t1.deleteAsync(long.MaxValue).Wait();
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void missingTableName()
        public virtual void missingTableName()
        {
            Transaction t1 = manager.newTransaction();
            Dictionary<string, AttributeValue> key1 = newKey(INTEG_HASH_TABLE_NAME);

            try
            {
                t1.updateItemAsync(new UpdateItemRequest
                {
                    Key = key1,

                }).Wait();
                fail();
            }
            catch (InvalidRequestException e)
            {
                assertTrue(e.Message, e.Message.Contains("TableName must not be null"));
            }
            assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, false);
            t1.rollbackAsync().Wait();
            assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, false);
            t1.deleteAsync(long.MaxValue).Wait();
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void emptyTransaction()
        public virtual void emptyTransaction()
        {
            Transaction t1 = manager.newTransaction();
            t1.commitAsync().Wait();
            t1.deleteAsync(long.MaxValue).Wait();
            assertTransactionDeleted(t1);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void missingKey()
        public virtual void missingKey()
        {
            Transaction t1 = manager.newTransaction();
            try
            {
                t1.updateItemAsync(new UpdateItemRequest
                {
                    TableName = INTEG_HASH_TABLE_NAME,

                }).Wait();
                fail();
            }
            catch (InvalidRequestException e)
            {
                assertTrue(e.Message, e.Message.Contains("The request key cannot be empty"));
            }
            t1.rollbackAsync().Wait();
            t1.deleteAsync(long.MaxValue).Wait();
        }

        /// <summary>
        /// This test makes a transaction with two large items, each of which are just below
        /// the DynamoDB item size limit (currently 400 KB).
        /// </summary>
        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void tooMuchDataInTransaction() throws Exception
        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        public virtual void tooMuchDataInTransaction()
        {
            Transaction t1 = manager.newTransaction();
            Transaction t2 = manager.newTransaction();
            Dictionary<string, AttributeValue> key1 = newKey(INTEG_HASH_TABLE_NAME);
            Dictionary<string, AttributeValue> key2 = newKey(INTEG_HASH_TABLE_NAME);

            // Write item 1 as a starting point
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < (MAX_ITEM_SIZE_BYTES / 1.5); i++)
            {
                sb.Append("a");
            }
            string bigString = sb.ToString();

            Dictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue>(key1);
            item1["bigattr"] = new AttributeValue("little");
            t1.putItemAsync(new PutItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Item = item1,

            }).Wait();

            assertItemLocked(INTEG_HASH_TABLE_NAME, key1, item1, t1.Id, true, true);

            t1.commitAsync().Wait();

            assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, item1, true);

            Dictionary<string, AttributeValue> item1a = new Dictionary<string, AttributeValue>(key1);
            item1a["bigattr"] = new AttributeValue(bigString);

            t2.putItemAsync(new PutItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Item = item1a,

            }).Wait();

            assertItemLocked(INTEG_HASH_TABLE_NAME, key1, item1a, t2.Id, false, true);

            Dictionary<string, AttributeValue> item2 = new Dictionary<string, AttributeValue>(key2);
            item2["bigattr"] = new AttributeValue(bigString);

            try
            {
                t2.putItemAsync(new PutItemRequest
                {
                    TableName = INTEG_HASH_TABLE_NAME,
                    Item = item2,

                }).Wait();
                fail();
            }
            catch (InvalidRequestException)
            {
            }

            assertItemNotLocked(INTEG_HASH_TABLE_NAME, key2, false);
            assertItemLocked(INTEG_HASH_TABLE_NAME, key1, item1a, t2.Id, false, true);

            item2["bigattr"] = new AttributeValue("fitsThisTime");
            t2.putItemAsync(new PutItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Item = item2,

            }).Wait();

            assertItemLocked(INTEG_HASH_TABLE_NAME, key2, item2, t2.Id, true, true);

            t2.commitAsync().Wait();

            assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, item1a, true);
            assertItemNotLocked(INTEG_HASH_TABLE_NAME, key2, item2, true);

            t1.deleteAsync(long.MaxValue).Wait();
            t2.deleteAsync(long.MaxValue).Wait();
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void containsBinaryAttributes()
        public virtual void containsBinaryAttributes()
        {
            Transaction t1 = manager.newTransaction();
            Dictionary<string, AttributeValue> key = newKey(INTEG_HASH_TABLE_NAME);
            Dictionary<string, AttributeValue> item = new Dictionary<string, AttributeValue>(key);

            item["attr_b"] = (new AttributeValue()).withB(new MemoryStream(Encoding.ASCII.GetBytes("asdf\n\t\u0123")));
            item["attr_bs"] = (new AttributeValue()).withBS(new List<MemoryStream> {
                new MemoryStream(Encoding.ASCII.GetBytes("asdf\n\t\u0123")),
                new MemoryStream(Encoding.ASCII.GetBytes("wef"))
            });

            t1.putItemAsync(new PutItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Item = item,

            }).Wait();

            assertItemLocked(INTEG_HASH_TABLE_NAME, key, item, t1.Id, true, true);

            t1.commitAsync().Wait();

            assertItemNotLocked(INTEG_HASH_TABLE_NAME, key, item, true);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void containsJSONAttributes()
        public virtual void containsJSONAttributes()
        {
            Transaction t1 = manager.newTransaction();
            Dictionary<string, AttributeValue> key = newKey(INTEG_HASH_TABLE_NAME);
            Dictionary<string, AttributeValue> item = new Dictionary<string, AttributeValue>(key);

            item["attr_json"] = new AttributeValue
            {
                M = JSON_M_ATTR_VAL,

            };

            t1.putItemAsync(new PutItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Item = item,

            }).Wait();

            assertItemLocked(INTEG_HASH_TABLE_NAME, key, item, t1.Id, true, true);

            t1.commitAsync().Wait();

            assertItemNotLocked(INTEG_HASH_TABLE_NAME, key, item, true);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void containsSpecialAttributes()
        public virtual void containsSpecialAttributes()
        {
            Transaction t1 = manager.newTransaction();
            Dictionary<string, AttributeValue> key = newKey(INTEG_HASH_TABLE_NAME);
            Dictionary<string, AttributeValue> item = new Dictionary<string, AttributeValue>(key);
            item[Transaction.AttributeName.TXID.ToString()] = new AttributeValue("asdf");

            try
            {
                t1.putItemAsync(new PutItemRequest
                {
                    TableName = INTEG_HASH_TABLE_NAME,
                    Item = item,

                }).Wait();
                fail();
            }
            catch (InvalidRequestException e)
            {
                assertTrue(e.Message.Contains("must not contain the reserved"));
            }

            item[Transaction.AttributeName.TRANSIENT.ToString()] = new AttributeValue("asdf");
            item.Remove(Transaction.AttributeName.TXID.ToString());

            try
            {
                t1.putItemAsync(new PutItemRequest
                {
                    TableName = INTEG_HASH_TABLE_NAME,
                    Item = item,

                });
                fail();
            }
            catch (InvalidRequestException e)
            {
                assertTrue(e.Message.Contains("must not contain the reserved"));
            }

            item[Transaction.AttributeName.APPLIED.ToString()] = new AttributeValue("asdf");
            item.Remove(Transaction.AttributeName.TRANSIENT.ToString());

            try
            {
                t1.putItemAsync(new PutItemRequest
                {
                    TableName = INTEG_HASH_TABLE_NAME,
                    Item = item,

                }).Wait();
                fail();
            }
            catch (InvalidRequestException e)
            {
                assertTrue(e.Message.Contains("must not contain the reserved"));
            }
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void itemTooLargeToLock()
        public virtual void itemTooLargeToLock()
        {

        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void itemTooLargeToApply()
        public virtual void itemTooLargeToApply()
        {

        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void itemTooLargeToSavePreviousVersion()
        public virtual void itemTooLargeToSavePreviousVersion()
        {

        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void failover() throws Exception
        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        public virtual void failover()
        {
            Transaction t1 = new TransactionAnonymousInnerClass14(this, Guid.NewGuid().ToString(), manager);

            // prepare a request
            UpdateItemRequest callerRequest = new UpdateItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Key = newKey(INTEG_HASH_TABLE_NAME),

            };

            try
            {
                t1.updateItemAsync(callerRequest).Wait();
                fail();
            }
            catch (FailingAmazonDynamoDBClient.FailedYourRequestException)
            {
            }
            assertItemNotLocked(INTEG_HASH_TABLE_NAME, callerRequest.Key, false);

            // The non-failing manager
            Transaction t2 = manager.resumeTransaction(t1.Id);
            t2.commitAsync().Wait();

            assertItemNotLocked(INTEG_HASH_TABLE_NAME, callerRequest.Key, true);

            // If this attempted to apply again, this would fail because of the failing client
            t1.commitAsync().Wait();

            assertItemNotLocked(INTEG_HASH_TABLE_NAME, callerRequest.Key, true);

            t1.deleteAsync(long.MaxValue).Wait();
            t2.deleteAsync(long.MaxValue).Wait();
        }

        private class TransactionAnonymousInnerClass14 : Transaction
        {
            private readonly TransactionsIntegrationTest outerInstance;

            public TransactionAnonymousInnerClass14(TransactionsIntegrationTest outerInstance, string toString, TransactionManager manager) : base(toString, manager, true)
            {
                this.outerInstance = outerInstance;
            }

            //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
            //ORIGINAL LINE: @Override protected java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> lockItem(Request callerRequest, boolean expectExists, int attempts) throws com.amazonaws.services.dynamodbv2.transactions.exceptions.TransactionException
            protected internal override Task<Dictionary<string, AttributeValue>> lockItemAsync(Request callerRequest, bool expectExists, int attempts)
            {

                throw new FailingAmazonDynamoDBClient.FailedYourRequestException();
            }
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void oneTransactionPerItem()
        public virtual void oneTransactionPerItem()
        {
            Transaction transaction = manager.newTransaction();
            Dictionary<string, AttributeValue> key = newKey(INTEG_HASH_TABLE_NAME);

            transaction.putItemAsync(new PutItemRequest
            {
                TableName = INTEG_HASH_TABLE_NAME,
                Item = key,

            }).Wait();
            try
            {
                transaction.putItemAsync(new PutItemRequest
                {
                    TableName = INTEG_HASH_TABLE_NAME,
                    Item = key,

                }).Wait();
                fail();
            }
            catch (DuplicateRequestException)
            {
                transaction.rollbackAsync().Wait();
            }
            assertItemNotLocked(INTEG_HASH_TABLE_NAME, key, false);
            transaction.deleteAsync(long.MaxValue).Wait();
        }
    }
}