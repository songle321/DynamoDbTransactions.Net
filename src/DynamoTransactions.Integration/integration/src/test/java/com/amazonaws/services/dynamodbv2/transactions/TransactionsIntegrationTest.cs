using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using com.amazonaws.services.dynamodbv2.transactions.exceptions;

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
    using DynamoTransactions;
    using After = org.junit.After;
    using Before = org.junit.Before;
    using Test = org.junit.Test;

    public class TransactionsIntegrationTest : IntegrationTest
	{

		private const int MAX_ITEM_SIZE_BYTES = 1024 * 400; // 400 KB

		internal static readonly IDictionary<string, AttributeValue> JSON_M_ATTR_VAL = new Dictionary<string, AttributeValue>();

		static TransactionsIntegrationTest()
		{
			JSON_M_ATTR_VAL["attr_s"] = (new AttributeValue()).withS("s");
			JSON_M_ATTR_VAL["attr_n"] = (new AttributeValue()).withN("1");
			JSON_M_ATTR_VAL["attr_b"] = (new AttributeValue()).withB(ByteBuffer.wrap(("asdf").GetBytes()));
			JSON_M_ATTR_VAL["attr_ss"] = (new AttributeValue()).withSS("a", "b");
			JSON_M_ATTR_VAL["attr_ns"] = (new AttributeValue()).withNS("1", "2");
			JSON_M_ATTR_VAL["attr_bs"] = (new AttributeValue()).withBS(ByteBuffer.wrap(("asdf").GetBytes()), ByteBuffer.wrap(("ghjk").GetBytes()));
			JSON_M_ATTR_VAL["attr_bool"] = (new AttributeValue()).withBOOL(true);
			JSON_M_ATTR_VAL["attr_l"] = (new AttributeValue()).withL((new AttributeValue()).withS("s"), (new AttributeValue()).withN("1"), (new AttributeValue()).withB(ByteBuffer.wrap(("asdf").GetBytes())), (new AttributeValue()).withBOOL(true), (new AttributeValue()).withNULL(true));
			JSON_M_ATTR_VAL["attr_null"] = (new AttributeValue()).withNULL(true);
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
			item0.Add("ss_otherattr", (new AttributeValue()).withSS("one", "two"));
			IDictionary<string, AttributeValue> putResponse = t.putItem(new PutItemRequest()
				.withTableName(INTEG_HASH_TABLE_NAME).withItem(item0).withReturnValues(ReturnValue.ALL_OLD)).Attributes;
			assertNull(putResult);
			t.commit();
			assertItemNotLocked(INTEG_HASH_TABLE_NAME, key0, item0, true);
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @After public void teardown()
		public virtual void teardown()
		{
			dynamodb.reset();
			Transaction t = manager.newTransaction();
			t.deleteItem((new DeleteItemRequest()).withTableName(INTEG_HASH_TABLE_NAME).withKey(key0));
			t.commit();
			assertItemNotLocked(INTEG_HASH_TABLE_NAME, key0, false);
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void phantomItemFromDelete()
		public virtual void phantomItemFromDelete()
		{
			IDictionary<string, AttributeValue> key1 = newKey(INTEG_HASH_TABLE_NAME);
			Transaction transaction = manager.newTransaction();
			DeleteItemRequest deleteRequest = (new DeleteItemRequest()).withTableName(INTEG_HASH_TABLE_NAME).withKey(key1);
			transaction.deleteItem(deleteRequest);
			assertItemLocked(INTEG_HASH_TABLE_NAME, key1, transaction.Id, true, false);
			transaction.rollback();
			assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, false);
			transaction.delete(long.MaxValue);
		}

		/*
		 * GetItem tests
		 */

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void lockItem()
		public virtual void lockItem()
		{
			IDictionary<string, AttributeValue> key1 = newKey(INTEG_HASH_TABLE_NAME);
			Transaction t1 = manager.newTransaction();
			Transaction t2 = manager.newTransaction();

			DeleteItemRequest deleteRequest = (new DeleteItemRequest()).withTableName(INTEG_HASH_TABLE_NAME).withKey(key1);

			GetItemRequest lockRequest = (new GetItemRequest()).withTableName(INTEG_HASH_TABLE_NAME).withKey(key1);

			IDictionary<string, AttributeValue> getResponse = t1.getItem(lockRequest).Item;

			assertItemLocked(INTEG_HASH_TABLE_NAME, key1, t1.Id, true, false); // we're not applying locks
			assertNull(getResult);

			IDictionary<string, AttributeValue> deleteResponse = t2.deleteItem(deleteRequest).Attributes;
			assertItemLocked(INTEG_HASH_TABLE_NAME, key1, t2.Id, true, false); // we're not applying deletes either
			assertNull(deleteResult); // return values is null in the request

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

			assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, false);
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void lock2Items()
		public virtual void lock2Items()
		{
			IDictionary<string, AttributeValue> key1 = newKey(INTEG_HASH_TABLE_NAME);
			IDictionary<string, AttributeValue> key2 = newKey(INTEG_HASH_TABLE_NAME);

			Transaction t0 = manager.newTransaction();
			IDictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue>(key1);
			item1["something"] = new AttributeValue("val");
			IDictionary<string, AttributeValue> putResponse = t0.putItem(new PutItemRequest()
				.withTableName(INTEG_HASH_TABLE_NAME).withItem(item1).withReturnValues(ReturnValue.ALL_OLD)).Attributes;
			assertNull(putResult);

			t0.commit();

			Transaction t1 = manager.newTransaction();

			IDictionary<string, AttributeValue> getResult1 = t1.getItem(new GetItemRequest()
				.withTableName(INTEG_HASH_TABLE_NAME).withKey(key1)).Item;
			assertItemLocked(INTEG_HASH_TABLE_NAME, key1, item1, t1.Id, false, false);
			assertEquals(item1, getResult1);

			IDictionary<string, AttributeValue> getResult2 = t1.getItem(new GetItemRequest()
				.withTableName(INTEG_HASH_TABLE_NAME).withKey(key2)).Item;
			assertItemLocked(INTEG_HASH_TABLE_NAME, key1, item1, t1.Id, false, false);
			assertItemLocked(INTEG_HASH_TABLE_NAME, key2, t1.Id, true, false);
			assertNull(getResult2);

			t1.commit();
			t1.delete(long.MaxValue);

			assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, item1, true);
			assertItemNotLocked(INTEG_HASH_TABLE_NAME, key2, false);
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void getItemWithDelete()
		public virtual void getItemWithDelete()
		{
			Transaction t1 = manager.newTransaction();
			IDictionary<string, AttributeValue> getResult1 = t1.getItem((new GetItemRequest()).withTableName(INTEG_HASH_TABLE_NAME).withKey(key0)).Item;
			assertEquals(getResult1, item0);
			assertItemLocked(INTEG_HASH_TABLE_NAME, key0, item0, t1.Id, false, false);

			t1.deleteItem((new DeleteItemRequest()).withTableName(INTEG_HASH_TABLE_NAME).withKey(key0));
			assertItemLocked(INTEG_HASH_TABLE_NAME, key0, item0, t1.Id, false, false);

			IDictionary<string, AttributeValue> getResult2 = t1.getItem((new GetItemRequest()).withTableName(INTEG_HASH_TABLE_NAME).withKey(key0)).Item;
			assertNull(getResult2);
			assertItemLocked(INTEG_HASH_TABLE_NAME, key0, item0, t1.Id, false, false);

			t1.commit();
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void getFilterAttributesToGet()
		public virtual void getFilterAttributesToGet()
		{
			Transaction t1 = manager.newTransaction();

			IDictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue>();
			item1["s_someattr"] = item0.get("s_someattr");

			IDictionary<string, AttributeValue> getResult1 = t1.getItem(new GetItemRequest()
				.withTableName(INTEG_HASH_TABLE_NAME).withAttributesToGet("s_someattr", "notexists").withKey(key0)).Item;
			assertEquals(item1, getResult1);
			assertItemLocked(INTEG_HASH_TABLE_NAME, key0, t1.Id, false, false);

			t1.commit();

			assertItemNotLocked(INTEG_HASH_TABLE_NAME, key0, item0, true);
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void getItemNotExists()
		public virtual void getItemNotExists()
		{
			Transaction t1 = manager.newTransaction();
			IDictionary<string, AttributeValue> key1 = newKey(INTEG_HASH_TABLE_NAME);

			IDictionary<string, AttributeValue> getResult1 = t1.getItem((new GetItemRequest()).withTableName(INTEG_HASH_TABLE_NAME).withKey(key1)).Item;
			assertNull(getResult1);
			assertItemLocked(INTEG_HASH_TABLE_NAME, key1, t1.Id, true, false);

			IDictionary<string, AttributeValue> getResult2 = t1.getItem((new GetItemRequest()).withTableName(INTEG_HASH_TABLE_NAME).withKey(key1)).Item;
			assertNull(getResult2);
			assertItemLocked(INTEG_HASH_TABLE_NAME, key1, t1.Id, true, false);

			t1.commit();

			assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, false);
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void getItemAfterPutItemInsert()
		public virtual void getItemAfterPutItemInsert()
		{
			Transaction t1 = manager.newTransaction();
			IDictionary<string, AttributeValue> key1 = newKey(INTEG_HASH_TABLE_NAME);
			IDictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue>(key1);
			item1["asdf"] = new AttributeValue("wef");

			IDictionary<string, AttributeValue> getResult1 = t1.getItem((new GetItemRequest()).withTableName(INTEG_HASH_TABLE_NAME).withKey(key1)).Item;
			assertNull(getResult1);
			assertItemLocked(INTEG_HASH_TABLE_NAME, key1, t1.Id, true, false);

			IDictionary<string, AttributeValue> putResult1 = t1.putItem(new PutItemRequest()
				.withTableName(INTEG_HASH_TABLE_NAME).withItem(item1).withReturnValues(ReturnValue.ALL_OLD)).Attributes;
			assertItemLocked(INTEG_HASH_TABLE_NAME, key1, item1, t1.Id, true, true);
			assertNull(putResult1);

			IDictionary<string, AttributeValue> getResult2 = t1.getItem((new GetItemRequest()).withTableName(INTEG_HASH_TABLE_NAME).withKey(key1)).Item;
			assertEquals(getResult2, item1);
			assertItemLocked(INTEG_HASH_TABLE_NAME, key1, item1, t1.Id, true, true);

			t1.commit();

			assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, item1, true);
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void getItemAfterPutItemOverwrite()
		public virtual void getItemAfterPutItemOverwrite()
		{
			Transaction t1 = manager.newTransaction();
			IDictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue>(item0);
			item1["asdf"] = new AttributeValue("wef");

			IDictionary<string, AttributeValue> getResult1 = t1.getItem((new GetItemRequest()).withTableName(INTEG_HASH_TABLE_NAME).withKey(key0)).Item;
			assertEquals(getResult1, item0);
			assertItemLocked(INTEG_HASH_TABLE_NAME, key0, item0, t1.Id, false, false);

			IDictionary<string, AttributeValue> putResult1 = t1.putItem(new PutItemRequest()
				.withTableName(INTEG_HASH_TABLE_NAME).withItem(item1).withReturnValues(ReturnValue.ALL_OLD)).Attributes;
			assertItemLocked(INTEG_HASH_TABLE_NAME, key0, item1, t1.Id, false, true);
			assertEquals(putResult1, item0);

			IDictionary<string, AttributeValue> getResult2 = t1.getItem((new GetItemRequest()).withTableName(INTEG_HASH_TABLE_NAME).withKey(key0)).Item;
			assertEquals(getResult2, item1);
			assertItemLocked(INTEG_HASH_TABLE_NAME, key0, item1, t1.Id, false, true);

			t1.commit();

			assertItemNotLocked(INTEG_HASH_TABLE_NAME, key0, item1, true);
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void getItemAfterPutItemInsertInResumedTx()
		public virtual void getItemAfterPutItemInsertInResumedTx()
		{
			Transaction t1 = new TransactionAnonymousInnerClass(this, UUID.randomUUID().ToString(), manager);

			Transaction t2 = manager.resumeTransaction(t1.Id);

			IDictionary<string, AttributeValue> key1 = newKey(INTEG_HASH_TABLE_NAME);
			IDictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue>(key1);
			item1["asdf"] = new AttributeValue("wef");

			try
			{
				// This Put needs to fail in apply
				t1.putItem(new PutItemRequest()
					.withTableName(INTEG_HASH_TABLE_NAME).withItem(item1).withReturnValues(ReturnValue.ALL_OLD)).Attributes;
				fail();
			}
			catch (FailingAmazonDynamoDBClient.FailedYourRequestException)
			{
			}
			assertItemLocked(INTEG_HASH_TABLE_NAME, key1, t1.Id, true, false);

			// second copy of same tx
			IDictionary<string, AttributeValue> getResult1 = t2.getItem((new GetItemRequest()).withTableName(INTEG_HASH_TABLE_NAME).withKey(key1)).Item;
			assertEquals(getResult1, item1);
			assertItemLocked(INTEG_HASH_TABLE_NAME, key1, item1, t1.Id, true, true);

			t2.commit();

			assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, item1, true);
		}

		private class TransactionAnonymousInnerClass : Transaction
		{
			private readonly TransactionsIntegrationTest outerInstance;

			public TransactionAnonymousInnerClass(TransactionsIntegrationTest outerInstance, string toString, UnknownType manager) : base(toString, manager, true)
			{
				this.outerInstance = outerInstance;
			}

			protected internal override IDictionary<string, AttributeValue> applyAndKeepLock(Request request, IDictionary<string, AttributeValue> lockedItem)
			{
				throw new FailingAmazonDynamoDBClient.FailedYourRequestException();
			}
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void getItemThenPutItemInResumedTxThenGetItem()
		public virtual void getItemThenPutItemInResumedTxThenGetItem()
		{
			Transaction t1 = new TransactionAnonymousInnerClass2(this, UUID.randomUUID().ToString(), manager);

			Transaction t2 = manager.resumeTransaction(t1.Id);

			IDictionary<string, AttributeValue> key1 = newKey(INTEG_HASH_TABLE_NAME);
			IDictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue>(key1);
			item1["asdf"] = new AttributeValue("wef");

			// Get a read lock in t2
			IDictionary<string, AttributeValue> getResult1 = t2.getItem((new GetItemRequest()).withTableName(INTEG_HASH_TABLE_NAME).withKey(key1)).Item;
			assertNull(getResult1);
			assertItemLocked(INTEG_HASH_TABLE_NAME, key1, null, t1.Id, true, false);

			// Begin a PutItem in t1, but fail apply
			try
			{
				t1.putItem(new PutItemRequest()
					.withTableName(INTEG_HASH_TABLE_NAME).withItem(item1).withReturnValues(ReturnValue.ALL_OLD)).Attributes;
				fail();
			}
			catch (FailingAmazonDynamoDBClient.FailedYourRequestException)
			{
			}
			assertItemLocked(INTEG_HASH_TABLE_NAME, key1, t1.Id, true, false);

			// Read again in the non-failing copy of the transaction
			IDictionary<string, AttributeValue> getResult2 = t2.getItem((new GetItemRequest()).withTableName(INTEG_HASH_TABLE_NAME).withKey(key1)).Item;
			assertItemLocked(INTEG_HASH_TABLE_NAME, key1, item1, t1.Id, true, true);
			t2.commit();
			assertEquals(item1, getResult2);

			assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, item1, true);
		}

		private class TransactionAnonymousInnerClass2 : Transaction
		{
			private readonly TransactionsIntegrationTest outerInstance;

			public TransactionAnonymousInnerClass2(TransactionsIntegrationTest outerInstance, string toString, UnknownType manager) : base(toString, manager, true)
			{
				this.outerInstance = outerInstance;
			}

			protected internal override IDictionary<string, AttributeValue> applyAndKeepLock(Request request, IDictionary<string, AttributeValue> lockedItem)
			{
				if (request is Request.GetItem || request is Request.DeleteItem)
				{
					return base.applyAndKeepLock(request, lockedItem);
				}
				throw new FailingAmazonDynamoDBClient.FailedYourRequestException();
			}
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void getThenUpdateNewItem()
		public virtual void getThenUpdateNewItem()
		{
			Transaction t1 = manager.newTransaction();
			IDictionary<string, AttributeValue> key1 = newKey(INTEG_HASH_TABLE_NAME);

			IDictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue>(key1);
			item1["asdf"] = new AttributeValue("didn't exist");

			IDictionary<string, AttributeValueUpdate> updates1 = new Dictionary<string, AttributeValueUpdate>();
			updates1["asdf"] = new AttributeValueUpdate(new AttributeValue("didn't exist"), AttributeAction.PUT);

			IDictionary<string, AttributeValue> getResponse = t1.getItem((new GetItemRequest()).withTableName(INTEG_HASH_TABLE_NAME).withKey(key1)).Item;
			assertItemLocked(INTEG_HASH_TABLE_NAME, key1, t1.Id, true, false);
			assertNull(getResult);

			IDictionary<string, AttributeValue> updateResponse = t1.updateItem((new UpdateItemRequest()).withTableName(INTEG_HASH_TABLE_NAME).withKey(key1).withAttributeUpdates(updates1).withReturnValues(ReturnValue.ALL_NEW)).Attributes;
			assertItemLocked(INTEG_HASH_TABLE_NAME, key1, item1, t1.Id, true, true);
			assertEquals(item1, updateResult);

			t1.commit();

			assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, item1, true);
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void getThenUpdateExistingItem()
		public virtual void getThenUpdateExistingItem()
		{
			Transaction t1 = manager.newTransaction();

			IDictionary<string, AttributeValue> item0a = new Dictionary<string, AttributeValue>(item0);
			item0a["wef"] = new AttributeValue("new attr");

			IDictionary<string, AttributeValueUpdate> updates1 = new Dictionary<string, AttributeValueUpdate>();
			updates1["wef"] = new AttributeValueUpdate(new AttributeValue("new attr"), AttributeAction.PUT);

			IDictionary<string, AttributeValue> getResponse = t1.getItem((new GetItemRequest()).withTableName(INTEG_HASH_TABLE_NAME).withKey(key0)).Item;
			assertItemLocked(INTEG_HASH_TABLE_NAME, key0, item0, t1.Id, false, false);
			assertEquals(item0, getResult);

			IDictionary<string, AttributeValue> updateResponse = t1.updateItem((new UpdateItemRequest()).withTableName(INTEG_HASH_TABLE_NAME).withKey(key0).withAttributeUpdates(updates1).withReturnValues(ReturnValue.ALL_NEW)).Attributes;
			assertItemLocked(INTEG_HASH_TABLE_NAME, key0, item0a, t1.Id, false, true);
			assertEquals(item0a, updateResult);

			t1.commit();

			assertItemNotLocked(INTEG_HASH_TABLE_NAME, key0, item0a, true);
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void getItemUncommittedInsert()
		public virtual void getItemUncommittedInsert()
		{
			Transaction t1 = manager.newTransaction();

			IDictionary<string, AttributeValue> key1 = newKey(INTEG_HASH_TABLE_NAME);
			IDictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue>(key1);
			item1["asdf"] = new AttributeValue("wef");

			t1.putItem(new PutItemRequest()
				.withTableName(INTEG_HASH_TABLE_NAME).withItem(item1));

			assertItemLocked(INTEG_HASH_TABLE_NAME, key1, item1, t1.Id, true, true);

			IDictionary<string, AttributeValue> item = manager.getItem(new GetItemRequest()
				.withTableName(INTEG_HASH_TABLE_NAME).withKey(key1), Transaction.IsolationLevel.UNCOMMITTED).Item;
			assertNoSpecialAttributes(item);
			assertEquals(item1, item);
			assertItemLocked(INTEG_HASH_TABLE_NAME, key1, item1, t1.Id, true, true);

			t1.rollback();
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void getItemUncommittedDeleted()
		public virtual void getItemUncommittedDeleted()
		{
			Transaction t1 = manager.newTransaction();

			t1.deleteItem(new DeleteItemRequest()
				.withTableName(INTEG_HASH_TABLE_NAME).withKey(key0));

			assertItemLocked(INTEG_HASH_TABLE_NAME, key0, item0, t1.Id, false, false);

			IDictionary<string, AttributeValue> item = manager.getItem(new GetItemRequest()
				.withTableName(INTEG_HASH_TABLE_NAME).withKey(key0), Transaction.IsolationLevel.UNCOMMITTED).Item;
			assertNoSpecialAttributes(item);
			assertEquals(item0, item);
			assertItemLocked(INTEG_HASH_TABLE_NAME, key0, item0, t1.Id, false, false);

			t1.rollback();
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void getItemCommittedInsert()
		public virtual void getItemCommittedInsert()
		{
			Transaction t1 = manager.newTransaction();

			IDictionary<string, AttributeValue> key1 = newKey(INTEG_HASH_TABLE_NAME);
			IDictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue>(key1);
			item1["asdf"] = new AttributeValue("wef");

			t1.putItem(new PutItemRequest()
				.withTableName(INTEG_HASH_TABLE_NAME).withItem(item1));

			assertItemLocked(INTEG_HASH_TABLE_NAME, key1, item1, t1.Id, true, true);

			IDictionary<string, AttributeValue> item = manager.getItem(new GetItemRequest()
				.withTableName(INTEG_HASH_TABLE_NAME).withKey(key1), Transaction.IsolationLevel.COMMITTED).Item;
			assertNull(item);
			assertItemLocked(INTEG_HASH_TABLE_NAME, key1, item1, t1.Id, true, true);

			t1.rollback();
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void getItemCommittedDeleted()
		public virtual void getItemCommittedDeleted()
		{
			Transaction t1 = manager.newTransaction();

			t1.deleteItem(new DeleteItemRequest()
				.withTableName(INTEG_HASH_TABLE_NAME).withKey(key0));

			assertItemLocked(INTEG_HASH_TABLE_NAME, key0, item0, t1.Id, false, false);

			IDictionary<string, AttributeValue> item = manager.getItem(new GetItemRequest()
				.withTableName(INTEG_HASH_TABLE_NAME).withKey(key0), Transaction.IsolationLevel.COMMITTED).Item;
			assertNoSpecialAttributes(item);
			assertEquals(item0, item);
			assertItemLocked(INTEG_HASH_TABLE_NAME, key0, item0, t1.Id, false, false);

			t1.rollback();
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void getItemCommittedUpdated()
		public virtual void getItemCommittedUpdated()
		{
			Transaction t1 = manager.newTransaction();

			IDictionary<string, AttributeValueUpdate> updates = new Dictionary<string, AttributeValueUpdate>();
			updates["asdf"] = (new AttributeValueUpdate()).withAction(AttributeAction.PUT).withValue(new AttributeValue("asdf"));
			IDictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue>(item0);
			item1["asdf"] = new AttributeValue("asdf");

			t1.updateItem(new UpdateItemRequest()
				.withTableName(INTEG_HASH_TABLE_NAME).withAttributeUpdates(updates).withKey(key0));

			assertItemLocked(INTEG_HASH_TABLE_NAME, key0, item1, t1.Id, false, true);

			IDictionary<string, AttributeValue> item = manager.getItem(new GetItemRequest()
				.withTableName(INTEG_HASH_TABLE_NAME).withKey(key0), Transaction.IsolationLevel.COMMITTED).Item;
			assertNoSpecialAttributes(item);
			assertEquals(item0, item);
			assertItemLocked(INTEG_HASH_TABLE_NAME, key0, item1, t1.Id, false, true);

			t1.commit();

			item = manager.getItem(new GetItemRequest()
				.withTableName(INTEG_HASH_TABLE_NAME).withKey(key0), Transaction.IsolationLevel.COMMITTED).Item;
			assertNoSpecialAttributes(item);
			assertEquals(item1, item);
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void getItemCommittedUpdatedAndApplied()
		public virtual void getItemCommittedUpdatedAndApplied()
		{
			Transaction t1 = new TransactionAnonymousInnerClass3(this, UUID.randomUUID().ToString(), manager);

			IDictionary<string, AttributeValueUpdate> updates = new Dictionary<string, AttributeValueUpdate>();
			updates["asdf"] = (new AttributeValueUpdate()).withAction(AttributeAction.PUT).withValue(new AttributeValue("asdf"));
			IDictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue>(item0);
			item1["asdf"] = new AttributeValue("asdf");

			t1.updateItem(new UpdateItemRequest()
				.withTableName(INTEG_HASH_TABLE_NAME).withAttributeUpdates(updates).withKey(key0));

			assertItemLocked(INTEG_HASH_TABLE_NAME, key0, item1, t1.Id, false, true);

			t1.commit();

			IDictionary<string, AttributeValue> item = manager.getItem(new GetItemRequest()
				.withTableName(INTEG_HASH_TABLE_NAME).withKey(key0), Transaction.IsolationLevel.COMMITTED).Item;
			assertNoSpecialAttributes(item);
			assertEquals(item1, item);
			assertItemLocked(INTEG_HASH_TABLE_NAME, key0, item1, t1.Id, false, true);
		}

		private class TransactionAnonymousInnerClass3 : Transaction
		{
			private readonly TransactionsIntegrationTest outerInstance;

			public TransactionAnonymousInnerClass3(TransactionsIntegrationTest outerInstance, string toString, UnknownType manager) : base(toString, manager, true)
			{
				this.outerInstance = outerInstance;
			}

			protected internal override void doCommit()
			{
				//Skip cleaning up the transaction so we can validate reading.
			}
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void getItemCommittedMissingImage()
		public virtual void getItemCommittedMissingImage()
		{
			Transaction t1 = manager.newTransaction();
			IDictionary<string, AttributeValueUpdate> updates = new Dictionary<string, AttributeValueUpdate>();
			updates["asdf"] = (new AttributeValueUpdate()).withAction(AttributeAction.PUT).withValue(new AttributeValue("asdf"));
			IDictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue>(item0);
			item1["asdf"] = new AttributeValue("asdf");

			t1.updateItem(new UpdateItemRequest()
				.withTableName(INTEG_HASH_TABLE_NAME).withAttributeUpdates(updates).withKey(key0));

			assertItemLocked(INTEG_HASH_TABLE_NAME, key0, item1, t1.Id, false, true);

			dynamodb.getRequestsToTreatAsDeleted.add(new GetItemRequest()
				.withTableName(manager.ItemImageTableName).addKeyEntry(Transaction.AttributeName.IMAGE_ID.ToString(), new AttributeValue(t1.TxItem.txId + "#" + 1)).withConsistentRead(true));

			try
			{
				manager.getItem(new GetItemRequest()
					.withTableName(INTEG_HASH_TABLE_NAME).withKey(key0), Transaction.IsolationLevel.COMMITTED).Item;
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

			Transaction t1 = new TransactionAnonymousInnerClass4(this, UUID.randomUUID().ToString(), manager);
			IDictionary<string, AttributeValueUpdate> updates = new Dictionary<string, AttributeValueUpdate>();
			updates["asdf"] = (new AttributeValueUpdate()).withAction(AttributeAction.PUT).withValue(new AttributeValue("asdf"));
			IDictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue>(item0);
			item1["asdf"] = new AttributeValue("asdf");

			t1.updateItem(new UpdateItemRequest()
				.withTableName(INTEG_HASH_TABLE_NAME).withAttributeUpdates(updates).withKey(key0));

			assertItemLocked(INTEG_HASH_TABLE_NAME, key0, item1, t1.Id, false, true);

			GetItemRequest txItemRequest = (new GetItemRequest()).withTableName(manager.TransactionTableName).addKeyEntry(Transaction.AttributeName.TXID.ToString(), new AttributeValue(t1.TxItem.txId)).withConsistentRead(true);

			//Save the copy of the transaction before commit. 
			GetItemResponse uncommittedTransaction = dynamodb.getItem(txItemRequest);

			t1.commit();
			assertItemLocked(INTEG_HASH_TABLE_NAME, key0, item1, t1.Id, false, true);

			dynamodb.getRequestsToStub.Add(txItemRequest, new LinkedList<GetItemResult>(Collections.singletonList(uncommittedTransaction)));
			//Stub out the image so it appears deleted
			dynamodb.getRequestsToTreatAsDeleted.add(new GetItemRequest()
				.withTableName(manager.ItemImageTableName).addKeyEntry(Transaction.AttributeName.IMAGE_ID.ToString(), new AttributeValue(t1.TxItem.txId + "#" + 1)).withConsistentRead(true));

			IDictionary<string, AttributeValue> item = manager.getItem(new GetItemRequest()
				.withTableName(INTEG_HASH_TABLE_NAME).withKey(key0), Transaction.IsolationLevel.COMMITTED).Item;
			assertNoSpecialAttributes(item);
			assertEquals(item1, item);
		}

		private class TransactionAnonymousInnerClass4 : Transaction
		{
			private readonly TransactionsIntegrationTest outerInstance;

			public TransactionAnonymousInnerClass4(TransactionsIntegrationTest outerInstance, string toString, UnknownType manager) : base(toString, manager, true)
			{
				this.outerInstance = outerInstance;
			}

			protected internal override void doCommit()
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
			IDictionary<string, AttributeValue> key1 = newKey(INTEG_HASH_TABLE_NAME);
			IDictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue>(key1);
			item1["asdf"] = new AttributeValue("wef");

			IDictionary<string, AttributeValue> putResult1 = t1.putItem(new PutItemRequest()
				.withTableName(INTEG_HASH_TABLE_NAME).withItem(item1).withReturnValues(ReturnValue.ALL_OLD)).Attributes;
			assertItemLocked(INTEG_HASH_TABLE_NAME, key1, item1, t1.Id, true, true);
			assertNull(putResult1);

			t1.commit();

			assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, item1, true);
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void putItemAllOldOverwrite()
		public virtual void putItemAllOldOverwrite()
		{
			Transaction t1 = manager.newTransaction();
			IDictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue>(item0);
			item1["asdf"] = new AttributeValue("wef");

			IDictionary<string, AttributeValue> putResult1 = t1.putItem(new PutItemRequest()
				.withTableName(INTEG_HASH_TABLE_NAME).withItem(item1).withReturnValues(ReturnValue.ALL_OLD)).Attributes;
			assertItemLocked(INTEG_HASH_TABLE_NAME, key0, item1, t1.Id, false, true);
			assertEquals(putResult1, item0);

			t1.commit();

			assertItemNotLocked(INTEG_HASH_TABLE_NAME, key0, item1, true);
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void updateItemAllOldInsert()
		public virtual void updateItemAllOldInsert()
		{
			Transaction t1 = manager.newTransaction();
			IDictionary<string, AttributeValue> key1 = newKey(INTEG_HASH_TABLE_NAME);
			IDictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue>(key1);
			item1["asdf"] = new AttributeValue("wef");
			IDictionary<string, AttributeValueUpdate> updates = new Dictionary<string, AttributeValueUpdate>();
			updates["asdf"] = (new AttributeValueUpdate()).withAction(AttributeAction.PUT).withValue(new AttributeValue("wef"));

			IDictionary<string, AttributeValue> result1 = t1.updateItem(new UpdateItemRequest()
				.withTableName(INTEG_HASH_TABLE_NAME).withKey(key1).withAttributeUpdates(updates).withReturnValues(ReturnValue.ALL_OLD)).Attributes;
			assertItemLocked(INTEG_HASH_TABLE_NAME, key1, item1, t1.Id, true, true);
			assertNull(result1);

			t1.commit();

			assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, item1, true);
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void updateItemAllOldOverwrite()
		public virtual void updateItemAllOldOverwrite()
		{
			Transaction t1 = manager.newTransaction();
			IDictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue>(item0);
			item1["asdf"] = new AttributeValue("wef");
			IDictionary<string, AttributeValueUpdate> updates = new Dictionary<string, AttributeValueUpdate>();
			updates["asdf"] = (new AttributeValueUpdate()).withAction(AttributeAction.PUT).withValue(new AttributeValue("wef"));

			IDictionary<string, AttributeValue> result1 = t1.updateItem(new UpdateItemRequest()
				.withTableName(INTEG_HASH_TABLE_NAME).withKey(key0).withAttributeUpdates(updates).withReturnValues(ReturnValue.ALL_OLD)).Attributes;
			assertItemLocked(INTEG_HASH_TABLE_NAME, key0, item1, t1.Id, false, true);
			assertEquals(result1, item0);

			t1.commit();

			assertItemNotLocked(INTEG_HASH_TABLE_NAME, key0, item1, true);
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void updateItemAllNewInsert()
		public virtual void updateItemAllNewInsert()
		{
			Transaction t1 = manager.newTransaction();
			IDictionary<string, AttributeValue> key1 = newKey(INTEG_HASH_TABLE_NAME);
			IDictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue>(key1);
			item1["asdf"] = new AttributeValue("wef");
			IDictionary<string, AttributeValueUpdate> updates = new Dictionary<string, AttributeValueUpdate>();
			updates["asdf"] = (new AttributeValueUpdate()).withAction(AttributeAction.PUT).withValue(new AttributeValue("wef"));

			IDictionary<string, AttributeValue> result1 = t1.updateItem(new UpdateItemRequest()
				.withTableName(INTEG_HASH_TABLE_NAME).withKey(key1).withAttributeUpdates(updates).withReturnValues(ReturnValue.ALL_NEW)).Attributes;
			assertItemLocked(INTEG_HASH_TABLE_NAME, key1, item1, t1.Id, true, true);
			assertEquals(result1, item1);

			t1.commit();

			assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, item1, true);
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void updateItemAllNewOverwrite()
		public virtual void updateItemAllNewOverwrite()
		{
			Transaction t1 = manager.newTransaction();
			IDictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue>(item0);
			item1["asdf"] = new AttributeValue("wef");
			IDictionary<string, AttributeValueUpdate> updates = new Dictionary<string, AttributeValueUpdate>();
			updates["asdf"] = (new AttributeValueUpdate()).withAction(AttributeAction.PUT).withValue(new AttributeValue("wef"));

			IDictionary<string, AttributeValue> result1 = t1.updateItem(new UpdateItemRequest()
				.withTableName(INTEG_HASH_TABLE_NAME).withKey(key0).withAttributeUpdates(updates).withReturnValues(ReturnValue.ALL_NEW)).Attributes;
			assertItemLocked(INTEG_HASH_TABLE_NAME, key0, item1, t1.Id, false, true);
			assertEquals(result1, item1);

			t1.commit();

			assertItemNotLocked(INTEG_HASH_TABLE_NAME, key0, item1, true);
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void deleteItemAllOldNotExists()
		public virtual void deleteItemAllOldNotExists()
		{
			Transaction t1 = manager.newTransaction();
			IDictionary<string, AttributeValue> key1 = newKey(INTEG_HASH_TABLE_NAME);

			IDictionary<string, AttributeValue> result1 = t1.deleteItem(new DeleteItemRequest()
				.withTableName(INTEG_HASH_TABLE_NAME).withKey(key1).withReturnValues(ReturnValue.ALL_OLD)).Attributes;
			assertItemLocked(INTEG_HASH_TABLE_NAME, key1, key1, t1.Id, true, false);
			assertNull(result1);

			t1.commit();

			assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, false);
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void deleteItemAllOldExists()
		public virtual void deleteItemAllOldExists()
		{
			Transaction t1 = manager.newTransaction();

			IDictionary<string, AttributeValue> result1 = t1.deleteItem(new DeleteItemRequest()
				.withTableName(INTEG_HASH_TABLE_NAME).withKey(key0).withReturnValues(ReturnValue.ALL_OLD)).Attributes;
			assertItemLocked(INTEG_HASH_TABLE_NAME, key0, item0, t1.Id, false, false);
			assertEquals(item0, result1);

			t1.commit();

			assertItemNotLocked(INTEG_HASH_TABLE_NAME, key0, false);
		}

		/*
		 * Transaction isolation and error tests
		 */

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void conflictingWrites()
		public virtual void conflictingWrites()
		{
			IDictionary<string, AttributeValue> key1 = newKey(INTEG_HASH_TABLE_NAME);
			Transaction t1 = manager.newTransaction();
			Transaction t2 = manager.newTransaction();
			Transaction t3 = manager.newTransaction();

			// Finish t1 
			IDictionary<string, AttributeValue> t1Item = new Dictionary<string, AttributeValue>(key1);
			t1Item["whoami"] = new AttributeValue("t1");

			t1.putItem(new PutItemRequest()
				.withTableName(INTEG_HASH_TABLE_NAME).withItem(new Dictionary<string, AttributeValue>(t1Item)));
			assertItemLocked(INTEG_HASH_TABLE_NAME, key1, t1Item, t1.Id, true, true);

			t1.commit();
			assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, t1Item, true);

			// Begin t2
			IDictionary<string, AttributeValue> t2Item = new Dictionary<string, AttributeValue>(key1);
			t2Item["whoami"] = new AttributeValue("t2");
			t2Item["t2stuff"] = new AttributeValue("extra");

			t2.putItem(new PutItemRequest()
				.withTableName(INTEG_HASH_TABLE_NAME).withItem(new Dictionary<string, AttributeValue>(t2Item)));
			assertItemLocked(INTEG_HASH_TABLE_NAME, key1, t2Item, t2.Id, false, true);

			// Begin and finish t3
			IDictionary<string, AttributeValue> t3Item = new Dictionary<string, AttributeValue>(key1);
			t3Item["whoami"] = new AttributeValue("t3");
			t3Item["t3stuff"] = new AttributeValue("things");

			t3.putItem(new PutItemRequest()
				.withTableName(INTEG_HASH_TABLE_NAME).withItem(new Dictionary<string, AttributeValue>(t3Item)));
			assertItemLocked(INTEG_HASH_TABLE_NAME, key1, t3Item, t3.Id, false, true);

			t3.commit();

			assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, t3Item, true);

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

			assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, t3Item, true);
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void failValidationInApply()
		public virtual void failValidationInApply()
		{
			IDictionary<string, AttributeValue> key = newKey(INTEG_HASH_TABLE_NAME);
			IDictionary<string, AttributeValueUpdate> updates = new Dictionary<string, AttributeValueUpdate>();
			updates["FooAttribute"] = (new AttributeValueUpdate()).withAction(AttributeAction.PUT).withValue(new AttributeValue("Bar"));

			Transaction t1 = manager.newTransaction();
			Transaction t2 = manager.newTransaction();

			t1.updateItem(new UpdateItemRequest()
				.withTableName(INTEG_HASH_TABLE_NAME).withKey(key).withAttributeUpdates(updates));

			assertItemLocked(INTEG_HASH_TABLE_NAME, key, t1.Id, true, true);

			t1.commit();

			assertItemNotLocked(INTEG_HASH_TABLE_NAME, key, true);

			updates["FooAttribute"] = (new AttributeValueUpdate()).withAction(AttributeAction.ADD).withValue((new AttributeValue()).withN("1"));

			try
			{
				t2.updateItem(new UpdateItemRequest()
					.withTableName(INTEG_HASH_TABLE_NAME).withKey(key).withAttributeUpdates(updates));
				fail();
			}
			catch (AmazonServiceException e)
			{
				assertEquals("ValidationException", e.ErrorCode);
				assertTrue(e.Message.contains("Type mismatch for attribute"));
			}

			assertItemLocked(INTEG_HASH_TABLE_NAME, key, t2.Id, false, false);

			try
			{
				t2.commit();
				fail();
			}
			catch (AmazonServiceException e)
			{
				assertEquals("ValidationException", e.ErrorCode);
				assertTrue(e.Message.contains("Type mismatch for attribute"));
			}

			assertItemLocked(INTEG_HASH_TABLE_NAME, key, t2.Id, false, false);

			t2.rollback();

			assertItemNotLocked(INTEG_HASH_TABLE_NAME, key, true);

			t1.delete(long.MaxValue);
			t2.delete(long.MaxValue);
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void useCommittedTransaction()
		public virtual void useCommittedTransaction()
		{
			IDictionary<string, AttributeValue> key1 = newKey(INTEG_HASH_TABLE_NAME);
			Transaction t1 = manager.newTransaction();
			t1.commit();

			DeleteItemRequest deleteRequest = (new DeleteItemRequest()).withTableName(INTEG_HASH_TABLE_NAME).withKey(key1);

			try
			{
				t1.deleteItem(deleteRequest);
				fail();
			}
			catch (TransactionCommittedException)
			{
			}

			assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, false);

			Transaction t2 = manager.resumeTransaction(t1.Id);

			try
			{
				t1.deleteItem(deleteRequest);
				fail();
			}
			catch (TransactionCommittedException)
			{
			}

			assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, false);

			try
			{
				t2.rollback();
				fail();
			}
			catch (TransactionCommittedException)
			{
			}

			t2.delete(long.MaxValue);
			t1.delete(long.MaxValue);
		}
		
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void useRolledBackTransaction()
		public virtual void useRolledBackTransaction()
		{
			IDictionary<string, AttributeValue> key1 = newKey(INTEG_HASH_TABLE_NAME);
			Transaction t1 = manager.newTransaction();
			t1.rollback();

			DeleteItemRequest deleteRequest = (new DeleteItemRequest()).withTableName(INTEG_HASH_TABLE_NAME).withKey(key1);

			try
			{
				t1.deleteItem(deleteRequest);
				fail();
			}
			catch (TransactionRolledBackException)
			{
			}

			assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, false);

			Transaction t2 = manager.resumeTransaction(t1.Id);

			try
			{
				t1.deleteItem(deleteRequest);
				fail();
			}
			catch (TransactionRolledBackException)
			{
			}

			assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, false);

			try
			{
				t2.commit();
				fail();
			}
			catch (TransactionRolledBackException)
			{
			}

			assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, false);

			Transaction t3 = manager.resumeTransaction(t1.Id);
			t3.rollback();

			Transaction t4 = manager.resumeTransaction(t1.Id);

			t2.delete(long.MaxValue);
			t1.delete(long.MaxValue);

			try
			{
				t4.deleteItem(deleteRequest);
				fail();
			}
			catch (TransactionNotFoundException)
			{
			}

			assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, false);

			t3.delete(long.MaxValue);
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void useDeletedTransaction()
		public virtual void useDeletedTransaction()
		{
			Transaction t1 = manager.newTransaction();
			Transaction t2 = manager.resumeTransaction(t1.Id);
			t1.commit();
			t1.delete(long.MaxValue);

			try
			{
				t2.commit();
				fail();
			}
			catch (UnknownCompletedTransactionException)
			{
			}

			t2.delete(long.MaxValue);

		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void driveCommit()
		public virtual void driveCommit()
		{
			Transaction t1 = manager.newTransaction();
			IDictionary<string, AttributeValue> key1 = newKey(INTEG_HASH_TABLE_NAME);
			IDictionary<string, AttributeValue> key2 = newKey(INTEG_HASH_TABLE_NAME);
			IDictionary<string, AttributeValue> item = new Dictionary<string, AttributeValue> (key1);
			item["attr"] = new AttributeValue("original");

			t1.putItem(new PutItemRequest()
				.withTableName(INTEG_HASH_TABLE_NAME).withItem(item));

			t1.commit();
			t1.delete(long.MaxValue);

			assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, item, true);
			assertItemNotLocked(INTEG_HASH_TABLE_NAME, key2, false);

			Transaction t2 = manager.newTransaction();

			item["attr2"] = new AttributeValue("new");
			t2.putItem(new PutItemRequest()
				.withTableName(INTEG_HASH_TABLE_NAME).withItem(item));

			t2.getItem(new GetItemRequest()
				.withTableName(INTEG_HASH_TABLE_NAME).withKey(key2));

			assertItemLocked(INTEG_HASH_TABLE_NAME, key1, item, t2.Id, false, true);
			assertItemLocked(INTEG_HASH_TABLE_NAME, key2, key2, t2.Id, true, false);

			Transaction commitFailingTransaction = new TransactionAnonymousInnerClass(this, t2.Id, manager);

			try
			{
				commitFailingTransaction.commit();
				fail();
			}
			catch (FailingAmazonDynamoDBClient.FailedYourRequestException)
			{
			}

			assertItemLocked(INTEG_HASH_TABLE_NAME, key1, item, t2.Id, false, true);
			assertItemLocked(INTEG_HASH_TABLE_NAME, key2, key2, t2.Id, true, false);

			t2.commit();

			assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, item, true);
			assertItemNotLocked(INTEG_HASH_TABLE_NAME, key2, false);

			commitFailingTransaction.commit();

			t2.delete(long.MaxValue);
		}

		private class TransactionAnonymousInnerClass : Transaction
		{
			private readonly TransactionsIntegrationTest outerInstance;

			public TransactionAnonymousInnerClass(TransactionsIntegrationTest outerInstance, UnknownType getId, UnknownType manager) : base(getId, manager, false)
			{
				this.outerInstance = outerInstance;
			}

			protected internal override void unlockItemAfterCommit(Request request)
			{
				throw new FailingAmazonDynamoDBClient.FailedYourRequestException();
			}
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void driveRollback()
		public virtual void driveRollback()
		{
			Transaction t1 = manager.newTransaction();
			IDictionary<string, AttributeValue> key1 = newKey(INTEG_HASH_TABLE_NAME);
			IDictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue> (key1);
			item1["attr1"] = new AttributeValue("original1");

			IDictionary<string, AttributeValue> key2 = newKey(INTEG_HASH_TABLE_NAME);
			IDictionary<string, AttributeValue> item2 = new Dictionary<string, AttributeValue> (key2);
			item1["attr2"] = new AttributeValue("original2");

			IDictionary<string, AttributeValue> key3 = newKey(INTEG_HASH_TABLE_NAME);

			t1.putItem(new PutItemRequest()
				.withTableName(INTEG_HASH_TABLE_NAME).withItem(item1));

			t1.putItem(new PutItemRequest()
				.withTableName(INTEG_HASH_TABLE_NAME).withItem(item2));

			t1.commit();
			t1.delete(long.MaxValue);

			assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, item1, true);
			assertItemNotLocked(INTEG_HASH_TABLE_NAME, key2, item2, true);

			Transaction t2 = manager.newTransaction();

			IDictionary<string, AttributeValue> item1a = new Dictionary<string, AttributeValue> (item1);
			item1a["attr1"] = new AttributeValue("new1");
			item1a["attr2"] = new AttributeValue("new1");

			t2.putItem(new PutItemRequest()
				.withTableName(INTEG_HASH_TABLE_NAME).withItem(item1a));

			t2.getItem(new GetItemRequest()
				.withTableName(INTEG_HASH_TABLE_NAME).withKey(key2));

			t2.getItem(new GetItemRequest()
				.withTableName(INTEG_HASH_TABLE_NAME).withKey(key3));

			assertItemLocked(INTEG_HASH_TABLE_NAME, key1, item1a, t2.Id, false, true);
			assertItemLocked(INTEG_HASH_TABLE_NAME, key2, item2, t2.Id, false, false);
			assertItemLocked(INTEG_HASH_TABLE_NAME, key3, key3, t2.Id, true, false);

			Transaction rollbackFailingTransaction = new TransactionAnonymousInnerClass2(this, t2.Id, manager);

			try
			{
				rollbackFailingTransaction.rollback();
				fail();
			}
			catch (FailingAmazonDynamoDBClient.FailedYourRequestException)
			{
			}

			assertItemLocked(INTEG_HASH_TABLE_NAME, key1, item1a, t2.Id, false, true);
			assertItemLocked(INTEG_HASH_TABLE_NAME, key2, item2, t2.Id, false, false);
			assertItemLocked(INTEG_HASH_TABLE_NAME, key3, key3, t2.Id, true, false);

			t2.rollback();

			assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, item1, true);
			assertItemNotLocked(INTEG_HASH_TABLE_NAME, key2, item2, true);
			assertItemNotLocked(INTEG_HASH_TABLE_NAME, key3, false);

			rollbackFailingTransaction.rollback();

			t2.delete(long.MaxValue);
		}

		private class TransactionAnonymousInnerClass2 : Transaction
		{
			private readonly TransactionsIntegrationTest outerInstance;

			public TransactionAnonymousInnerClass2(TransactionsIntegrationTest outerInstance, UnknownType getId, UnknownType manager) : base(getId, manager, false)
			{
				this.outerInstance = outerInstance;
			}

			protected internal override void rollbackItemAndReleaseLock(Request request)
			{
				throw new FailingAmazonDynamoDBClient.FailedYourRequestException();
			}
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void rollbackCompletedTransaction()
		public virtual void rollbackCompletedTransaction()
		{
			Transaction t1 = manager.newTransaction();
			Transaction rollbackFailingTransaction = new TransactionAnonymousInnerClass3(this, t1.Id, manager);

			IDictionary<string, AttributeValue> key1 = newKey(INTEG_HASH_TABLE_NAME);
			t1.putItem(new PutItemRequest()
				.withTableName(INTEG_HASH_TABLE_NAME).withItem(key1));
			assertItemLocked(INTEG_HASH_TABLE_NAME, key1, key1, t1.Id, true, true);

			t1.rollback();
			rollbackFailingTransaction.rollback();
		}

		private class TransactionAnonymousInnerClass3 : Transaction
		{
			private readonly TransactionsIntegrationTest outerInstance;

			public TransactionAnonymousInnerClass3(TransactionsIntegrationTest outerInstance, UnknownType getId, UnknownType manager) : base(getId, manager, false)
			{
				this.outerInstance = outerInstance;
			}

			protected internal override void doRollback()
			{
				throw new FailingAmazonDynamoDBClient.FailedYourRequestException();
			}
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void commitCompletedTransaction()
		public virtual void commitCompletedTransaction()
		{
			Transaction t1 = manager.newTransaction();
			Transaction commitFailingTransaction = new TransactionAnonymousInnerClass4(this, t1.Id, manager);

			IDictionary<string, AttributeValue> key1 = newKey(INTEG_HASH_TABLE_NAME);
			t1.putItem(new PutItemRequest()
				.withTableName(INTEG_HASH_TABLE_NAME).withItem(key1));
			assertItemLocked(INTEG_HASH_TABLE_NAME, key1, key1, t1.Id, true, true);

			t1.commit();
			commitFailingTransaction.commit();
		}

		private class TransactionAnonymousInnerClass4 : Transaction
		{
			private readonly TransactionsIntegrationTest outerInstance;

			public TransactionAnonymousInnerClass4(TransactionsIntegrationTest outerInstance, UnknownType getId, UnknownType manager) : base(getId, manager, false)
			{
				this.outerInstance = outerInstance;
			}

			protected internal override void doCommit()
			{
				throw new FailingAmazonDynamoDBClient.FailedYourRequestException();
			}
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void resumePendingTransaction()
		public virtual void resumePendingTransaction()
		{
			Transaction t1 = manager.newTransaction();

			IDictionary<string, AttributeValue> key1 = newKey(INTEG_HASH_TABLE_NAME);
			IDictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue> (key1);
			item1["attr1"] = new AttributeValue("original1");

			IDictionary<string, AttributeValue> key2 = newKey(INTEG_HASH_TABLE_NAME);

			t1.putItem(new PutItemRequest()
				.withTableName(INTEG_HASH_TABLE_NAME).withItem(item1));

			assertItemLocked(INTEG_HASH_TABLE_NAME, key1, item1, t1.Id, true, true);
			assertOldItemImage(t1.Id, INTEG_HASH_TABLE_NAME, key1, key1, false);

			Transaction t2 = manager.resumeTransaction(t1.Id);

			t2.getItem(new GetItemRequest()
				.withTableName(INTEG_HASH_TABLE_NAME).withKey(key2));

			assertItemLocked(INTEG_HASH_TABLE_NAME, key1, item1, t1.Id, true, true);
			assertOldItemImage(t1.Id, INTEG_HASH_TABLE_NAME, key1, key1, false);
			assertItemLocked(INTEG_HASH_TABLE_NAME, key2, t1.Id, true, false);
			assertOldItemImage(t1.Id, INTEG_HASH_TABLE_NAME, key2, null, false);

			t2.commit();

			assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, true);
			assertItemNotLocked(INTEG_HASH_TABLE_NAME, key2, false);

			assertOldItemImage(t1.Id, INTEG_HASH_TABLE_NAME, key1, null, false);
			assertOldItemImage(t1.Id, INTEG_HASH_TABLE_NAME, key2, null, false);

			t2.delete(long.MaxValue);
			assertTransactionDeleted(t2);
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void resumeAndCommitAfterTransientApplyFailure()
		public virtual void resumeAndCommitAfterTransientApplyFailure()
		{
			Transaction t1 = new TransactionAnonymousInnerClass5(this, UUID.randomUUID().ToString(), manager);

			IDictionary<string, AttributeValue> key1 = newKey(INTEG_HASH_TABLE_NAME);
			IDictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue> (key1);
			item1["attr1"] = new AttributeValue("original1");

			IDictionary<string, AttributeValue> key2 = newKey(INTEG_HASH_TABLE_NAME);

			try
			{
				t1.putItem(new PutItemRequest()
					.withTableName(INTEG_HASH_TABLE_NAME).withItem(item1));
				fail();
			}
			catch (FailingAmazonDynamoDBClient.FailedYourRequestException)
			{
			}

			assertItemLocked(INTEG_HASH_TABLE_NAME, key1, key1, t1.Id, true, false);
			assertOldItemImage(t1.Id, INTEG_HASH_TABLE_NAME, key1, key1, false);

			Transaction t2 = manager.resumeTransaction(t1.Id);

			t2.getItem(new GetItemRequest()
				.withTableName(INTEG_HASH_TABLE_NAME).withKey(key2));

			assertItemLocked(INTEG_HASH_TABLE_NAME, key1, item1, t1.Id, true, true);
			assertOldItemImage(t1.Id, INTEG_HASH_TABLE_NAME, key1, key1, false);
			assertItemLocked(INTEG_HASH_TABLE_NAME, key2, t1.Id, true, false);
			assertOldItemImage(t1.Id, INTEG_HASH_TABLE_NAME, key2, null, false);

			Transaction t3 = manager.resumeTransaction(t1.Id);
			t3.commit();

			assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, item1, true);
			assertItemNotLocked(INTEG_HASH_TABLE_NAME, key2, false);

			assertOldItemImage(t1.Id, INTEG_HASH_TABLE_NAME, key1, null, false);
			assertOldItemImage(t1.Id, INTEG_HASH_TABLE_NAME, key2, null, false);

			t3.commit();

			t3.delete(long.MaxValue);
			assertTransactionDeleted(t2);
		}

		private class TransactionAnonymousInnerClass5 : Transaction
		{
			private readonly TransactionsIntegrationTest outerInstance;

			public TransactionAnonymousInnerClass5(TransactionsIntegrationTest outerInstance, string toString, UnknownType manager) : base(toString, manager, true)
			{
				this.outerInstance = outerInstance;
			}

			protected internal override IDictionary<string, AttributeValue> applyAndKeepLock(Request request, IDictionary<string, AttributeValue> lockedItem)
			{
				throw new FailingAmazonDynamoDBClient.FailedYourRequestException();
			}
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void applyOnlyOnce()
		public virtual void applyOnlyOnce()
		{
			Transaction t1 = new TransactionAnonymousInnerClass6(this, UUID.randomUUID().ToString(), manager);

			IDictionary<string, AttributeValue> key1 = newKey(INTEG_HASH_TABLE_NAME);
			IDictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue> (key1);
			item1["attr1"] = (new AttributeValue()).withN("1");

			IDictionary<string, AttributeValue> key2 = newKey(INTEG_HASH_TABLE_NAME);

			IDictionary<string, AttributeValueUpdate> updates = new Dictionary<string, AttributeValueUpdate>();
			updates["attr1"] = (new AttributeValueUpdate()).withAction(AttributeAction.ADD).withValue((new AttributeValue()).withN("1"));

			UpdateItemRequest update = (new UpdateItemRequest()).withTableName(INTEG_HASH_TABLE_NAME).withAttributeUpdates(updates).withKey(key1);

			try
			{
				t1.updateItem(update);
				fail();
			}
			catch (FailingAmazonDynamoDBClient.FailedYourRequestException)
			{
			}

			assertItemLocked(INTEG_HASH_TABLE_NAME, key1, item1, t1.Id, true, true);
			assertOldItemImage(t1.Id, INTEG_HASH_TABLE_NAME, key1, key1, false);

			Transaction t2 = manager.resumeTransaction(t1.Id);

			t2.getItem(new GetItemRequest()
				.withTableName(INTEG_HASH_TABLE_NAME).withKey(key2));

			assertItemLocked(INTEG_HASH_TABLE_NAME, key1, item1, t1.Id, true, true);
			assertOldItemImage(t1.Id, INTEG_HASH_TABLE_NAME, key1, key1, false);
			assertItemLocked(INTEG_HASH_TABLE_NAME, key2, t1.Id, true, false);
			assertOldItemImage(t1.Id, INTEG_HASH_TABLE_NAME, key2, null, false);

			t2.commit();

			assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, item1, true);
			assertItemNotLocked(INTEG_HASH_TABLE_NAME, key2, false);

			assertOldItemImage(t1.Id, INTEG_HASH_TABLE_NAME, key1, null, false);
			assertOldItemImage(t1.Id, INTEG_HASH_TABLE_NAME, key2, null, false);

			t2.delete(long.MaxValue);
			assertTransactionDeleted(t2);
		}

		private class TransactionAnonymousInnerClass6 : Transaction
		{
			private readonly TransactionsIntegrationTest outerInstance;

			public TransactionAnonymousInnerClass6(TransactionsIntegrationTest outerInstance, string toString, UnknownType manager) : base(toString, manager, true)
			{
				this.outerInstance = outerInstance;
			}

			protected internal override IDictionary<string, AttributeValue> applyAndKeepLock(Request request, IDictionary<string, AttributeValue> lockedItem)
			{
				base.applyAndKeepLock(request, lockedItem);
				throw new FailingAmazonDynamoDBClient.FailedYourRequestException();
			}
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void resumeRollbackAfterTransientApplyFailure()
		public virtual void resumeRollbackAfterTransientApplyFailure()
		{
			Transaction t1 = new TransactionAnonymousInnerClass7(this, UUID.randomUUID().ToString(), manager);

			IDictionary<string, AttributeValue> key1 = newKey(INTEG_HASH_TABLE_NAME);
			IDictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue> (key1);
			item1["attr1"] = new AttributeValue("original1");

			IDictionary<string, AttributeValue> key2 = newKey(INTEG_HASH_TABLE_NAME);

			try
			{
				t1.putItem(new PutItemRequest()
					.withTableName(INTEG_HASH_TABLE_NAME).withItem(item1));
				fail();
			}
			catch (FailingAmazonDynamoDBClient.FailedYourRequestException)
			{
			}

			assertItemLocked(INTEG_HASH_TABLE_NAME, key1, key1, t1.Id, true, false);
			assertOldItemImage(t1.Id, INTEG_HASH_TABLE_NAME, key1, key1, false);

			Transaction t2 = manager.resumeTransaction(t1.Id);

			t2.getItem(new GetItemRequest()
				.withTableName(INTEG_HASH_TABLE_NAME).withKey(key2));

			assertItemLocked(INTEG_HASH_TABLE_NAME, key1, item1, t1.Id, true, true);
			assertOldItemImage(t1.Id, INTEG_HASH_TABLE_NAME, key1, key1, false);
			assertItemLocked(INTEG_HASH_TABLE_NAME, key2, t1.Id, true, false);
			assertOldItemImage(t1.Id, INTEG_HASH_TABLE_NAME, key2, null, false);

			Transaction t3 = manager.resumeTransaction(t1.Id);
			t3.rollback();

			assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, false);
			assertItemNotLocked(INTEG_HASH_TABLE_NAME, key2, false);

			assertOldItemImage(t1.Id, INTEG_HASH_TABLE_NAME, key1, null, false);
			assertOldItemImage(t1.Id, INTEG_HASH_TABLE_NAME, key2, null, false);

			t3.delete(long.MaxValue);
			assertTransactionDeleted(t2);
		}

		private class TransactionAnonymousInnerClass7 : Transaction
		{
			private readonly TransactionsIntegrationTest outerInstance;

			public TransactionAnonymousInnerClass7(TransactionsIntegrationTest outerInstance, string toString, UnknownType manager) : base(toString, manager, true)
			{
				this.outerInstance = outerInstance;
			}

			protected internal override IDictionary<string, AttributeValue> applyAndKeepLock(Request request, IDictionary<string, AttributeValue> lockedItem)
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
//ORIGINAL LINE: final Transaction t1 = new Transaction(java.util.UUID.randomUUID().toString(), manager, true)
			Transaction t1 = new TransactionAnonymousInnerClass8(this, UUID.randomUUID().ToString(), manager);

			// Change the existing item key0, failing when trying to save away the item image
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> item0a = new java.util.HashMap<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> (item0);
			IDictionary<string, AttributeValue> item0a = new Dictionary<string, AttributeValue> (item0);
			item0a["attr1"] = new AttributeValue("original1");

			try
			{
				t1.putItem(new PutItemRequest()
					.withTableName(INTEG_HASH_TABLE_NAME).withItem(item0a));
				fail();
			}
			catch (FailingAmazonDynamoDBClient.FailedYourRequestException)
			{
			}

			assertItemLocked(INTEG_HASH_TABLE_NAME, key0, item0, t1.Id, false, false);

			// Roll back, and ensure the item was reverted correctly
			t1.rollback();

			assertItemNotLocked(INTEG_HASH_TABLE_NAME, key0, item0, true);
		}

		private class TransactionAnonymousInnerClass8 : Transaction
		{
			private readonly TransactionsIntegrationTest outerInstance;

			public TransactionAnonymousInnerClass8(TransactionsIntegrationTest outerInstance, string toString, UnknownType manager) : base(toString, manager, true)
			{
				this.outerInstance = outerInstance;
			}

			protected internal override void saveItemImage(Request callerRequest, IDictionary<string, AttributeValue> item)
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
//ORIGINAL LINE: final java.util.concurrent.Semaphore barrier = new java.util.concurrent.Semaphore(0);
			Semaphore barrier = new Semaphore(0);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Transaction t1 = new Transaction(java.util.UUID.randomUUID().toString(), manager, true)
			Transaction t1 = new TransactionAnonymousInnerClass9(this, UUID.randomUUID().ToString(), manager, barrier);

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> key1 = newKey(INTEG_HASH_TABLE_NAME);
			IDictionary<string, AttributeValue> key1 = newKey(INTEG_HASH_TABLE_NAME);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> item1 = new java.util.HashMap<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> (key1);
			IDictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue> (key1);
			item1["attr1"] = new AttributeValue("original1");

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.concurrent.Semaphore caughtRolledBackException = new java.util.concurrent.Semaphore(0);
			Semaphore caughtRolledBackException = new Semaphore(0);

			Thread thread = new Thread(() =>
		{
			try
			{
				t1.putItem(new PutItemRequest()
					.withTableName(INTEG_HASH_TABLE_NAME).withItem(item1));
			}
			catch (TransactionRolledBackException)
			{
				caughtRolledBackException.release();
			}
		});

			thread.Start();

			assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, false);
			Transaction t2 = manager.resumeTransaction(t1.Id);
			t2.rollback();
			assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, false);

			barrier.release(100);

			thread.Join();

			assertEquals(1, caughtRolledBackException.availablePermits());

			assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, false);
			assertTrue(t1.delete(long.MinValue));

			// Now start a new transaction involving key1 and make sure it will complete
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> item1a = new java.util.HashMap<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> (key1);
			IDictionary<string, AttributeValue> item1a = new Dictionary<string, AttributeValue> (key1);
			item1a["attr1"] = new AttributeValue("new");

			Transaction t3 = manager.newTransaction();
			t3.putItem(new PutItemRequest()
				.withTableName(INTEG_HASH_TABLE_NAME).withItem(item1a));
			assertItemLocked(INTEG_HASH_TABLE_NAME, key1, item1a, t3.Id, true, true);
			t3.commit();
			assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, item1a, true);
		}

		private class TransactionAnonymousInnerClass9 : Transaction
		{
			private readonly TransactionsIntegrationTest outerInstance;

			private Semaphore barrier;

			public TransactionAnonymousInnerClass9(TransactionsIntegrationTest outerInstance, string toString, UnknownType manager, Semaphore barrier) : base(toString, manager, true)
			{
				this.outerInstance = outerInstance;
				this.barrier = barrier;
			}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override protected java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> lockItem(Request callerRequest, boolean expectExists, int attempts) throws com.amazonaws.services.dynamodbv2.transactions.exceptions.TransactionException
			protected internal override IDictionary<string, AttributeValue> lockItem(Request callerRequest, bool expectExists, int attempts)
			{
				try
				{
					barrier.acquire();
				}
				catch (InterruptedException e)
				{
					throw new Exception(e);
				}
				return base.lockItem(callerRequest, expectExists, attempts);
			}
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void shouldNotApplyAfterRollbackAndDeleted() throws InterruptedException
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
		public virtual void shouldNotApplyAfterRollbackAndDeleted()
		{
			// Very similar to "shouldNotApplyAfterRollback" except the transaction is rolled back and then deleted.
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.concurrent.Semaphore barrier = new java.util.concurrent.Semaphore(0);
			Semaphore barrier = new Semaphore(0);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Transaction t1 = new Transaction(java.util.UUID.randomUUID().toString(), manager, true)
			Transaction t1 = new TransactionAnonymousInnerClass10(this, UUID.randomUUID().ToString(), manager, barrier);

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> key1 = newKey(INTEG_HASH_TABLE_NAME);
			IDictionary<string, AttributeValue> key1 = newKey(INTEG_HASH_TABLE_NAME);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> item1 = new java.util.HashMap<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> (key1);
			IDictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue> (key1);
			item1["attr1"] = new AttributeValue("original1");

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.concurrent.Semaphore caughtTransactionNotFoundException = new java.util.concurrent.Semaphore(0);
			Semaphore caughtTransactionNotFoundException = new Semaphore(0);

			Thread thread = new Thread(() =>
		{
			try
			{
				t1.putItem(new PutItemRequest()
					.withTableName(INTEG_HASH_TABLE_NAME).withItem(item1));
			}
			catch (TransactionNotFoundException)
			{
				caughtTransactionNotFoundException.release();
			}
		});

			thread.Start();

			assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, false);
			Transaction t2 = manager.resumeTransaction(t1.Id);
			t2.rollback();
			assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, false);
			assertTrue(t2.delete(long.MinValue)); // This is the key difference with shouldNotApplyAfterRollback

			barrier.release(100);

			thread.Join();

			assertEquals(1, caughtTransactionNotFoundException.availablePermits());

			assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, false);

			// Now start a new transaction involving key1 and make sure it will complete
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> item1a = new java.util.HashMap<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> (key1);
			IDictionary<string, AttributeValue> item1a = new Dictionary<string, AttributeValue> (key1);
			item1a["attr1"] = new AttributeValue("new");

			Transaction t3 = manager.newTransaction();
			t3.putItem(new PutItemRequest()
				.withTableName(INTEG_HASH_TABLE_NAME).withItem(item1a));
			assertItemLocked(INTEG_HASH_TABLE_NAME, key1, item1a, t3.Id, true, true);
			t3.commit();
			assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, item1a, true);
		}

		private class TransactionAnonymousInnerClass10 : Transaction
		{
			private readonly TransactionsIntegrationTest outerInstance;

			private Semaphore barrier;

			public TransactionAnonymousInnerClass10(TransactionsIntegrationTest outerInstance, string toString, UnknownType manager, Semaphore barrier) : base(toString, manager, true)
			{
				this.outerInstance = outerInstance;
				this.barrier = barrier;
			}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override protected java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> lockItem(Request callerRequest, boolean expectExists, int attempts) throws com.amazonaws.services.dynamodbv2.transactions.exceptions.TransactionException
			protected internal override IDictionary<string, AttributeValue> lockItem(Request callerRequest, bool expectExists, int attempts)
			{
				try
				{
					barrier.acquire();
				}
				catch (InterruptedException e)
				{
					throw new Exception(e);
				}
				return base.lockItem(callerRequest, expectExists, attempts);
			}
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void shouldNotApplyAfterRollbackAndDeletedAndLeftLocked() throws InterruptedException
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
		public virtual void shouldNotApplyAfterRollbackAndDeletedAndLeftLocked()
		{

			// Very similar to "shouldNotApplyAfterRollbackAndDeleted" except the lock is broken by a new transaction, not t1
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.concurrent.Semaphore barrier = new java.util.concurrent.Semaphore(0);
			Semaphore barrier = new Semaphore(0);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Transaction t1 = new Transaction(java.util.UUID.randomUUID().toString(), manager, true)
			Transaction t1 = new TransactionAnonymousInnerClass11(this, UUID.randomUUID().ToString(), manager, barrier);

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> key1 = newKey(INTEG_HASH_TABLE_NAME);
			IDictionary<string, AttributeValue> key1 = newKey(INTEG_HASH_TABLE_NAME);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> item1 = new java.util.HashMap<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> (key1);
			IDictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue> (key1);
			item1["attr1"] = new AttributeValue("original1");

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.concurrent.Semaphore caughtFailedYourRequestException = new java.util.concurrent.Semaphore(0);
			Semaphore caughtFailedYourRequestException = new Semaphore(0);

			Thread thread = new Thread(() =>
		{
			try
			{
				t1.putItem(new PutItemRequest()
					.withTableName(INTEG_HASH_TABLE_NAME).withItem(item1));
			}
			catch (FailingAmazonDynamoDBClient.FailedYourRequestException)
			{
				caughtFailedYourRequestException.release();
			}
		});

			thread.Start();

			assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, false);
			Transaction t2 = manager.resumeTransaction(t1.Id);
			t2.rollback();
			assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, false);
			assertTrue(t2.delete(long.MinValue));

			barrier.release(100);

			thread.Join();

			assertEquals(1, caughtFailedYourRequestException.availablePermits());

			assertItemLocked(INTEG_HASH_TABLE_NAME, key1, null, t1.Id, true, false, false); // locked and "null", but don't check the tx item

			// Now start a new transaction involving key1 and make sure it will complete
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> item1a = new java.util.HashMap<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> (key1);
			IDictionary<string, AttributeValue> item1a = new Dictionary<string, AttributeValue> (key1);
			item1a["attr1"] = new AttributeValue("new");

			Transaction t3 = manager.newTransaction();
			t3.putItem(new PutItemRequest()
				.withTableName(INTEG_HASH_TABLE_NAME).withItem(item1a));
			assertItemLocked(INTEG_HASH_TABLE_NAME, key1, item1a, t3.Id, true, true);
			t3.commit();
			assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, item1a, true);
		}

		private class TransactionAnonymousInnerClass11 : Transaction
		{
			private readonly TransactionsIntegrationTest outerInstance;

			private Semaphore barrier;

			public TransactionAnonymousInnerClass11(TransactionsIntegrationTest outerInstance, string toString, UnknownType manager, Semaphore barrier) : base(toString, manager, true)
			{
				this.outerInstance = outerInstance;
				this.barrier = barrier;
			}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override protected java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> lockItem(Request callerRequest, boolean expectExists, int attempts) throws com.amazonaws.services.dynamodbv2.transactions.exceptions.TransactionException
			protected internal override IDictionary<string, AttributeValue> lockItem(Request callerRequest, bool expectExists, int attempts)
			{
				try
				{
					barrier.acquire();
				}
				catch (InterruptedException e)
				{
					throw new Exception(e);
				}
				return base.lockItem(callerRequest, expectExists, attempts);
			}

			protected internal override void releaseReadLock(string tableName, IDictionary<string, AttributeValue> key)
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

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Transaction t1 = new Transaction(java.util.UUID.randomUUID().toString(), manager, true)
			Transaction t1 = new TransactionAnonymousInnerClass12(this, UUID.randomUUID().ToString(), manager, shouldThrowAfterApply);

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> key1 = newKey(INTEG_HASH_TABLE_NAME);
			IDictionary<string, AttributeValue> key1 = newKey(INTEG_HASH_TABLE_NAME);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> key2 = newKey(INTEG_HASH_TABLE_NAME);
			IDictionary<string, AttributeValue> key2 = newKey(INTEG_HASH_TABLE_NAME);

			// Read an item that doesn't exist to get its read lock
			IDictionary<string, AttributeValue> item1Returned = t1.getItem(new GetItemRequest(INTEG_HASH_TABLE_NAME, key1, true)).Item;
			assertNull(item1Returned);
			assertItemLocked(INTEG_HASH_TABLE_NAME, key1, t1.Id, true, false);

			// Now start another transaction that is going to try to read that same item,
			// but stop after you read the competing transaction record (don't try to roll it back yet)

			// t2 waits on this for the main thread to signal it.
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.concurrent.Semaphore waitAfterResumeTransaction = new java.util.concurrent.Semaphore(0);
			Semaphore waitAfterResumeTransaction = new Semaphore(0);

			// the main thread waits on this for t2 to signal that it's ready
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.concurrent.Semaphore resumedTransaction = new java.util.concurrent.Semaphore(0);
			Semaphore resumedTransaction = new Semaphore(0);

			// the main thread waits on this for t2 to finish with its rollback of t1
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.concurrent.Semaphore rolledBackT1 = new java.util.concurrent.Semaphore(0);
			Semaphore rolledBackT1 = new Semaphore(0);

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final TransactionManager manager = new TransactionManager(dynamodb, INTEG_LOCK_TABLE_NAME, INTEG_IMAGES_TABLE_NAME)
			TransactionManager manager = new TransactionManagerAnonymousInnerClass(this, dynamodb, INTEG_LOCK_TABLE_NAME, INTEG_IMAGES_TABLE_NAME, waitAfterResumeTransaction, resumedTransaction);

			Thread thread = new Thread(() =>
		{
			Transaction t2 = new TransactionAnonymousInnerClass13(this, UUID.randomUUID().ToString(), manager);
			// This will stop pause on waitAfterResumeTransaction once it finds that key1 is already locked by t1.
			IDictionary<string, AttributeValue> item1Returned = t2.getItem(new GetItemRequest(INTEG_HASH_TABLE_NAME, key1, true)).Item;
			assertNull(item1Returned);
			rolledBackT1.release();
		});
			thread.Start();

			// Wait for t2 to get to the point where it loaded the t1 tx record.
			resumedTransaction.acquire();

			// Now change that getItem to an updateItem in t1
			IDictionary<string, AttributeValueUpdate> updates = new Dictionary<string, AttributeValueUpdate>();
			updates["asdf"] = new AttributeValueUpdate(new AttributeValue("wef"), AttributeAction.PUT);
			t1.updateItem(new UpdateItemRequest(INTEG_HASH_TABLE_NAME, key1, updates));

			// Now let t2 continue on and roll back t1
			waitAfterResumeTransaction.release();

			// Wait for t2 to finish rolling back t1
			rolledBackT1.acquire();

			// T1 should be rolled back now and unable to do stuff
			try
			{
				t1.getItem(new GetItemRequest(INTEG_HASH_TABLE_NAME, key2, true)).Item;
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

			public TransactionAnonymousInnerClass12(TransactionsIntegrationTest outerInstance, string toString, UnknownType manager, AtomicBoolean shouldThrowAfterApply) : base(toString, manager, true)
			{
				this.outerInstance = outerInstance;
				this.shouldThrowAfterApply = shouldThrowAfterApply;
			}

			protected internal override IDictionary<string, AttributeValue> applyAndKeepLock(Request request, IDictionary<string, AttributeValue> lockedItem)
			{
				IDictionary<string, AttributeValue> toReturn = base.applyAndKeepLock(request, lockedItem);
				if (shouldThrowAfterApply.get())
				{
					throw new Exception("throwing as desired");
				}
				return toReturn;
			}
		}

		private class TransactionManagerAnonymousInnerClass : TransactionManager
		{
			private readonly TransactionsIntegrationTest outerInstance;

			private Semaphore waitAfterResumeTransaction;
			private Semaphore resumedTransaction;

			public TransactionManagerAnonymousInnerClass(TransactionsIntegrationTest outerInstance, UnknownType dynamodb, UnknownType INTEG_LOCK_TABLE_NAME, UnknownType INTEG_IMAGES_TABLE_NAME, Semaphore waitAfterResumeTransaction, Semaphore resumedTransaction) : base(dynamodb, INTEG_LOCK_TABLE_NAME, INTEG_IMAGES_TABLE_NAME)
			{
				this.outerInstance = outerInstance;
				this.waitAfterResumeTransaction = waitAfterResumeTransaction;
				this.resumedTransaction = resumedTransaction;
			}

			public override Transaction resumeTransaction(string txId)
			{
				Transaction t = base.resumeTransaction(txId);

				// Signal to the main thread that t2 has loaded the tx record.
				resumedTransaction.release();

				try
				{
					// Wait for the main thread to upgrade key1 to a write lock (but we won't know about it)
					waitAfterResumeTransaction.acquire();
				}
				catch (InterruptedException e)
				{
					throw new Exception(e);
				}
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
			IDictionary<string, AttributeValue> key1 = newKey(INTEG_HASH_TABLE_NAME);

			t1.updateItem(new UpdateItemRequest()
				.withTableName(INTEG_HASH_TABLE_NAME).withKey(key1));
			assertItemLocked(INTEG_HASH_TABLE_NAME, key1, t1.Id, true, true);

			t1.rollback();
			assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, false);

			t1.delete(long.MaxValue);
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void basicNewItemCommit()
		public virtual void basicNewItemCommit()
		{
			Transaction t1 = manager.newTransaction();
			IDictionary<string, AttributeValue> key1 = newKey(INTEG_HASH_TABLE_NAME);

			t1.updateItem(new UpdateItemRequest()
				.withTableName(INTEG_HASH_TABLE_NAME).withKey(key1));
			assertItemLocked(INTEG_HASH_TABLE_NAME, key1, t1.Id, true, true);

			t1.commit();
			assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, key1, true);
			t1.delete(long.MaxValue);
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void missingTableName()
		public virtual void missingTableName()
		{
			Transaction t1 = manager.newTransaction();
			IDictionary<string, AttributeValue> key1 = newKey(INTEG_HASH_TABLE_NAME);

			try
			{
				t1.updateItem(new UpdateItemRequest()
					.withKey(key1));
				fail();
			}
			catch (InvalidRequestException e)
			{
				assertTrue(e.Message, e.Message.contains("TableName must not be null"));
			}
			assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, false);
			t1.rollback();
			assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, false);
			t1.delete(long.MaxValue);
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void emptyTransaction()
		public virtual void emptyTransaction()
		{
			Transaction t1 = manager.newTransaction();
			t1.commit();
			t1.delete(long.MaxValue);
			assertTransactionDeleted(t1);
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void missingKey()
		public virtual void missingKey()
		{
			Transaction t1 = manager.newTransaction();
			try
			{
				t1.updateItem(new UpdateItemRequest()
					.withTableName(INTEG_HASH_TABLE_NAME));
				fail();
			}
			catch (InvalidRequestException e)
			{
				assertTrue(e.Message, e.Message.contains("The request key cannot be empty"));
			}
			t1.rollback();
			t1.delete(long.MaxValue);
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
			IDictionary<string, AttributeValue> key1 = newKey(INTEG_HASH_TABLE_NAME);
			IDictionary<string, AttributeValue> key2 = newKey(INTEG_HASH_TABLE_NAME);

			// Write item 1 as a starting point
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < (MAX_ITEM_SIZE_BYTES / 1.5); i++)
			{
				sb.Append("a");
			}
			string bigString = sb.ToString();

			IDictionary<string, AttributeValue> item1 = new Dictionary<string, AttributeValue>(key1);
			item1["bigattr"] = new AttributeValue("little");
			t1.putItem(new PutItemRequest()
				.withTableName(INTEG_HASH_TABLE_NAME).withItem(item1));

			assertItemLocked(INTEG_HASH_TABLE_NAME, key1, item1, t1.Id, true, true);

			t1.commit();

			assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, item1, true);

			IDictionary<string, AttributeValue> item1a = new Dictionary<string, AttributeValue>(key1);
			item1a["bigattr"] = new AttributeValue(bigString);

			t2.putItem(new PutItemRequest()
				.withTableName(INTEG_HASH_TABLE_NAME).withItem(item1a));

			assertItemLocked(INTEG_HASH_TABLE_NAME, key1, item1a, t2.Id, false, true);

			IDictionary<string, AttributeValue> item2 = new Dictionary<string, AttributeValue>(key2);
			item2["bigattr"] = new AttributeValue(bigString);

			try
			{
				t2.putItem(new PutItemRequest()
					.withTableName(INTEG_HASH_TABLE_NAME).withItem(item2));
				fail();
			}
			catch (InvalidRequestException)
			{
			}

			assertItemNotLocked(INTEG_HASH_TABLE_NAME, key2, false);
			assertItemLocked(INTEG_HASH_TABLE_NAME, key1, item1a, t2.Id, false, true);

			item2["bigattr"] = new AttributeValue("fitsThisTime");
			t2.putItem(new PutItemRequest()
				.withTableName(INTEG_HASH_TABLE_NAME).withItem(item2));

			assertItemLocked(INTEG_HASH_TABLE_NAME, key2, item2, t2.Id, true, true);

			t2.commit();

			assertItemNotLocked(INTEG_HASH_TABLE_NAME, key1, item1a, true);
			assertItemNotLocked(INTEG_HASH_TABLE_NAME, key2, item2, true);

			t1.delete(long.MaxValue);
			t2.delete(long.MaxValue);
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void containsBinaryAttributes()
		public virtual void containsBinaryAttributes()
		{
			Transaction t1 = manager.newTransaction();
			IDictionary<string, AttributeValue> key = newKey(INTEG_HASH_TABLE_NAME);
			IDictionary<string, AttributeValue> item = new Dictionary<string, AttributeValue>(key);

			item["attr_b"] = (new AttributeValue()).withB(ByteBuffer.wrap("asdf\n\t\u0123".GetBytes()));
			item["attr_bs"] = (new AttributeValue()).withBS(ByteBuffer.wrap("asdf\n\t\u0123".GetBytes()), ByteBuffer.wrap("wef".GetBytes()));

			t1.putItem(new PutItemRequest()
					.withTableName(INTEG_HASH_TABLE_NAME).withItem(item));

			assertItemLocked(INTEG_HASH_TABLE_NAME, key, item, t1.Id, true, true);

			t1.commit();

			assertItemNotLocked(INTEG_HASH_TABLE_NAME, key, item, true);
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void containsJSONAttributes()
		public virtual void containsJSONAttributes()
		{
			Transaction t1 = manager.newTransaction();
			IDictionary<string, AttributeValue> key = newKey(INTEG_HASH_TABLE_NAME);
			IDictionary<string, AttributeValue> item = new Dictionary<string, AttributeValue>(key);

			item["attr_json"] = (new AttributeValue()).withM(JSON_M_ATTR_VAL);

			t1.putItem(new PutItemRequest()
				.withTableName(INTEG_HASH_TABLE_NAME).withItem(item));

			assertItemLocked(INTEG_HASH_TABLE_NAME, key, item, t1.Id, true, true);

			t1.commit();

			assertItemNotLocked(INTEG_HASH_TABLE_NAME, key, item, true);
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void containsSpecialAttributes()
		public virtual void containsSpecialAttributes()
		{
			Transaction t1 = manager.newTransaction();
			IDictionary<string, AttributeValue> key = newKey(INTEG_HASH_TABLE_NAME);
			IDictionary<string, AttributeValue> item = new Dictionary<string, AttributeValue>(key);
			item[Transaction.AttributeName.TXID.ToString()] = new AttributeValue("asdf");

			try
			{
				t1.putItem(new PutItemRequest()
					.withTableName(INTEG_HASH_TABLE_NAME).withItem(item));
				fail();
			}
			catch (InvalidRequestException e)
			{
				assertTrue(e.Message.contains("must not contain the reserved"));
			}

			item[Transaction.AttributeName.TRANSIENT.ToString()] = new AttributeValue("asdf");
			item.Remove(Transaction.AttributeName.TXID.ToString());

			try
			{
				t1.putItem(new PutItemRequest()
					.withTableName(INTEG_HASH_TABLE_NAME).withItem(item));
				fail();
			}
			catch (InvalidRequestException e)
			{
				assertTrue(e.Message.contains("must not contain the reserved"));
			}

			item[Transaction.AttributeName.APPLIED.ToString()] = new AttributeValue("asdf");
			item.Remove(Transaction.AttributeName.TRANSIENT.ToString());

			try
			{
				t1.putItem(new PutItemRequest()
					.withTableName(INTEG_HASH_TABLE_NAME).withItem(item));
				fail();
			}
			catch (InvalidRequestException e)
			{
				assertTrue(e.Message.contains("must not contain the reserved"));
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
			Transaction t1 = new TransactionAnonymousInnerClass14(this, UUID.randomUUID().ToString(), manager);

			// prepare a request
			UpdateItemRequest callerRequest = (new UpdateItemRequest()).withTableName(INTEG_HASH_TABLE_NAME).withKey(newKey(INTEG_HASH_TABLE_NAME));

			try
			{
				t1.updateItem(callerRequest);
				fail();
			}
			catch (FailingAmazonDynamoDBClient.FailedYourRequestException)
			{
			}
			assertItemNotLocked(INTEG_HASH_TABLE_NAME, callerRequest.Key, false);

			// The non-failing manager
			Transaction t2 = manager.resumeTransaction(t1.Id);
			t2.commit();

			assertItemNotLocked(INTEG_HASH_TABLE_NAME, callerRequest.Key, true);

			// If this attempted to apply again, this would fail because of the failing client
			t1.commit();

			assertItemNotLocked(INTEG_HASH_TABLE_NAME, callerRequest.Key, true);

			t1.delete(long.MaxValue);
			t2.delete(long.MaxValue);
		}

		private class TransactionAnonymousInnerClass14 : Transaction
		{
			private readonly TransactionsIntegrationTest outerInstance;

			public TransactionAnonymousInnerClass14(TransactionsIntegrationTest outerInstance, string toString, UnknownType manager) : base(toString, manager, true)
			{
				this.outerInstance = outerInstance;
			}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override protected java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> lockItem(Request callerRequest, boolean expectExists, int attempts) throws com.amazonaws.services.dynamodbv2.transactions.exceptions.TransactionException
			protected internal override IDictionary<string, AttributeValue> lockItem(Request callerRequest, bool expectExists, int attempts)
			{

				throw new FailingAmazonDynamoDBClient.FailedYourRequestException();
			}
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void oneTransactionPerItem()
		public virtual void oneTransactionPerItem()
		{
			Transaction transaction = manager.newTransaction();
			IDictionary<string, AttributeValue> key = newKey(INTEG_HASH_TABLE_NAME);

			transaction.putItem(new PutItemRequest()
				.withTableName(INTEG_HASH_TABLE_NAME).withItem(key));
			try
			{
				transaction.putItem(new PutItemRequest()
					.withTableName(INTEG_HASH_TABLE_NAME).withItem(key));
				fail();
			}
			catch (DuplicateRequestException)
			{
				transaction.rollback();
			}
			assertItemNotLocked(INTEG_HASH_TABLE_NAME, key, false);
			transaction.delete(long.MaxValue);
		}
	}
}