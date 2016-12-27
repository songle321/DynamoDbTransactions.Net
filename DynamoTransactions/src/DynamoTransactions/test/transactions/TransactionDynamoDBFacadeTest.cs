using System.Collections.Generic;

/// <summary>
/// Copyright 2014-2014 Amazon.com, Inc. or its affiliates. All Rights Reserved.
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


	using Test = org.junit.Test;

	using AttributeValue = com.amazonaws.services.dynamodbv2.model.AttributeValue;
	using ConditionalCheckFailedException = com.amazonaws.services.dynamodbv2.model.ConditionalCheckFailedException;
	using ExpectedAttributeValue = com.amazonaws.services.dynamodbv2.model.ExpectedAttributeValue;

	public class TransactionDynamoDBFacadeTest
	{

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testCheckExpectedStringValueWithMatchingItem()
		public virtual void testCheckExpectedStringValueWithMatchingItem()
		{
			IDictionary<string, AttributeValue> item = Collections.singletonMap("Foo", new AttributeValue("Bar"));
			IDictionary<string, ExpectedAttributeValue> expected = Collections.singletonMap("Foo", new ExpectedAttributeValue(new AttributeValue("Bar")));

			TransactionDynamoDBFacade.checkExpectedValues(expected, item);
			// no exception expected
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test(expected = com.amazonaws.services.dynamodbv2.model.ConditionalCheckFailedException.class) public void testCheckExpectedStringValueWithNonMatchingItem()
		public virtual void testCheckExpectedStringValueWithNonMatchingItem()
		{
			IDictionary<string, AttributeValue> item = Collections.singletonMap("Foo", new AttributeValue("Bar"));
			IDictionary<string, ExpectedAttributeValue> expected = Collections.singletonMap("Foo", new ExpectedAttributeValue(new AttributeValue("NotBar")));

			TransactionDynamoDBFacade.checkExpectedValues(expected, item);
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testCheckExpectedBinaryValueWithMatchingItem()
		public virtual void testCheckExpectedBinaryValueWithMatchingItem()
		{
			IDictionary<string, AttributeValue> item = Collections.singletonMap("Foo", (new AttributeValue()).withB(ByteBuffer.wrap(new sbyte[] {1, 127, (sbyte)-127})));
			IDictionary<string, ExpectedAttributeValue> expected = Collections.singletonMap("Foo", new ExpectedAttributeValue((new AttributeValue()).withB(ByteBuffer.wrap(new sbyte[] {1, 127, (sbyte)-127}))));

			TransactionDynamoDBFacade.checkExpectedValues(expected, item);
			// no exception expected
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test(expected = com.amazonaws.services.dynamodbv2.model.ConditionalCheckFailedException.class) public void testCheckExpectedBinaryValueWithNonMatchingItem()
		public virtual void testCheckExpectedBinaryValueWithNonMatchingItem()
		{
			IDictionary<string, AttributeValue> item = Collections.singletonMap("Foo", (new AttributeValue()).withB(ByteBuffer.wrap(new sbyte[] {1, 127, (sbyte)-127})));
			IDictionary<string, ExpectedAttributeValue> expected = Collections.singletonMap("Foo", new ExpectedAttributeValue((new AttributeValue()).withB(ByteBuffer.wrap(new sbyte[] {0, 127, (sbyte)-127}))));

			TransactionDynamoDBFacade.checkExpectedValues(expected, item);
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testCheckExpectedNumericValueWithMatchingItem()
		public virtual void testCheckExpectedNumericValueWithMatchingItem()
		{
			IDictionary<string, AttributeValue> item = Collections.singletonMap("Foo", (new AttributeValue()).withN("3.14"));
			IDictionary<string, ExpectedAttributeValue> expected = Collections.singletonMap("Foo", new ExpectedAttributeValue((new AttributeValue()).withN("3.14")));

			TransactionDynamoDBFacade.checkExpectedValues(expected, item);
			// no exception expected
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testCheckExpectedNumericValueWithMatchingNotStringEqualItem()
		public virtual void testCheckExpectedNumericValueWithMatchingNotStringEqualItem()
		{
			IDictionary<string, AttributeValue> item = Collections.singletonMap("Foo", (new AttributeValue()).withN("3.140"));
			IDictionary<string, ExpectedAttributeValue> expected = Collections.singletonMap("Foo", new ExpectedAttributeValue((new AttributeValue()).withN("3.14")));

			TransactionDynamoDBFacade.checkExpectedValues(expected, item);
			// no exception expected
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test(expected = com.amazonaws.services.dynamodbv2.model.ConditionalCheckFailedException.class) public void testCheckExpectedNumericValueWithNonMatchingItem()
		public virtual void testCheckExpectedNumericValueWithNonMatchingItem()
		{
			IDictionary<string, AttributeValue> item = Collections.singletonMap("Foo", (new AttributeValue()).withN("3.14"));
			IDictionary<string, ExpectedAttributeValue> expected = Collections.singletonMap("Foo", new ExpectedAttributeValue((new AttributeValue()).withN("12")));

			TransactionDynamoDBFacade.checkExpectedValues(expected, item);
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test(expected = com.amazonaws.services.dynamodbv2.model.ConditionalCheckFailedException.class) public void testCheckExpectedNumericValueWithStringTypedItem()
		public virtual void testCheckExpectedNumericValueWithStringTypedItem()
		{
			IDictionary<string, AttributeValue> item = Collections.singletonMap("Foo", new AttributeValue("3.14"));
			IDictionary<string, ExpectedAttributeValue> expected = Collections.singletonMap("Foo", new ExpectedAttributeValue((new AttributeValue()).withN("3.14")));

			TransactionDynamoDBFacade.checkExpectedValues(expected, item);
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test(expected = IllegalArgumentException.class) public void testCheckExpectedInvalidNumericValue()
		public virtual void testCheckExpectedInvalidNumericValue()
		{
			IDictionary<string, AttributeValue> item = Collections.singletonMap("Foo", (new AttributeValue()).withN("1.1"));
			IDictionary<string, ExpectedAttributeValue> expected = Collections.singletonMap("Foo", new ExpectedAttributeValue((new AttributeValue()).withN("!!.!!")));

			TransactionDynamoDBFacade.checkExpectedValues(expected, item);
		}

	}

}