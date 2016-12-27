using System.Collections.Generic;

/// <summary>
/// Copyright 2013-2016 Amazon.com, Inc. or its affiliates. All Rights Reserved.
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

	using AttributeValue = com.amazonaws.services.dynamodbv2.model.AttributeValue;
	using Before = org.junit.Before;
	using Test = org.junit.Test;
	using RunWith = org.junit.runner.RunWith;
	using MockitoJUnitRunner = org.mockito.runners.MockitoJUnitRunner;


//JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to C#:
//	import static org.junit.Assert.assertEquals;
//JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to C#:
//	import static org.junit.Assert.assertNull;

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @RunWith(MockitoJUnitRunner.class) public class ReadUncommittedIsolationHandlerImplUnitTest
	public class ReadUncommittedIsolationHandlerImplUnitTest
	{

		protected internal const string TABLE_NAME = "TEST_TABLE";
		protected internal static readonly Dictionary<string, AttributeValue> KEY = Collections.singletonMap("Id", new AttributeValue {

S = "KeyValue",)
};
		protected internal const string TX_ID = "e1b52a78-0187-4787-b1a3-27f63a78898b";
		protected internal static readonly Dictionary<string, AttributeValue> UNLOCKED_ITEM = createItem(false, false, false);
		protected internal static readonly Dictionary<string, AttributeValue> TRANSIENT_UNAPPLIED_ITEM = createItem(true, true, false);
		protected internal static readonly Dictionary<string, AttributeValue> TRANSIENT_APPLIED_ITEM = createItem(true, true, true);
		protected internal static readonly Dictionary<string, AttributeValue> NON_TRANSIENT_APPLIED_ITEM = createItem(true, false, true);

		private ReadUncommittedIsolationHandlerImpl isolationHandler;

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Before public void setup()
		public virtual void setup()
		{
			isolationHandler = new ReadUncommittedIsolationHandlerImpl();
		}

		private static Dictionary<string, AttributeValue> createItem(bool isLocked, bool isTransient, bool isApplied)
		{
			Dictionary<string, AttributeValue> item = new Dictionary<string, AttributeValue>();
			if (isLocked)
			{
				item[Transaction.AttributeName.TXID.ToString()] = new AttributeValue(TX_ID);
				item[Transaction.AttributeName.DATE.ToString()] = new AttributeValue {

S = "",
};
				if (isTransient)
				{
					item[Transaction.AttributeName.TRANSIENT.ToString()] = new AttributeValue {

S = "",
};
				}
				if (isApplied)
				{
					item[Transaction.AttributeName.APPLIED.ToString()] = new AttributeValue {

S = "",
};
				}
			}
			if (!isTransient)
			{
				item["attr1"] = new AttributeValue {

S = "some value",
};
			}
//JAVA TO C# CONVERTER TODO TASK: There is no .NET Dictionary equivalent to the Java 'putAll' method:
			item.putAll(KEY);
			return item;
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void handleItemReturnsNullForNullItem()
		public virtual void handleItemReturnsNullForNullItem()
		{
			assertNull(isolationHandler.handleItem(null, null, TABLE_NAME));
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void handleItemReturnsItemForUnlockedItem()
		public virtual void handleItemReturnsItemForUnlockedItem()
		{
			assertEquals(UNLOCKED_ITEM, isolationHandler.handleItem(UNLOCKED_ITEM, null, TABLE_NAME));
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void handleItemReturnsNullForTransientUnappliedItem()
		public virtual void handleItemReturnsNullForTransientUnappliedItem()
		{
			assertNull(isolationHandler.handleItem(TRANSIENT_UNAPPLIED_ITEM, null, TABLE_NAME));
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void handleItemReturnsNullForTransientAppliedItem()
		public virtual void handleItemReturnsNullForTransientAppliedItem()
		{
			assertEquals(TRANSIENT_APPLIED_ITEM, isolationHandler.handleItem(TRANSIENT_APPLIED_ITEM, null, TABLE_NAME));
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void handleItemReturnsItemForNonTransientAppliedItem()
		public virtual void handleItemReturnsItemForNonTransientAppliedItem()
		{
			assertEquals(NON_TRANSIENT_APPLIED_ITEM, isolationHandler.handleItem(NON_TRANSIENT_APPLIED_ITEM, null, TABLE_NAME));
		}

	}

}