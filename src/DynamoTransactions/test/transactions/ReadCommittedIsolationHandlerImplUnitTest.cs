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
	using GetItemRequest = com.amazonaws.services.dynamodbv2.model.GetItemRequest;
	using GetItemResponse = com.amazonaws.services.dynamodbv2.model.GetItemResult;
	using State = com.amazonaws.services.dynamodbv2.transactions.TransactionItem.State;
	using TransactionAssertionException = com.amazonaws.services.dynamodbv2.transactions.exceptions.TransactionAssertionException;
	using TransactionException = com.amazonaws.services.dynamodbv2.transactions.exceptions.TransactionException;
	using TransactionNotFoundException = com.amazonaws.services.dynamodbv2.transactions.exceptions.TransactionNotFoundException;
	using UnknownCompletedTransactionException = com.amazonaws.services.dynamodbv2.transactions.exceptions.UnknownCompletedTransactionException;
	using Before = org.junit.Before;
	using Test = org.junit.Test;
	using RunWith = org.junit.runner.RunWith;
	using Mock = org.mockito.Mock;
	using MockitoJUnitRunner = org.mockito.runners.MockitoJUnitRunner;


//JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to C#:
//	import static com.amazonaws.services.dynamodbv2.transactions.ReadUncommittedIsolationHandlerImplUnitTest.KEY;
//JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to C#:
//	import static com.amazonaws.services.dynamodbv2.transactions.ReadUncommittedIsolationHandlerImplUnitTest.NON_TRANSIENT_APPLIED_ITEM;
//JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to C#:
//	import static com.amazonaws.services.dynamodbv2.transactions.ReadUncommittedIsolationHandlerImplUnitTest.TABLE_NAME;
//JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to C#:
//	import static com.amazonaws.services.dynamodbv2.transactions.ReadUncommittedIsolationHandlerImplUnitTest.TRANSIENT_APPLIED_ITEM;
//JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to C#:
//	import static com.amazonaws.services.dynamodbv2.transactions.ReadUncommittedIsolationHandlerImplUnitTest.TRANSIENT_UNAPPLIED_ITEM;
//JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to C#:
//	import static com.amazonaws.services.dynamodbv2.transactions.ReadUncommittedIsolationHandlerImplUnitTest.TX_ID;
//JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to C#:
//	import static com.amazonaws.services.dynamodbv2.transactions.ReadUncommittedIsolationHandlerImplUnitTest.UNLOCKED_ITEM;
//JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to C#:
//	import static org.junit.Assert.assertEquals;
//JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to C#:
//	import static org.junit.Assert.assertNull;
//JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to C#:
//	import static org.junit.Assert.assertTrue;
//JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to C#:
//	import static org.mockito.Mockito.doReturn;
//JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to C#:
//	import static org.mockito.Mockito.doThrow;
//JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to C#:
//	import static org.mockito.Mockito.spy;
//JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to C#:
//	import static org.mockito.Mockito.times;
//JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to C#:
//	import static org.mockito.Mockito.verify;
//JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to C#:
//	import static org.mockito.Mockito.when;

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @RunWith(MockitoJUnitRunner.class) public class ReadCommittedIsolationHandlerImplUnitTest
	public class ReadCommittedIsolationHandlerImplUnitTest
	{
protected internal const int RID = 1;
		protected internal static GetItemRequest GET_ITEM_REQUEST = new GetItemRequest {
TableName = TABLE_NAME,
Key = KEY,
ConsistentRead = true
};

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Mock private TransactionManager mockTxManager;
		private TransactionManager mockTxManager;

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Mock private Transaction mockTx;
		private Transaction mockTx;

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Mock private TransactionItem mockTxItem;
		private TransactionItem mockTxItem;

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Mock private Request mockRequest;
		private Request mockRequest;

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Mock private com.amazonaws.services.dynamodbv2.AmazonDynamoDBClient mockClient;
		private AmazonDynamoDBClient mockClient;

		private ReadCommittedIsolationHandlerImpl isolationHandler;

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Before public void setup()
		public virtual void setup()
		{
			isolationHandler = spy(new ReadCommittedIsolationHandlerImpl(mockTxManager, 0));
			when(mockTx.TxItem).thenReturn(mockTxItem);
			when(mockTx.Id).thenReturn(TX_ID);
			when(mockTxManager.Client).thenReturn(mockClient);
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void checkItemCommittedReturnsNullForNullItem()
		public virtual void checkItemCommittedReturnsNullForNullItem()
		{
			assertNull(isolationHandler.checkItemCommitted(null));
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void checkItemCommittedReturnsItemForUnlockedItem()
		public virtual void checkItemCommittedReturnsItemForUnlockedItem()
		{
			assertEquals(UNLOCKED_ITEM, isolationHandler.checkItemCommitted(UNLOCKED_ITEM));
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void checkItemCommittedReturnsNullForTransientItem()
		public virtual void checkItemCommittedReturnsNullForTransientItem()
		{
			assertNull(isolationHandler.checkItemCommitted(TRANSIENT_APPLIED_ITEM));
			assertNull(isolationHandler.checkItemCommitted(TRANSIENT_UNAPPLIED_ITEM));
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test(expected = com.amazonaws.services.dynamodbv2.transactions.exceptions.TransactionException.class) public void checkItemCommittedThrowsExceptionForNonTransientAppliedItem()
		public virtual void checkItemCommittedThrowsExceptionForNonTransientAppliedItem()
		{
			isolationHandler.checkItemCommitted(NON_TRANSIENT_APPLIED_ITEM);
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void filterAttributesToGetReturnsNullForNullItem()
		public virtual void filterAttributesToGetReturnsNullForNullItem()
		{
			isolationHandler.filterAttributesToGet(null, null);
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void filterAttributesToGetReturnsItemWhenAttributesToGetIsNull()
		public virtual void filterAttributesToGetReturnsItemWhenAttributesToGetIsNull()
		{
			Dictionary<string, AttributeValue> result = isolationHandler.filterAttributesToGet(UNLOCKED_ITEM, null);
			assertEquals(UNLOCKED_ITEM, result);
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void filterAttributesToGetReturnsItemWhenAttributesToGetIsEmpty()
		public virtual void filterAttributesToGetReturnsItemWhenAttributesToGetIsEmpty()
		{
			Dictionary<string, AttributeValue> result = isolationHandler.filterAttributesToGet(UNLOCKED_ITEM, new List<string>());
			assertEquals(UNLOCKED_ITEM, result);
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void filterAttributesToGetReturnsItemWhenAttributesToGetContainsAllAttributes()
		public virtual void filterAttributesToGetReturnsItemWhenAttributesToGetContainsAllAttributes()
		{
			List<string> attributesToGet = Arrays.asList("Id", "attr1"); // all attributes
			Dictionary<string, AttributeValue> result = isolationHandler.filterAttributesToGet(UNLOCKED_ITEM, attributesToGet);
			assertEquals(UNLOCKED_ITEM, result);
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void filterAttributesToGetReturnsOnlySpecifiedAttributesWhenSpecified()
		public virtual void filterAttributesToGetReturnsOnlySpecifiedAttributesWhenSpecified()
		{
			List<string> attributesToGet = Arrays.asList("Id"); // only keep the key
			Dictionary<string, AttributeValue> result = isolationHandler.filterAttributesToGet(UNLOCKED_ITEM, attributesToGet);
			assertEquals(KEY, result);
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test(expected = com.amazonaws.services.dynamodbv2.transactions.exceptions.TransactionAssertionException.class) public void getOldCommittedItemThrowsExceptionIfNoLockingRequestExists()
		public virtual void getOldCommittedItemThrowsExceptionIfNoLockingRequestExists()
		{
			when(mockTxItem.getRequestForKey(TABLE_NAME, KEY)).thenReturn(null);
			isolationHandler.getOldCommittedItem(mockTx, TABLE_NAME, KEY);
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test(expected = com.amazonaws.services.dynamodbv2.transactions.exceptions.UnknownCompletedTransactionException.class) public void getOldCommittedItemThrowsExceptionIfOldItemDoesNotExist()
		public virtual void getOldCommittedItemThrowsExceptionIfOldItemDoesNotExist()
		{
			when(mockTxItem.getRequestForKey(TABLE_NAME, KEY)).thenReturn(mockRequest);
			when(mockRequest.Rid).thenReturn(RID);
			when(mockTxItem.loadItemImage(RID)).thenReturn(null);
			isolationHandler.getOldCommittedItem(mockTx, TABLE_NAME, KEY);
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void getOldCommittedItemReturnsOldImageIfOldItemExists()
		public virtual void getOldCommittedItemReturnsOldImageIfOldItemExists()
		{
			when(mockTxItem.getRequestForKey(TABLE_NAME, KEY)).thenReturn(mockRequest);
			when(mockRequest.Rid).thenReturn(RID);
			when(mockTxItem.loadItemImage(RID)).thenReturn(UNLOCKED_ITEM);
			Dictionary<string, AttributeValue> result = isolationHandler.getOldCommittedItem(mockTx, TABLE_NAME, KEY);
			assertEquals(UNLOCKED_ITEM, result);
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void createGetItemRequestCorrectlyCreatesRequest()
		public virtual void createGetItemRequestCorrectlyCreatesRequest()
		{
			when(mockTxManager.CreateKeyMapAsync(TABLE_NAME, NON_TRANSIENT_APPLIED_ITEM)).thenReturn(KEY);
			GetItemRequest request = isolationHandler.CreateGetItemRequestAsync(TABLE_NAME, NON_TRANSIENT_APPLIED_ITEM);
			assertEquals(TABLE_NAME, request.TableName);
			assertEquals(KEY, request.Key);
			assertEquals(null, request.AttributesToGet);
			assertTrue(request.ConsistentRead);
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void handleItemReturnsNullForNullItem()
		public virtual void handleItemReturnsNullForNullItem()
		{
			assertNull(isolationHandler.HandleItemAsync(null, TABLE_NAME, 0));
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void handleItemReturnsItemForUnlockedItem()
		public virtual void handleItemReturnsItemForUnlockedItem()
		{
			assertEquals(UNLOCKED_ITEM, isolationHandler.HandleItemAsync(UNLOCKED_ITEM, TABLE_NAME, 0));
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void handleItemReturnsNullForTransientItem()
		public virtual void handleItemReturnsNullForTransientItem()
		{
			assertNull(isolationHandler.HandleItemAsync(TRANSIENT_APPLIED_ITEM, TABLE_NAME, 0));
			assertNull(isolationHandler.HandleItemAsync(TRANSIENT_UNAPPLIED_ITEM, TABLE_NAME, 0));
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test(expected = com.amazonaws.services.dynamodbv2.transactions.exceptions.TransactionException.class) public void handleItemThrowsExceptionForNonTransientAppliedItemWithNoCorrespondingTx()
		public virtual void handleItemThrowsExceptionForNonTransientAppliedItemWithNoCorrespondingTx()
		{
			doThrow(typeof(TransactionNotFoundException)).when(isolationHandler).loadTransaction(TX_ID);
			isolationHandler.HandleItemAsync(NON_TRANSIENT_APPLIED_ITEM, TABLE_NAME, 0);
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void handleItemReturnsItemForNonTransientAppliedItemWithCommittedTxItem()
		public virtual void handleItemReturnsItemForNonTransientAppliedItemWithCommittedTxItem()
		{
			doReturn(mockTx).when(isolationHandler).loadTransaction(TX_ID);
			when(mockTxItem.State).thenReturn(State.COMMITTED);
			assertEquals(NON_TRANSIENT_APPLIED_ITEM, isolationHandler.HandleItemAsync(NON_TRANSIENT_APPLIED_ITEM, TABLE_NAME, 0));
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void handleItemReturnsOldVersionOfItemForNonTransientAppliedItemWithPendingTxItem()
		public virtual void handleItemReturnsOldVersionOfItemForNonTransientAppliedItemWithPendingTxItem()
		{
			doReturn(mockTx).when(isolationHandler).loadTransaction(TX_ID);
			doReturn(UNLOCKED_ITEM).when(isolationHandler).getOldCommittedItem(mockTx, TABLE_NAME, KEY);
			when(mockTxManager.CreateKeyMapAsync(TABLE_NAME, NON_TRANSIENT_APPLIED_ITEM)).thenReturn(KEY);
			when(mockTxItem.State).thenReturn(State.PENDING);
			when(mockTxItem.getRequestForKey(TABLE_NAME, KEY)).thenReturn(mockRequest);
			assertEquals(UNLOCKED_ITEM, isolationHandler.HandleItemAsync(NON_TRANSIENT_APPLIED_ITEM, TABLE_NAME, 0));
			verify(isolationHandler).loadTransaction(TX_ID);
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test(expected = com.amazonaws.services.dynamodbv2.transactions.exceptions.TransactionException.class) public void handleItemThrowsExceptionForNonTransientAppliedItemWithPendingTxItemWithNoOldVersionAndNoRetries()
		public virtual void handleItemThrowsExceptionForNonTransientAppliedItemWithPendingTxItemWithNoOldVersionAndNoRetries()
		{
			doReturn(mockTx).when(isolationHandler).loadTransaction(TX_ID);
			doThrow(typeof(UnknownCompletedTransactionException)).when(isolationHandler).getOldCommittedItem(mockTx, TABLE_NAME, KEY);
			when(mockTxItem.State).thenReturn(State.PENDING);
			when(mockTxItem.getRequestForKey(TABLE_NAME, KEY)).thenReturn(mockRequest);
			isolationHandler.HandleItemAsync(NON_TRANSIENT_APPLIED_ITEM, TABLE_NAME, 0);
			verify(isolationHandler).loadTransaction(TX_ID);
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void handleItemRetriesWhenTransactionNotFound()
		public virtual void handleItemRetriesWhenTransactionNotFound()
		{
			doThrow(typeof(TransactionNotFoundException)).when(isolationHandler).loadTransaction(TX_ID);
			when(mockTxManager.CreateKeyMapAsync(TABLE_NAME, NON_TRANSIENT_APPLIED_ITEM)).thenReturn(KEY);
			when(mockClient.getItem(GET_ITEM_REQUEST)).thenReturn(new GetItemResult {
Item = NON_TRANSIENT_APPLIED_ITEM,)
};
			bool caughtException = false;
			try
			{
				isolationHandler.HandleItemAsync(NON_TRANSIENT_APPLIED_ITEM, TABLE_NAME, 1);
			}
			catch (TransactionException)
			{
				caughtException = true;
			}
			assertTrue(caughtException);
			verify(isolationHandler, times(2)).loadTransaction(TX_ID);
			verify(isolationHandler).createGetItemRequest(TABLE_NAME, NON_TRANSIENT_APPLIED_ITEM);
			verify(mockClient).getItem(GET_ITEM_REQUEST);
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void handleItemRetriesWhenUnknownCompletedTransaction()
		public virtual void handleItemRetriesWhenUnknownCompletedTransaction()
		{
			doReturn(mockTx).when(isolationHandler).loadTransaction(TX_ID);
			doThrow(typeof(UnknownCompletedTransactionException)).when(isolationHandler).getOldCommittedItem(mockTx, TABLE_NAME, KEY);
			when(mockTxManager.CreateKeyMapAsync(TABLE_NAME, NON_TRANSIENT_APPLIED_ITEM)).thenReturn(KEY);
			when(mockClient.getItem(GET_ITEM_REQUEST)).thenReturn(new GetItemResult {
Item = NON_TRANSIENT_APPLIED_ITEM,)
};
			bool caughtException = false;
			try
			{
				isolationHandler.HandleItemAsync(NON_TRANSIENT_APPLIED_ITEM, TABLE_NAME, 1);
			}
			catch (TransactionException)
			{
				caughtException = true;
			}
			assertTrue(caughtException);
			verify(isolationHandler, times(2)).loadTransaction(TX_ID);
			verify(isolationHandler).createGetItemRequest(TABLE_NAME, NON_TRANSIENT_APPLIED_ITEM);
			verify(mockClient).getItem(GET_ITEM_REQUEST);
		}
	}

}