using System.Collections.Generic;

/// <summary>
/// Copyright 2013-2013 Amazon.com, Inc. or its affiliates. All Rights Reserved.
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
//	import static org.junit.Assert.assertArrayEquals;
//JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to C#:
//	import static org.junit.Assert.assertEquals;
//JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to C#:
//	import static org.junit.Assert.assertTrue;
//JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to C#:
//	import static org.junit.Assert.fail;


	using Test = org.junit.Test;

	using AttributeValue = com.amazonaws.services.dynamodbv2.model.AttributeValue;
	using AttributeValueUpdate = com.amazonaws.services.dynamodbv2.model.AttributeValueUpdate;
	using DeleteItemRequest = com.amazonaws.services.dynamodbv2.model.DeleteItemRequest;
	using ExpectedAttributeValue = com.amazonaws.services.dynamodbv2.model.ExpectedAttributeValue;
	using GetItemRequest = com.amazonaws.services.dynamodbv2.model.GetItemRequest;
	using KeySchemaElement = com.amazonaws.services.dynamodbv2.model.KeySchemaElement;
	using KeyType = com.amazonaws.services.dynamodbv2.model.KeyType;
	using PutItemRequest = com.amazonaws.services.dynamodbv2.model.PutItemRequest;
	using UpdateItemRequest = com.amazonaws.services.dynamodbv2.model.UpdateItemRequest;
	using ResourceNotFoundException = com.amazonaws.services.dynamodbv2.model.ResourceNotFoundException;
	using DeleteItem = com.amazonaws.services.dynamodbv2.transactions.Request.DeleteItem;
	using GetItem = com.amazonaws.services.dynamodbv2.transactions.Request.GetItem;
	using PutItem = com.amazonaws.services.dynamodbv2.transactions.Request.PutItem;
	using UpdateItem = com.amazonaws.services.dynamodbv2.transactions.Request.UpdateItem;
	using InvalidRequestException = com.amazonaws.services.dynamodbv2.transactions.exceptions.InvalidRequestException;

	public class RequestTest
	{

		private const string TABLE_NAME = "Dummy";
		private const string HASH_ATTR_NAME = "Foo";
		private static readonly IList<KeySchemaElement> HASH_SCHEMA = Arrays.asList(new KeySchemaElement().withAttributeName(HASH_ATTR_NAME).withKeyType(KeyType.HASH));

		internal static readonly IDictionary<string, AttributeValue> JSON_M_ATTR_VAL = new Dictionary<string, AttributeValue>();
		private static readonly IDictionary<string, ExpectedAttributeValue> NONNULL_EXPECTED_ATTR_VALUES = new Dictionary<string, ExpectedAttributeValue>();
		private static readonly IDictionary<string, string> NONNULL_EXP_ATTR_NAMES = new Dictionary<string, string>();
		private static readonly IDictionary<string, AttributeValue> NONNULL_EXP_ATTR_VALUES = new Dictionary<string, AttributeValue>();
		private static readonly IDictionary<string, AttributeValue> BASIC_ITEM = new Dictionary<string, AttributeValue>();

		static RequestTest()
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

			BASIC_ITEM[HASH_ATTR_NAME] = new AttributeValue("a");
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void validPut()
		public virtual void validPut()
		{
			PutItem r = new PutItem();
			IDictionary<string, AttributeValue> item = new Dictionary<string, AttributeValue>();
			item[HASH_ATTR_NAME] = new AttributeValue("a");
			r.setRequest(new PutItemRequest()
				.withTableName(TABLE_NAME).withItem(item));
			r.validate("1", new MockTransactionManager(this, HASH_SCHEMA));
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void putNullTableName()
		public virtual void putNullTableName()
		{
			IDictionary<string, AttributeValue> item = new Dictionary<string, AttributeValue>();
			item[HASH_ATTR_NAME] = new AttributeValue("a");

			invalidRequestTest(new PutItemRequest()
					.withItem(item), "TableName must not be null");
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void putNullItem()
		public virtual void putNullItem()
		{
			invalidRequestTest(new PutItemRequest()
					.withTableName(TABLE_NAME), "PutItem must contain an Item");
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void putMissingKey()
		public virtual void putMissingKey()
		{
			IDictionary<string, AttributeValue> item = new Dictionary<string, AttributeValue>();
			item["other-attr"] = new AttributeValue("a");

			invalidRequestTest(new PutItemRequest()
					.withTableName(TABLE_NAME).withItem(item), "PutItem request must contain the key attribute");
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void putExpected()
		public virtual void putExpected()
		{
			invalidRequestTest(BasicPutRequest.withExpected(NONNULL_EXPECTED_ATTR_VALUES), "Requests with conditions");
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void putConditionExpression()
		public virtual void putConditionExpression()
		{
			invalidRequestTest(BasicPutRequest.withConditionExpression("attribute_not_exists (some_field)"), "Requests with conditions");
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void putExpressionAttributeNames()
		public virtual void putExpressionAttributeNames()
		{
			invalidRequestTest(BasicPutRequest.withExpressionAttributeNames(NONNULL_EXP_ATTR_NAMES), "Requests with expressions");
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void putExpressionAttributeValues()
		public virtual void putExpressionAttributeValues()
		{
			invalidRequestTest(BasicPutRequest.withExpressionAttributeValues(NONNULL_EXP_ATTR_VALUES), "Requests with expressions");
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void updateExpected()
		public virtual void updateExpected()
		{
			invalidRequestTest(BasicUpdateRequest.withExpected(NONNULL_EXPECTED_ATTR_VALUES), "Requests with conditions");
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void updateConditionExpression()
		public virtual void updateConditionExpression()
		{
			invalidRequestTest(BasicUpdateRequest.withConditionExpression("attribute_not_exists(some_field)"), "Requests with conditions");
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void updateUpdateExpression()
		public virtual void updateUpdateExpression()
		{
			invalidRequestTest(BasicUpdateRequest.withUpdateExpression("REMOVE some_field"), "Requests with expressions");
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void updateExpressionAttributeNames()
		public virtual void updateExpressionAttributeNames()
		{
			invalidRequestTest(BasicUpdateRequest.withExpressionAttributeNames(NONNULL_EXP_ATTR_NAMES), "Requests with expressions");
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void updateExpressionAttributeValues()
		public virtual void updateExpressionAttributeValues()
		{
			invalidRequestTest(BasicUpdateRequest.withExpressionAttributeValues(NONNULL_EXP_ATTR_VALUES), "Requests with expressions");
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void deleteExpected()
		public virtual void deleteExpected()
		{
			invalidRequestTest(BasicDeleteRequest.withExpected(NONNULL_EXPECTED_ATTR_VALUES), "Requests with conditions");
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void deleteConditionExpression()
		public virtual void deleteConditionExpression()
		{
			invalidRequestTest(BasicDeleteRequest.withConditionExpression("attribute_not_exists (some_field)"), "Requests with conditions");
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void deleteExpressionAttributeNames()
		public virtual void deleteExpressionAttributeNames()
		{
			invalidRequestTest(BasicDeleteRequest.withExpressionAttributeNames(NONNULL_EXP_ATTR_NAMES), "Requests with expressions");
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void deleteExpressionAttributeValues()
		public virtual void deleteExpressionAttributeValues()
		{
			invalidRequestTest(BasicDeleteRequest.withExpressionAttributeValues(NONNULL_EXP_ATTR_VALUES), "Requests with expressions");
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void validUpdate()
		public virtual void validUpdate()
		{
			UpdateItem r = new UpdateItem();
			IDictionary<string, AttributeValue> item = new Dictionary<string, AttributeValue>();
			item[HASH_ATTR_NAME] = new AttributeValue("a");
			r.setRequest(new UpdateItemRequest()
				.withTableName(TABLE_NAME).withKey(item));
			r.validate("1", new MockTransactionManager(this, HASH_SCHEMA));
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void validDelete()
		public virtual void validDelete()
		{
			DeleteItem r = new DeleteItem();
			IDictionary<string, AttributeValue> item = new Dictionary<string, AttributeValue>();
			item[HASH_ATTR_NAME] = new AttributeValue("a");
			r.setRequest(new DeleteItemRequest()
				.withTableName(TABLE_NAME).withKey(item));
			r.validate("1", new MockTransactionManager(this, HASH_SCHEMA));
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void validLock()
		public virtual void validLock()
		{
			GetItem r = new GetItem();
			IDictionary<string, AttributeValue> item = new Dictionary<string, AttributeValue>();
			item[HASH_ATTR_NAME] = new AttributeValue("a");
			r.setRequest(new GetItemRequest()
				.withTableName(TABLE_NAME).withKey(item));
			r.validate("1", new MockTransactionManager(this, HASH_SCHEMA));
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void roundTripGetString()
		public virtual void roundTripGetString()
		{
			GetItem r1 = new GetItem();
			IDictionary<string, AttributeValue> item = new Dictionary<string, AttributeValue>();
			item[HASH_ATTR_NAME] = new AttributeValue("a");
			r1.setRequest(new GetItemRequest()
				.withTableName(TABLE_NAME).withKey(item));
			sbyte[] r1Bytes = Request.serialize("123", r1).array();
			Request r2 = Request.deserialize("123", ByteBuffer.wrap(r1Bytes));
			sbyte[] r2Bytes = Request.serialize("123", r2).array();
			assertArrayEquals(r1Bytes, r2Bytes);
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void roundTripPutAll()
		public virtual void roundTripPutAll()
		{
			PutItem r1 = new PutItem();
			IDictionary<string, AttributeValue> item = new Dictionary<string, AttributeValue>();
			item[HASH_ATTR_NAME] = new AttributeValue("a");
			item["attr_ss"] = (new AttributeValue()).withSS("a", "b");
			item["attr_n"] = (new AttributeValue()).withN("1");
			item["attr_ns"] = (new AttributeValue()).withNS("1", "2");
			item["attr_b"] = (new AttributeValue()).withB(ByteBuffer.wrap(("asdf").GetBytes()));
			item["attr_bs"] = (new AttributeValue()).withBS(ByteBuffer.wrap(("asdf").GetBytes()), ByteBuffer.wrap(("asdf").GetBytes()));
			r1.setRequest(new PutItemRequest()
				.withTableName(TABLE_NAME).withItem(item).withReturnValues("ALL_OLD"));
			sbyte[] r1Bytes = Request.serialize("123", r1).array();
			Request r2 = Request.deserialize("123", ByteBuffer.wrap(r1Bytes));
			assertEquals(r1.Request, ((PutItem)r2).Request);
			sbyte[] r2Bytes = Request.serialize("123", r2).array();
			assertArrayEquals(r1Bytes, r2Bytes);
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void roundTripUpdateAll()
		public virtual void roundTripUpdateAll()
		{
			UpdateItem r1 = new UpdateItem();
			IDictionary<string, AttributeValue> key = new Dictionary<string, AttributeValue>();
			key[HASH_ATTR_NAME] = new AttributeValue("a");

			IDictionary<string, AttributeValueUpdate> updates = new Dictionary<string, AttributeValueUpdate>();
			updates["attr_ss"] = (new AttributeValueUpdate()).withAction("PUT").withValue((new AttributeValue()).withSS("a", "b"));
			updates["attr_n"] = (new AttributeValueUpdate()).withAction("PUT").withValue((new AttributeValue()).withN("1"));
			updates["attr_ns"] = (new AttributeValueUpdate()).withAction("PUT").withValue((new AttributeValue()).withNS("1", "2"));
			updates["attr_b"] = (new AttributeValueUpdate()).withAction("PUT").withValue((new AttributeValue()).withB(ByteBuffer.wrap(("asdf").GetBytes())));
			updates["attr_bs"] = (new AttributeValueUpdate()).withAction("PUT").withValue((new AttributeValue()).withBS(ByteBuffer.wrap(("asdf").GetBytes()), ByteBuffer.wrap(("asdf").GetBytes())));
			r1.setRequest(new UpdateItemRequest()
				.withTableName(TABLE_NAME).withKey(key).withAttributeUpdates(updates));
			sbyte[] r1Bytes = Request.serialize("123", r1).array();
			Request r2 = Request.deserialize("123", ByteBuffer.wrap(r1Bytes));
			sbyte[] r2Bytes = Request.serialize("123", r2).array();
			assertArrayEquals(r1Bytes, r2Bytes);
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void roundTripPutAllJSON()
		public virtual void roundTripPutAllJSON()
		{
			PutItem r1 = new PutItem();
			IDictionary<string, AttributeValue> item = new Dictionary<string, AttributeValue>();
			item[HASH_ATTR_NAME] = new AttributeValue("a");
			item["json_attr"] = (new AttributeValue()).withM(JSON_M_ATTR_VAL);
			r1.setRequest(new PutItemRequest()
				.withTableName(TABLE_NAME).withItem(item).withReturnValues("ALL_OLD"));
			sbyte[] r1Bytes = Request.serialize("123", r1).array();
			Request r2 = Request.deserialize("123", ByteBuffer.wrap(r1Bytes));
			assertEquals(r1.Request, ((PutItem)r2).Request);
			sbyte[] r2Bytes = Request.serialize("123", r2).array();
			assertArrayEquals(r1Bytes, r2Bytes);
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void roundTripUpdateAllJSON()
		public virtual void roundTripUpdateAllJSON()
		{
			UpdateItem r1 = new UpdateItem();
			IDictionary<string, AttributeValue> key = new Dictionary<string, AttributeValue>();
			key[HASH_ATTR_NAME] = new AttributeValue("a");

			IDictionary<string, AttributeValueUpdate> updates = new Dictionary<string, AttributeValueUpdate>();
			updates["attr_m"] = (new AttributeValueUpdate()).withAction("PUT").withValue((new AttributeValue()).withM(JSON_M_ATTR_VAL));
			r1.setRequest(new UpdateItemRequest()
				.withTableName(TABLE_NAME).withKey(key).withAttributeUpdates(updates));
			sbyte[] r1Bytes = Request.serialize("123", r1).array();
			Request r2 = Request.deserialize("123", ByteBuffer.wrap(r1Bytes));
			sbyte[] r2Bytes = Request.serialize("123", r2).array();
			assertArrayEquals(r1Bytes, r2Bytes);
		}

		private PutItemRequest BasicPutRequest
		{
			get
			{
				return (new PutItemRequest()).withItem(BASIC_ITEM).withTableName(TABLE_NAME);
			}
		}

		private UpdateItemRequest BasicUpdateRequest
		{
			get
			{
				return (new UpdateItemRequest()).withKey(BASIC_ITEM).withTableName(TABLE_NAME);
			}
		}

		private DeleteItemRequest BasicDeleteRequest
		{
			get
			{
				return (new DeleteItemRequest()).withKey(BASIC_ITEM).withTableName(TABLE_NAME);
			}
		}

		private void invalidRequestTest(PutItemRequest request, string expectedExceptionMessage)
		{
			PutItem r = new PutItem();
			r.Request = request;
			try
			{
				r.validate("1", new MockTransactionManager(this, HASH_SCHEMA));
				fail();
			}
			catch (InvalidRequestException e)
			{
				assertTrue(e.Message.contains(expectedExceptionMessage));
			}
		}

		private void invalidRequestTest(UpdateItemRequest request, string expectedExceptionMessage)
		{
			UpdateItem r = new UpdateItem();
			r.Request = request;
			try
			{
				r.validate("1", new MockTransactionManager(this, HASH_SCHEMA));
				fail();
			}
			catch (InvalidRequestException e)
			{
				assertTrue(e.Message.contains(expectedExceptionMessage));
			}
		}

		private void invalidRequestTest(DeleteItemRequest request, string expectedExceptionMessage)
		{
			DeleteItem r = new DeleteItem();
			r.Request = request;
			try
			{
				r.validate("1", new MockTransactionManager(this, HASH_SCHEMA));
				fail();
			}
			catch (InvalidRequestException e)
			{
				assertTrue(e.Message.contains(expectedExceptionMessage));
			}
		}

		protected internal class MockTransactionManager : TransactionManager
		{
			private readonly RequestTest outerInstance;


			internal readonly IList<KeySchemaElement> keySchema;

			public MockTransactionManager(RequestTest outerInstance, IList<KeySchemaElement> keySchema) : base(new AmazonDynamoDBClient(), "Dummy", "DummyOther")
			{
				this.outerInstance = outerInstance;
				this.keySchema = keySchema;
			}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override protected java.util.List<com.amazonaws.services.dynamodbv2.model.KeySchemaElement> getTableSchema(String tableName) throws com.amazonaws.services.dynamodbv2.model.ResourceNotFoundException
			protected internal override IList<KeySchemaElement> getTableSchema(string tableName)
			{
				return keySchema;
			}
		}
	}


}