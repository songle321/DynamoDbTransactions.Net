using System;
using System.Collections.Generic;
using System.Threading;
using Amazon.DynamoDBv2.Model;
using FluentAssertions;
using Xunit;

// <summary>
// Copyright 2013-2016 Amazon.com, Inc. or its affiliates. All Rights Reserved.
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
    //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
    //ORIGINAL LINE: @RunWith(MockitoJUnitRunner.class) public class ReadUncommittedIsolationHandlerImplUnitTest
    public class ReadUncommittedIsolationHandlerImplUnitTest
    {
        protected internal const string TableName = "TEST_TABLE";
        protected internal static readonly Dictionary<string, AttributeValue> Key = Collections.SingletonMap("Id", new AttributeValue
        {
            S = "KeyValue"
        });
        protected internal const string TxId = "e1b52a78-0187-4787-b1a3-27f63a78898b";
        protected internal static readonly Dictionary<string, AttributeValue> UnlockedItem = CreateItem(false, false, false);
        protected internal static readonly Dictionary<string, AttributeValue> TransientUnappliedItem = CreateItem(true, true, false);
        protected internal static readonly Dictionary<string, AttributeValue> TransientAppliedItem = CreateItem(true, true, true);
        protected internal static readonly Dictionary<string, AttributeValue> NonTransientAppliedItem = CreateItem(true, false, true);

        private ReadUncommittedIsolationHandlerImpl _isolationHandler;

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Before public void setup()
        public virtual void Setup()
        {
            _isolationHandler = new ReadUncommittedIsolationHandlerImpl();
        }

        private static Dictionary<string, AttributeValue> CreateItem(bool isLocked, bool isTransient, bool isApplied)
        {
            Dictionary<string, AttributeValue> item = new Dictionary<string, AttributeValue>();
            if (isLocked)
            {
                item[Transaction.AttributeName.Txid.ToString()] = new AttributeValue(TxId);
                item[Transaction.AttributeName.Date.ToString()] = new AttributeValue
                {
                    S = ""
                };
                if (isTransient)
                {
                    item[Transaction.AttributeName.Transient.ToString()] = new AttributeValue
                    {
                        S = ""
                    };
                }
                if (isApplied)
                {
                    item[Transaction.AttributeName.Applied.ToString()] = new AttributeValue
                    {
                        S = ""
                    };
                }
            }
            if (!isTransient)
            {
                item["attr1"] = new AttributeValue
                {
                    S = "some value"
                };
            }
            //JAVA TO C# CONVERTER TODO TASK: There is no .NET Dictionary equivalent to the Java 'putAll' method:
            foreach (var entry in Key) item.Add(entry.Key, entry.Value);
            return item;
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void handleItemReturnsNullForNullItem()
        [Fact]
        public virtual void HandleItemReturnsNullForNullItem()
        {
            Setup();
            AssertNull(_isolationHandler.HandleItemAsync(null, null, TableName, CancellationToken.None).Result);
        }

        void AssertNull(object p)
        {
            p.Should().BeNull();
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void handleItemReturnsItemForUnlockedItem()
        [Fact]
        public virtual void HandleItemReturnsItemForUnlockedItem()
        {
            Setup();
            AssertEquals(UnlockedItem, _isolationHandler.HandleItemAsync(UnlockedItem, null, TableName, CancellationToken.None).Result);
        }

        void AssertEquals(object a, object b)
        {
            a.ShouldBeEquivalentTo(b);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void handleItemReturnsNullForTransientUnappliedItem()
        [Fact]
        public virtual void HandleItemReturnsNullForTransientUnappliedItem()
        {
            Setup();
            AssertNull(_isolationHandler.HandleItemAsync(TransientUnappliedItem, null, TableName, CancellationToken.None).Result);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void handleItemReturnsNullForTransientAppliedItem()
        [Fact]
        public virtual void HandleItemReturnsNullForTransientAppliedItem()
        {
            Setup();
            AssertEquals(TransientAppliedItem, _isolationHandler.HandleItemAsync(TransientAppliedItem, null, TableName, CancellationToken.None).Result);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void handleItemReturnsItemForNonTransientAppliedItem()
        [Fact]
        public virtual void HandleItemReturnsItemForNonTransientAppliedItem()
        {
            Setup();
            AssertEquals(NonTransientAppliedItem, _isolationHandler.HandleItemAsync(NonTransientAppliedItem, null, TableName, CancellationToken.None).Result);
        }

    }

}