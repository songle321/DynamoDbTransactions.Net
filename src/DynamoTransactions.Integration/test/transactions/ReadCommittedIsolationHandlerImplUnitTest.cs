using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using com.amazonaws.services.dynamodbv2.transactions.exceptions;
using Moq;
using Xunit;
using Xunit.Abstractions;
using static DynamoTransactions.Integration.AssertStatic;
using static com.amazonaws.services.dynamodbv2.transactions.ReadUncommittedIsolationHandlerImplUnitTest;

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
        protected internal static GetItemRequest GET_ITEM_REQUEST = new GetItemRequest
        {
            TableName = TABLE_NAME,
            Key = KEY,
            ConsistentRead = true
        };

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Mock private TransactionManager mockTxManager;
        private Mock<TransactionManager> mockTxManager;

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Mock private Transaction mockTx;
        private Mock<Transaction> mockTx;

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Mock private TransactionItem mockTxItem;
        private Mock<TransactionItem> mockTxItem;

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Mock private Request mockRequest;
        private Mock<Request> mockRequest;

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Mock private com.amazonaws.services.dynamodbv2.AmazonDynamoDBClient mockClient;
        private Mock<AmazonDynamoDBClient> mockClient;

        private ReadCommittedIsolationHandlerImpl isolationHandler;
        private Mock<ReadCommittedIsolationHandlerImpl> mockIsolationHandler;

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Before public void setup()
        public virtual void setup()
        {
            mockIsolationHandler = new Mock<ReadCommittedIsolationHandlerImpl>();
            mockIsolationHandler.CallBase = true;
            isolationHandler = mockIsolationHandler.Object;

            mockTxItem = new Mock<TransactionItem>();

            mockRequest = new Mock<Request>();

            mockClient = new Mock<AmazonDynamoDBClient>();

            mockTx = new Mock<Transaction>();
            mockTx.SetupGet(x => x.TxItem).Returns(mockTxItem.Object);
            mockTx.SetupGet(x => x.Id).Returns(TX_ID);

            //isolationHandler = spy(new ReadCommittedIsolationHandlerImpl(mockTxManager, 0));
            //when(mockTx.TxItem).thenReturn(mockTxItem);
            //when(mockTx.Id).thenReturn(TX_ID);
            //when(mockTxManager.Client).thenReturn(mockClient);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void checkItemCommittedReturnsNullForNullItem()
        [Fact]
        public virtual void checkItemCommittedReturnsNullForNullItem()
        {
            setup();
            assertNull(isolationHandler.checkItemCommitted(null));
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void checkItemCommittedReturnsItemForUnlockedItem()
        [Fact]
        public virtual void checkItemCommittedReturnsItemForUnlockedItem()
        {
            setup();
            assertEquals(UNLOCKED_ITEM, isolationHandler.checkItemCommitted(UNLOCKED_ITEM));
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void checkItemCommittedReturnsNullForTransientItem()
        [Fact]
        public virtual void checkItemCommittedReturnsNullForTransientItem()
        {
            setup();
            assertNull(isolationHandler.checkItemCommitted(TRANSIENT_APPLIED_ITEM));
            assertNull(isolationHandler.checkItemCommitted(TRANSIENT_UNAPPLIED_ITEM));
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test(expected = com.amazonaws.services.dynamodbv2.transactions.exceptions.TransactionException.class) public void checkItemCommittedThrowsExceptionForNonTransientAppliedItem()
        [Fact]
        public virtual void checkItemCommittedThrowsExceptionForNonTransientAppliedItem()
        {
            setup();
            Assert.Throws<TransactionException>(() => isolationHandler.checkItemCommitted(NON_TRANSIENT_APPLIED_ITEM));
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void filterAttributesToGetReturnsNullForNullItem()
        [Fact]
        public virtual void filterAttributesToGetReturnsNullForNullItem()
        {
            setup();
            isolationHandler.filterAttributesToGet(null, null);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void filterAttributesToGetReturnsItemWhenAttributesToGetIsNull()
        [Fact]
        public virtual void filterAttributesToGetReturnsItemWhenAttributesToGetIsNull()
        {
            setup();
            Dictionary<string, AttributeValue> result = isolationHandler.filterAttributesToGet(UNLOCKED_ITEM, null);
            assertEquals(UNLOCKED_ITEM, result);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void filterAttributesToGetReturnsItemWhenAttributesToGetIsEmpty()
        [Fact]
        public virtual void filterAttributesToGetReturnsItemWhenAttributesToGetIsEmpty()
        {
            setup();
            Dictionary<string, AttributeValue> result = isolationHandler.filterAttributesToGet(UNLOCKED_ITEM, new List<string>());
            assertEquals(UNLOCKED_ITEM, result);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void filterAttributesToGetReturnsItemWhenAttributesToGetContainsAllAttributes()
        [Fact]
        public virtual void filterAttributesToGetReturnsItemWhenAttributesToGetContainsAllAttributes()
        {
            setup();
            List<string> attributesToGet = Arrays.asList("Id", "attr1"); // all attributes
            Dictionary<string, AttributeValue> result = isolationHandler.filterAttributesToGet(UNLOCKED_ITEM, attributesToGet);
            assertEquals(UNLOCKED_ITEM, result);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void filterAttributesToGetReturnsOnlySpecifiedAttributesWhenSpecified()
        [Fact]
        public virtual void filterAttributesToGetReturnsOnlySpecifiedAttributesWhenSpecified()
        {
            setup();
            List<string> attributesToGet = Arrays.asList("Id"); // only keep the key
            Dictionary<string, AttributeValue> result = isolationHandler.filterAttributesToGet(UNLOCKED_ITEM, attributesToGet);
            assertEquals(KEY, result);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test(expected = com.amazonaws.services.dynamodbv2.transactions.exceptions.TransactionAssertionException.class) public void getOldCommittedItemThrowsExceptionIfNoLockingRequestExists()
        [Fact]
        public virtual void getOldCommittedItemThrowsExceptionIfNoLockingRequestExists()
        {
            setup();
            mockTxItem.Setup(x => x.getRequestForKey(TABLE_NAME, KEY)).Returns((Request) null);
            //when(mockTxItem.getRequestForKey(TABLE_NAME, KEY)).thenReturn(null);
            Assert.Throws<TransactionAssertionException>(() => isolationHandler.GetOldCommittedItemAsync(mockTx.Object, TABLE_NAME, KEY).Wait());
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test(expected = com.amazonaws.services.dynamodbv2.transactions.exceptions.UnknownCompletedTransactionException.class) public void getOldCommittedItemThrowsExceptionIfOldItemDoesNotExist()
        [Fact]
        public virtual void getOldCommittedItemThrowsExceptionIfOldItemDoesNotExist()
        {
            setup();
            mockTxItem.Setup(x => x.getRequestForKey(TABLE_NAME, KEY)).Returns(mockRequest.Object);
            //when(mockTxItem.getRequestForKey(TABLE_NAME, KEY)).thenReturn(mockRequest);

            mockRequest.SetupGet(x => x.Rid).Returns(RID);
            //when(mockRequest.Rid).thenReturn(RID);

            mockTxItem.Setup(x => x.loadItemImageAsync(RID)).Returns(Task.FromResult((Dictionary<string, AttributeValue>)  null));
            //when(mockTxItem.loadItemImageAsync(RID)).thenReturn(null);

            isolationHandler.GetOldCommittedItemAsync(mockTx.Object, TABLE_NAME, KEY);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void getOldCommittedItemReturnsOldImageIfOldItemExists()
        [Fact]
        public virtual void getOldCommittedItemReturnsOldImageIfOldItemExists()
        {
            setup();
            mockTxItem.Setup(x => x.getRequestForKey(TABLE_NAME, KEY)).Returns(mockRequest.Object);
            //when(mockTxItem.getRequestForKey(TABLE_NAME, KEY)).thenReturn(mockRequest);

            mockRequest.SetupGet(x => x.Rid).Returns(RID);
            //when(mockRequest.Rid).thenReturn(RID);

            mockTxItem.Setup(x => x.loadItemImageAsync(RID)).Returns(Task.FromResult(UNLOCKED_ITEM));
            //when(mockTxItem.loadItemImageAsync(RID)).thenReturn(UNLOCKED_ITEM);

            Dictionary<string, AttributeValue> result = isolationHandler.GetOldCommittedItemAsync(mockTx.Object, TABLE_NAME, KEY).Result;
            assertEquals(UNLOCKED_ITEM, result);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void createGetItemRequestCorrectlyCreatesRequest()
        [Fact]
        public virtual void createGetItemRequestCorrectlyCreatesRequest()
        {
            setup();
            mockTxManager.Setup(x => x.CreateKeyMapAsync(TABLE_NAME, NON_TRANSIENT_APPLIED_ITEM, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(KEY));
            //when(mockTxManager.CreateKeyMapAsync(TABLE_NAME, NON_TRANSIENT_APPLIED_ITEM)).thenReturn(KEY);
            GetItemRequest request = isolationHandler
                .CreateGetItemRequestAsync(TABLE_NAME, NON_TRANSIENT_APPLIED_ITEM, CancellationToken.None).Result;
            assertEquals(TABLE_NAME, request.TableName);
            assertEquals(KEY, request.Key);
            assertEquals(null, request.AttributesToGet);
            assertTrue(request.ConsistentRead);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void handleItemReturnsNullForNullItem()
        [Fact]
        public virtual void handleItemReturnsNullForNullItem()
        {
            setup();
            assertNull(isolationHandler.HandleItemAsync(null, TABLE_NAME, 0, CancellationToken.None).Result);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void handleItemReturnsItemForUnlockedItem()
        [Fact]
        public virtual void handleItemReturnsItemForUnlockedItem()
        {
            setup();
            assertEquals(UNLOCKED_ITEM, isolationHandler.HandleItemAsync(UNLOCKED_ITEM, TABLE_NAME, 0, CancellationToken.None).Result);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void handleItemReturnsNullForTransientItem()
        [Fact]
        public virtual void handleItemReturnsNullForTransientItem()
        {
            setup();
            assertNull(isolationHandler.HandleItemAsync(TRANSIENT_APPLIED_ITEM, TABLE_NAME, 0, CancellationToken.None).Result);
            assertNull(isolationHandler.HandleItemAsync(TRANSIENT_UNAPPLIED_ITEM, TABLE_NAME, 0, CancellationToken.None).Result);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test(expected = com.amazonaws.services.dynamodbv2.transactions.exceptions.TransactionException.class) public void handleItemThrowsExceptionForNonTransientAppliedItemWithNoCorrespondingTx()
        [Fact]
        public virtual void handleItemThrowsExceptionForNonTransientAppliedItemWithNoCorrespondingTx()
        {
            setup();
            Assert.Throws<TransactionNotFoundException>(() => isolationHandler.loadTransaction(TX_ID));
            //doThrow(typeof(TransactionNotFoundException)).when(isolationHandler).loadTransaction(TX_ID);
            Assert.Throws<TransactionNotFoundException>(() => isolationHandler.HandleItemAsync(NON_TRANSIENT_APPLIED_ITEM, TABLE_NAME, 0, CancellationToken.None).Result);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void handleItemReturnsItemForNonTransientAppliedItemWithCommittedTxItem()
        [Fact]
        public virtual void handleItemReturnsItemForNonTransientAppliedItemWithCommittedTxItem()
        {
            setup();
            mockIsolationHandler.Setup(x => x.loadTransaction(TX_ID)).Returns(mockTx.Object);
            //doReturn(mockTx).when(isolationHandler).loadTransaction(TX_ID);
            mockTxItem.Setup(x => x.getState()).Returns(TransactionItem.State.COMMITTED);
            //when(mockTxItem.getState()).thenReturn(TransactionItem.State.COMMITTED);
            assertEquals(NON_TRANSIENT_APPLIED_ITEM, isolationHandler
                .HandleItemAsync(NON_TRANSIENT_APPLIED_ITEM, TABLE_NAME, 0, CancellationToken.None).Result);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void handleItemReturnsOldVersionOfItemForNonTransientAppliedItemWithPendingTxItem()
        [Fact]
        public virtual void handleItemReturnsOldVersionOfItemForNonTransientAppliedItemWithPendingTxItem()
        {
            setup();
            mockIsolationHandler.Setup(x => x.loadTransaction(TX_ID)).Returns(mockTx.Object);
            //doReturn(mockTx).when(isolationHandler).loadTransaction(TX_ID);
            mockIsolationHandler.Setup(x => x.GetOldCommittedItemAsync(mockTx.Object, TABLE_NAME, KEY))
                .Returns(Task.FromResult(UNLOCKED_ITEM));
            //doReturn(UNLOCKED_ITEM).when(isolationHandler).getOldCommittedItem(mockTx, TABLE_NAME, KEY);
            mockTxManager
                .Setup(x => x.CreateKeyMapAsync(TABLE_NAME, NON_TRANSIENT_APPLIED_ITEM, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(KEY));
            //when(mockTxManager.CreateKeyMapAsync(TABLE_NAME, NON_TRANSIENT_APPLIED_ITEM)).thenReturn(KEY);
            mockTxItem.SetupGet(x => x.getState()).Returns(TransactionItem.State.PENDING);
            //when(mockTxItem.getState()).thenReturn(TransactionItem.State.PENDING);
            mockTxItem.Setup(x => x.getRequestForKey(TABLE_NAME, KEY)).Returns(mockRequest.Object);
            //when(mockTxItem.getRequestForKey(TABLE_NAME, KEY)).thenReturn(mockRequest);
            assertEquals(UNLOCKED_ITEM, isolationHandler
                .HandleItemAsync(NON_TRANSIENT_APPLIED_ITEM, TABLE_NAME, 0, CancellationToken.None).Result);
            mockIsolationHandler.Verify(x => x.loadTransaction(TX_ID));
            //verify(isolationHandler).loadTransaction(TX_ID);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test(expected = com.amazonaws.services.dynamodbv2.transactions.exceptions.TransactionException.class) public void handleItemThrowsExceptionForNonTransientAppliedItemWithPendingTxItemWithNoOldVersionAndNoRetries()
        [Fact]
        public virtual void handleItemThrowsExceptionForNonTransientAppliedItemWithPendingTxItemWithNoOldVersionAndNoRetries()
        {
            setup();
            mockIsolationHandler.Setup(x => x.loadTransaction(TX_ID)).Returns(mockTx.Object);
            //doReturn(mockTx).when(isolationHandler).loadTransaction(TX_ID);
            Assert.Throws<UnknownCompletedTransactionException>(
                () => isolationHandler.GetOldCommittedItemAsync(mockTx.Object, TABLE_NAME, KEY).Wait());
            //doThrow(typeof(UnknownCompletedTransactionException)).when(isolationHandler).getOldCommittedItem(mockTx, TABLE_NAME, KEY);
            mockTxItem.Setup(x => x.getState()).Returns(TransactionItem.State.PENDING);
            //when(mockTxItem.getState()).thenReturn(TransactionItem.State.PENDING);
            mockTxItem.Setup(x => x.getRequestForKey(TABLE_NAME, KEY)).Returns(mockRequest.Object);
            //when(mockTxItem.getRequestForKey(TABLE_NAME, KEY)).thenReturn(mockRequest);
            isolationHandler.HandleItemAsync(NON_TRANSIENT_APPLIED_ITEM, TABLE_NAME, 0, CancellationToken.None).Wait();
            mockIsolationHandler.Verify(x => x.loadTransaction(TX_ID));
            //verify(isolationHandler).loadTransaction(TX_ID);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void handleItemRetriesWhenTransactionNotFound()
        [Fact]
        public virtual void handleItemRetriesWhenTransactionNotFound()
        {
            setup();
            Assert.Throws<TransactionNotFoundException>(() => isolationHandler.loadTransaction(TX_ID));
            //doThrow(typeof(TransactionNotFoundException)).when(isolationHandler).loadTransaction(TX_ID);
            mockTxManager
                .Setup(x => x.CreateKeyMapAsync(TABLE_NAME, NON_TRANSIENT_APPLIED_ITEM, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(KEY));
            //when(mockTxManager.CreateKeyMapAsync(TABLE_NAME, NON_TRANSIENT_APPLIED_ITEM)).thenReturn(KEY);
            mockClient
                .Setup(x => x.GetItemAsync(GET_ITEM_REQUEST, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new GetItemResponse
            {
                Item = NON_TRANSIENT_APPLIED_ITEM
            }));
            //when(mockClient.GetItemAsync(GET_ITEM_REQUEST)).thenReturn(new GetItemResponse
            //{
            //    Item = NON_TRANSIENT_APPLIED_ITEM
            //});
            bool caughtException = false;
            try
            {
                isolationHandler.HandleItemAsync(NON_TRANSIENT_APPLIED_ITEM, TABLE_NAME, 1, CancellationToken.None).Wait();
            }
            catch (TransactionException)
            {
                caughtException = true;
            }

            assertTrue(caughtException);

            mockIsolationHandler.Verify(x => x.loadTransaction(TX_ID), Times.Exactly(2));
            //verify(isolationHandler, times(2)).loadTransaction(TX_ID);

            mockIsolationHandler.Verify(x => x.CreateGetItemRequestAsync(
                TABLE_NAME, NON_TRANSIENT_APPLIED_ITEM, It.IsAny<CancellationToken>()));
            //verify(isolationHandler).createGetItemRequest(TABLE_NAME, NON_TRANSIENT_APPLIED_ITEM);

            mockClient.Verify(x => x.GetItemAsync(GET_ITEM_REQUEST, It.IsAny<CancellationToken>()));
            //verify(mockClient).GetItemAsync(GET_ITEM_REQUEST);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void handleItemRetriesWhenUnknownCompletedTransaction()
        [Fact]
        public virtual void handleItemRetriesWhenUnknownCompletedTransaction()
        {
            setup();
            mockIsolationHandler.Setup(x => x.loadTransaction(TX_ID)).Returns(mockTx.Object);
            //doReturn(mockTx).when(isolationHandler).loadTransaction(TX_ID);
            Assert.Throws<UnknownCompletedTransactionException>(
                () => isolationHandler.GetOldCommittedItemAsync(mockTx.Object, TABLE_NAME, KEY).Wait());
            //doThrow(typeof(UnknownCompletedTransactionException)).when(isolationHandler).getOldCommittedItem(mockTx, TABLE_NAME, KEY);
            mockTxManager
                .Setup(x => x.CreateKeyMapAsync(TABLE_NAME, NON_TRANSIENT_APPLIED_ITEM, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(KEY));
            //when(mockTxManager.CreateKeyMapAsync(TABLE_NAME, NON_TRANSIENT_APPLIED_ITEM)).thenReturn(KEY);
            mockClient
                .Setup(x => x.GetItemAsync(GET_ITEM_REQUEST, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new GetItemResponse
                {
                    Item = NON_TRANSIENT_APPLIED_ITEM
                }));
            //when(mockClient.GetItemAsync(GET_ITEM_REQUEST)).thenReturn(new GetItemResponse
            //{
            //    Item = NON_TRANSIENT_APPLIED_ITEM
            //});
            bool caughtException = false;
            try
            {
                isolationHandler.HandleItemAsync(NON_TRANSIENT_APPLIED_ITEM, TABLE_NAME, 1, CancellationToken.None).Wait();
            }
            catch (TransactionException)
            {
                caughtException = true;
            }

            assertTrue(caughtException);

            mockIsolationHandler.Verify(x => x.loadTransaction(TX_ID), Times.Exactly(2));
            //verify(isolationHandler, times(2)).loadTransaction(TX_ID);

            mockIsolationHandler.Verify(x => x.CreateGetItemRequestAsync(
                TABLE_NAME, NON_TRANSIENT_APPLIED_ITEM, It.IsAny<CancellationToken>()));
            //verify(isolationHandler).createGetItemRequest(TABLE_NAME, NON_TRANSIENT_APPLIED_ITEM);

            mockClient.Verify(x => x.GetItemAsync(GET_ITEM_REQUEST, It.IsAny<CancellationToken>()));
            //verify(mockClient).GetItemAsync(GET_ITEM_REQUEST);
        }
    }

}