using System;
using System.Collections.Generic;
using System.IO;
using Amazon.DynamoDBv2.Model;
using FluentAssertions.Execution;
using Xunit;

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
    public class TransactionDynamoDbFacadeTest
    {
        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void testCheckExpectedStringValueWithMatchingItem()
        [Fact]
        public virtual void TestCheckExpectedStringValueWithMatchingItem()
        {
            var item = new Dictionary<string, AttributeValue> {{"Foo", new AttributeValue("Bar")}};
            var expected = new Dictionary<string, ExpectedAttributeValue>
            {
                {"Foo", new ExpectedAttributeValue(new AttributeValue("Bar"))}
            };

            TransactionDynamoDbFacade.CheckExpectedValuesAsync(expected, item).Wait();
            // no exception expected
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test(expected = com.amazonaws.services.dynamodbv2.model.ConditionalCheckFailedException.class) public void testCheckExpectedStringValueWithNonMatchingItem()
        [Fact]
        public virtual void TestCheckExpectedStringValueWithNonMatchingItem()
        {
            Dictionary<string, AttributeValue> item = new Dictionary<string, AttributeValue>
            {
                {"Foo", new AttributeValue("Bar")}
            };
            Dictionary<string, ExpectedAttributeValue> expected = new Dictionary<string, ExpectedAttributeValue>
            {
                {"Foo", new ExpectedAttributeValue(new AttributeValue("NotBar"))}
            };

            try
            {
                TransactionDynamoDbFacade.CheckExpectedValuesAsync(expected, item).Wait();
            }
            catch (AggregateException e) when (e.InnerException is ConditionalCheckFailedException)
            {
                return;
            }
            catch (ConditionalCheckFailedException)
            {
                return;
            }
            throw new AssertionFailedException("Expected ConditionalCheckFailedException");
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void testCheckExpectedBinaryValueWithMatchingItem()
        [Fact]
        public virtual void TestCheckExpectedBinaryValueWithMatchingItem()
        {
            Dictionary<string, AttributeValue> item = new Dictionary<string, AttributeValue>
            {
                {"Foo", new AttributeValue {B = new MemoryStream(new byte[] {1, 127, 255})}}
            };

            Dictionary<string, ExpectedAttributeValue> expected = new Dictionary<string, ExpectedAttributeValue>
            {
                {"Foo", new ExpectedAttributeValue(new AttributeValue {B = new MemoryStream(new byte[] {1, 127, 255})})}
            };


            TransactionDynamoDbFacade.CheckExpectedValuesAsync(expected, item).Wait();
            // no exception expected
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test(expected = com.amazonaws.services.dynamodbv2.model.ConditionalCheckFailedException.class) public void testCheckExpectedBinaryValueWithNonMatchingItem()
        [Fact]
        public virtual void TestCheckExpectedBinaryValueWithNonMatchingItem()
        {
            Dictionary<string, AttributeValue> item = new Dictionary<string, AttributeValue>
            {
                {"Foo", new AttributeValue {B = new MemoryStream(new byte[] {1, 127, 255})}}
            };

            Dictionary<string, ExpectedAttributeValue> expected = new Dictionary<string, ExpectedAttributeValue>
            {
                {"Foo", new ExpectedAttributeValue(new AttributeValue {B = new MemoryStream(new byte[] {0, 127, 255})})}
            };

            try
            {
                TransactionDynamoDbFacade.CheckExpectedValuesAsync(expected, item).Wait();
            }
            catch (AggregateException e) when (e.InnerException is ConditionalCheckFailedException)
            {
                return;
            }
            catch (ConditionalCheckFailedException)
            {
                return;
            }
            throw new AssertionFailedException("Expected ConditionalCheckFailedException");
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void testCheckExpectedNumericValueWithMatchingItem()
        [Fact]
        public virtual void TestCheckExpectedNumericValueWithMatchingItem()
        {
            Dictionary<string, AttributeValue> item = new Dictionary<string, AttributeValue>
            {
                {"Foo", new AttributeValue {N = "3.14"}}
            };

            Dictionary<string, ExpectedAttributeValue> expected = new Dictionary<string, ExpectedAttributeValue>
            {
                {"Foo", new ExpectedAttributeValue(new AttributeValue {N = "3.14"})}
            };

            TransactionDynamoDbFacade.CheckExpectedValuesAsync(expected, item).Wait();
            // no exception expected
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void testCheckExpectedNumericValueWithMatchingNotStringEqualItem()
        [Fact]
        public virtual void TestCheckExpectedNumericValueWithMatchingNotStringEqualItem()
        {
            Dictionary<string, AttributeValue> item = new Dictionary<string, AttributeValue>
            {
                {"Foo", new AttributeValue {N = "3.140"}}
            };

            Dictionary<string, ExpectedAttributeValue> expected = new Dictionary<string, ExpectedAttributeValue>
            {
                {"Foo", new ExpectedAttributeValue(new AttributeValue {N = "3.14"})}
            };

            TransactionDynamoDbFacade.CheckExpectedValuesAsync(expected, item).Wait();
            // no exception expected
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test(expected = com.amazonaws.services.dynamodbv2.model.ConditionalCheckFailedException.class) public void testCheckExpectedNumericValueWithNonMatchingItem()
        [Fact]
        public virtual void TestCheckExpectedNumericValueWithNonMatchingItem()
        {
            Dictionary<string, AttributeValue> item = new Dictionary<string, AttributeValue>
            {
                {"Foo", new AttributeValue {N = "3.14"}}
            };

            Dictionary<string, ExpectedAttributeValue> expected = new Dictionary<string, ExpectedAttributeValue>
            {
                {"Foo", new ExpectedAttributeValue(new AttributeValue {N = "12"})}
            };

            try
            {
                TransactionDynamoDbFacade.CheckExpectedValuesAsync(expected, item).Wait();
            }
            catch (AggregateException e) when (e.InnerException is ConditionalCheckFailedException)
            {
                return;
            }
            catch (ConditionalCheckFailedException)
            {
                return;
            }
            throw new AssertionFailedException("Expected ConditionalCheckFailedException");
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test(expected = com.amazonaws.services.dynamodbv2.model.ConditionalCheckFailedException.class) public void testCheckExpectedNumericValueWithStringTypedItem()
        [Fact]
        public virtual void TestCheckExpectedNumericValueWithStringTypedItem()
        {
            Dictionary<string, AttributeValue> item = new Dictionary<string, AttributeValue>
            {
                {"Foo", new AttributeValue("3.14")}
            };

            Dictionary<string, ExpectedAttributeValue> expected = new Dictionary<string, ExpectedAttributeValue>
            {
                {"Foo", new ExpectedAttributeValue(new AttributeValue {N = "3.14"})}
            };

            try
            {
                TransactionDynamoDbFacade.CheckExpectedValuesAsync(expected, item).Wait();
            }
            catch (AggregateException e) when (e.InnerException is ConditionalCheckFailedException)
            {
                return;
            }
            catch (ConditionalCheckFailedException)
            {
                return;
            }
            throw new AssertionFailedException("Expected ConditionalCheckFailedException");
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test(expected = IllegalArgumentException.class) public void testCheckExpectedInvalidNumericValue()
        [Fact]
        public virtual void TestCheckExpectedInvalidNumericValue()
        {
            Dictionary<string, AttributeValue> item = new Dictionary<string, AttributeValue>
            {
                {"Foo", new AttributeValue {N = "1.1"}}
            };

            Dictionary<string, ExpectedAttributeValue> expected = new Dictionary<string, ExpectedAttributeValue>
            {
                {"Foo", new ExpectedAttributeValue(new AttributeValue {N = "!!.!!"})}
            };

            TransactionDynamoDbFacade.CheckExpectedValuesAsync(expected, item).Wait();
        }

    }

}