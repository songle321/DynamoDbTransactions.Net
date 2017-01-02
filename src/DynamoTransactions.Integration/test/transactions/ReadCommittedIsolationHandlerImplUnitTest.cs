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
        protected internal const int Rid = 1;
        protected internal static GetItemRequest GetItemRequest = new GetItemRequest
        {
            TableName = TableName,
            Key = Key,
            ConsistentRead = true
        };

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Mock private TransactionManager mockTxManager;
        private Mock<TransactionManager> _mockTxManager;

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Mock private Transaction mockTx;
        private Mock<Transaction> _mockTx;

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Mock private TransactionItem mockTxItem;
        private Mock<TransactionItem> _mockTxItem;

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Mock private Request mockRequest;
        private Mock<Request> _mockRequest;

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Mock private com.amazonaws.services.dynamodbv2.AmazonDynamoDBClient mockClient;
        private Mock<AmazonDynamoDBClient> _mockClient;

        private ReadCommittedIsolationHandlerImpl _isolationHandler;
        private Mock<ReadCommittedIsolationHandlerImpl> _mockIsolationHandler;

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Before public void setup()
        public virtual void Setup()
        {
            _mockIsolationHandler = new Mock<ReadCommittedIsolationHandlerImpl>();
            _mockIsolationHandler.CallBase = true;
            _isolationHandler = _mockIsolationHandler.Object;

            _mockTxItem = new Mock<TransactionItem>();

            _mockRequest = new Mock<Request>();

            _mockClient = new Mock<AmazonDynamoDBClient>();

            _mockTxManager = new Mock<TransactionManager>();

            _mockTx = new Mock<Transaction>();
            _mockTx.SetupGet(x => x.TxItem).Returns(_mockTxItem.Object);
            _mockTx.SetupGet(x => x.Id).Returns(TxId);

            //isolationHandler = spy(new ReadCommittedIsolationHandlerImpl(mockTxManager, 0));
            //when(mockTx.TxItem).thenReturn(mockTxItem);
            //when(mockTx.Id).thenReturn(TX_ID);
            //when(mockTxManager.Client).thenReturn(mockClient);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void checkItemCommittedReturnsNullForNullItem()
        [Fact]
        public virtual void CheckItemCommittedReturnsNullForNullItem()
        {
            Setup();
            AssertNull(_isolationHandler.CheckItemCommitted(null));
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void checkItemCommittedReturnsItemForUnlockedItem()
        [Fact]
        public virtual void CheckItemCommittedReturnsItemForUnlockedItem()
        {
            Setup();
            AssertEquals(UnlockedItem, _isolationHandler.CheckItemCommitted(UnlockedItem));
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void checkItemCommittedReturnsNullForTransientItem()
        [Fact]
        public virtual void CheckItemCommittedReturnsNullForTransientItem()
        {
            Setup();
            AssertNull(_isolationHandler.CheckItemCommitted(TransientAppliedItem));
            AssertNull(_isolationHandler.CheckItemCommitted(TransientUnappliedItem));
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test(expected = com.amazonaws.services.dynamodbv2.transactions.exceptions.TransactionException.class) public void checkItemCommittedThrowsExceptionForNonTransientAppliedItem()
        [Fact]
        public virtual void CheckItemCommittedThrowsExceptionForNonTransientAppliedItem()
        {
            Setup();
            Assert.Throws<TransactionException>(() => _isolationHandler.CheckItemCommitted(NonTransientAppliedItem));
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void filterAttributesToGetReturnsNullForNullItem()
        [Fact]
        public virtual void FilterAttributesToGetReturnsNullForNullItem()
        {
            Setup();
            _isolationHandler.FilterAttributesToGet(null, null);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void filterAttributesToGetReturnsItemWhenAttributesToGetIsNull()
        [Fact]
        public virtual void FilterAttributesToGetReturnsItemWhenAttributesToGetIsNull()
        {
            Setup();
            Dictionary<string, AttributeValue> result = _isolationHandler.FilterAttributesToGet(UnlockedItem, null);
            AssertEquals(UnlockedItem, result);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void filterAttributesToGetReturnsItemWhenAttributesToGetIsEmpty()
        [Fact]
        public virtual void FilterAttributesToGetReturnsItemWhenAttributesToGetIsEmpty()
        {
            Setup();
            Dictionary<string, AttributeValue> result = _isolationHandler.FilterAttributesToGet(UnlockedItem, new List<string>());
            AssertEquals(UnlockedItem, result);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void filterAttributesToGetReturnsItemWhenAttributesToGetContainsAllAttributes()
        [Fact]
        public virtual void FilterAttributesToGetReturnsItemWhenAttributesToGetContainsAllAttributes()
        {
            Setup();
            List<string> attributesToGet = Arrays.AsList("Id", "attr1"); // all attributes
            Dictionary<string, AttributeValue> result = _isolationHandler.FilterAttributesToGet(UnlockedItem, attributesToGet);
            AssertEquals(UnlockedItem, result);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void filterAttributesToGetReturnsOnlySpecifiedAttributesWhenSpecified()
        [Fact]
        public virtual void FilterAttributesToGetReturnsOnlySpecifiedAttributesWhenSpecified()
        {
            Setup();
            List<string> attributesToGet = Arrays.AsList("Id"); // only keep the key
            Dictionary<string, AttributeValue> result = _isolationHandler.FilterAttributesToGet(UnlockedItem, attributesToGet);
            AssertEquals(Key, result);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test(expected = com.amazonaws.services.dynamodbv2.transactions.exceptions.TransactionAssertionException.class) public void getOldCommittedItemThrowsExceptionIfNoLockingRequestExists()
        [Fact]
        public virtual void GetOldCommittedItemThrowsExceptionIfNoLockingRequestExists()
        {
            Setup();
            _mockTxItem.Setup(x => x.GetRequestForKey(TableName, Key)).Returns((Request) null);
            //when(mockTxItem.getRequestForKey(TABLE_NAME, KEY)).thenReturn(null);
            Assert.Throws<TransactionAssertionException>(() => _isolationHandler.GetOldCommittedItemAsync(_mockTx.Object, TableName, Key).Wait());
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test(expected = com.amazonaws.services.dynamodbv2.transactions.exceptions.UnknownCompletedTransactionException.class) public void getOldCommittedItemThrowsExceptionIfOldItemDoesNotExist()
        [Fact]
        public virtual void GetOldCommittedItemThrowsExceptionIfOldItemDoesNotExist()
        {
            Setup();
            _mockTxItem.Setup(x => x.GetRequestForKey(TableName, Key)).Returns(_mockRequest.Object);
            //when(mockTxItem.getRequestForKey(TABLE_NAME, KEY)).thenReturn(mockRequest);

            _mockRequest.SetupGet(x => x.Rid).Returns(Rid);
            //when(mockRequest.Rid).thenReturn(RID);

            _mockTxItem.Setup(x => x.LoadItemImageAsync(Rid)).Returns(Task.FromResult((Dictionary<string, AttributeValue>)  null));
            //when(mockTxItem.loadItemImageAsync(RID)).thenReturn(null);

            _isolationHandler.GetOldCommittedItemAsync(_mockTx.Object, TableName, Key).Wait();
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void getOldCommittedItemReturnsOldImageIfOldItemExists()
        [Fact]
        public virtual void GetOldCommittedItemReturnsOldImageIfOldItemExists()
        {
            Setup();
            _mockTxItem.Setup(x => x.GetRequestForKey(TableName, Key)).Returns(_mockRequest.Object);
            //when(mockTxItem.getRequestForKey(TABLE_NAME, KEY)).thenReturn(mockRequest);

            _mockRequest.SetupGet(x => x.Rid).Returns(Rid);
            //when(mockRequest.Rid).thenReturn(RID);

            _mockTxItem.Setup(x => x.LoadItemImageAsync(Rid)).Returns(Task.FromResult(UnlockedItem));
            //when(mockTxItem.loadItemImageAsync(RID)).thenReturn(UNLOCKED_ITEM);

            Dictionary<string, AttributeValue> result = _isolationHandler.GetOldCommittedItemAsync(_mockTx.Object, TableName, Key).Result;
            AssertEquals(UnlockedItem, result);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void createGetItemRequestCorrectlyCreatesRequest()
        [Fact]
        public virtual void CreateGetItemRequestCorrectlyCreatesRequest()
        {
            Setup();
            _mockTxManager.Setup(x => x.CreateKeyMapAsync(TableName, NonTransientAppliedItem, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(Key));
            //when(mockTxManager.CreateKeyMapAsync(TABLE_NAME, NON_TRANSIENT_APPLIED_ITEM)).thenReturn(KEY);
            GetItemRequest request = _isolationHandler
                .CreateGetItemRequestAsync(TableName, NonTransientAppliedItem, CancellationToken.None).Result;
            AssertEquals(TableName, request.TableName);
            AssertEquals(Key, request.Key);
            AssertEquals(null, request.AttributesToGet);
            AssertTrue(request.ConsistentRead);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void handleItemReturnsNullForNullItem()
        [Fact]
        public virtual void HandleItemReturnsNullForNullItem()
        {
            Setup();
            AssertNull(_isolationHandler.HandleItemAsync(null, TableName, 0, CancellationToken.None).Result);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void handleItemReturnsItemForUnlockedItem()
        [Fact]
        public virtual void HandleItemReturnsItemForUnlockedItem()
        {
            Setup();
            AssertEquals(UnlockedItem, _isolationHandler.HandleItemAsync(UnlockedItem, TableName, 0, CancellationToken.None).Result);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void handleItemReturnsNullForTransientItem()
        [Fact]
        public virtual void HandleItemReturnsNullForTransientItem()
        {
            Setup();
            AssertNull(_isolationHandler.HandleItemAsync(TransientAppliedItem, TableName, 0, CancellationToken.None).Result);
            AssertNull(_isolationHandler.HandleItemAsync(TransientUnappliedItem, TableName, 0, CancellationToken.None).Result);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test(expected = com.amazonaws.services.dynamodbv2.transactions.exceptions.TransactionException.class) public void handleItemThrowsExceptionForNonTransientAppliedItemWithNoCorrespondingTx()
        [Fact]
        public virtual void HandleItemThrowsExceptionForNonTransientAppliedItemWithNoCorrespondingTx()
        {
            Setup();
            Assert.Throws<TransactionNotFoundException>(() => _isolationHandler.LoadTransaction(TxId));
            //doThrow(typeof(TransactionNotFoundException)).when(isolationHandler).loadTransaction(TX_ID);
            Assert.Throws<TransactionNotFoundException>(() => _isolationHandler.HandleItemAsync(NonTransientAppliedItem, TableName, 0, CancellationToken.None).Result);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void handleItemReturnsItemForNonTransientAppliedItemWithCommittedTxItem()
        [Fact]
        public virtual void HandleItemReturnsItemForNonTransientAppliedItemWithCommittedTxItem()
        {
            Setup();
            _mockIsolationHandler.Setup(x => x.LoadTransaction(TxId)).Returns(_mockTx.Object);
            //doReturn(mockTx).when(isolationHandler).loadTransaction(TX_ID);
            _mockTxItem.Setup(x => x.GetState()).Returns(TransactionItem.State.Committed);
            //when(mockTxItem.getState()).thenReturn(TransactionItem.State.COMMITTED);
            AssertEquals(NonTransientAppliedItem, _isolationHandler
                .HandleItemAsync(NonTransientAppliedItem, TableName, 0, CancellationToken.None).Result);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void handleItemReturnsOldVersionOfItemForNonTransientAppliedItemWithPendingTxItem()
        [Fact]
        public virtual void HandleItemReturnsOldVersionOfItemForNonTransientAppliedItemWithPendingTxItem()
        {
            Setup();
            _mockIsolationHandler.Setup(x => x.LoadTransaction(TxId)).Returns(_mockTx.Object);
            //doReturn(mockTx).when(isolationHandler).loadTransaction(TX_ID);
            _mockIsolationHandler.Setup(x => x.GetOldCommittedItemAsync(_mockTx.Object, TableName, Key))
                .Returns(Task.FromResult(UnlockedItem));
            //doReturn(UNLOCKED_ITEM).when(isolationHandler).getOldCommittedItem(mockTx, TABLE_NAME, KEY);
            _mockTxManager
                .Setup(x => x.CreateKeyMapAsync(TableName, NonTransientAppliedItem, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(Key));
            //when(mockTxManager.CreateKeyMapAsync(TABLE_NAME, NON_TRANSIENT_APPLIED_ITEM)).thenReturn(KEY);
            _mockTxItem.SetupGet(x => x.GetState()).Returns(TransactionItem.State.Pending);
            //when(mockTxItem.getState()).thenReturn(TransactionItem.State.PENDING);
            _mockTxItem.Setup(x => x.GetRequestForKey(TableName, Key)).Returns(_mockRequest.Object);
            //when(mockTxItem.getRequestForKey(TABLE_NAME, KEY)).thenReturn(mockRequest);
            AssertEquals(UnlockedItem, _isolationHandler
                .HandleItemAsync(NonTransientAppliedItem, TableName, 0, CancellationToken.None).Result);
            _mockIsolationHandler.Verify(x => x.LoadTransaction(TxId));
            //verify(isolationHandler).loadTransaction(TX_ID);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test(expected = com.amazonaws.services.dynamodbv2.transactions.exceptions.TransactionException.class) public void handleItemThrowsExceptionForNonTransientAppliedItemWithPendingTxItemWithNoOldVersionAndNoRetries()
        [Fact]
        public virtual void HandleItemThrowsExceptionForNonTransientAppliedItemWithPendingTxItemWithNoOldVersionAndNoRetries()
        {
            Setup();
            _mockIsolationHandler.Setup(x => x.LoadTransaction(TxId)).Returns(_mockTx.Object);
            //doReturn(mockTx).when(isolationHandler).loadTransaction(TX_ID);
            Assert.Throws<UnknownCompletedTransactionException>(
                () => _isolationHandler.GetOldCommittedItemAsync(_mockTx.Object, TableName, Key).Wait());
            //doThrow(typeof(UnknownCompletedTransactionException)).when(isolationHandler).getOldCommittedItem(mockTx, TABLE_NAME, KEY);
            _mockTxItem.Setup(x => x.GetState()).Returns(TransactionItem.State.Pending);
            //when(mockTxItem.getState()).thenReturn(TransactionItem.State.PENDING);
            _mockTxItem.Setup(x => x.GetRequestForKey(TableName, Key)).Returns(_mockRequest.Object);
            //when(mockTxItem.getRequestForKey(TABLE_NAME, KEY)).thenReturn(mockRequest);
            _isolationHandler.HandleItemAsync(NonTransientAppliedItem, TableName, 0, CancellationToken.None).Wait();
            _mockIsolationHandler.Verify(x => x.LoadTransaction(TxId));
            //verify(isolationHandler).loadTransaction(TX_ID);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void handleItemRetriesWhenTransactionNotFound()
        [Fact]
        public virtual void HandleItemRetriesWhenTransactionNotFound()
        {
            Setup();
            Assert.Throws<TransactionNotFoundException>(() => _isolationHandler.LoadTransaction(TxId));
            //doThrow(typeof(TransactionNotFoundException)).when(isolationHandler).loadTransaction(TX_ID);
            _mockTxManager
                .Setup(x => x.CreateKeyMapAsync(TableName, NonTransientAppliedItem, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(Key));
            //when(mockTxManager.CreateKeyMapAsync(TABLE_NAME, NON_TRANSIENT_APPLIED_ITEM)).thenReturn(KEY);
            _mockClient
                .Setup(x => x.GetItemAsync(GetItemRequest, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new GetItemResponse
            {
                Item = NonTransientAppliedItem
            }));
            //when(mockClient.GetItemAsync(GET_ITEM_REQUEST)).thenReturn(new GetItemResponse
            //{
            //    Item = NON_TRANSIENT_APPLIED_ITEM
            //});
            bool caughtException = false;
            try
            {
                _isolationHandler.HandleItemAsync(NonTransientAppliedItem, TableName, 1, CancellationToken.None).Wait();
            }
            catch (TransactionException)
            {
                caughtException = true;
            }

            AssertTrue(caughtException);

            _mockIsolationHandler.Verify(x => x.LoadTransaction(TxId), Times.Exactly(2));
            //verify(isolationHandler, times(2)).loadTransaction(TX_ID);

            _mockIsolationHandler.Verify(x => x.CreateGetItemRequestAsync(
                TableName, NonTransientAppliedItem, It.IsAny<CancellationToken>()));
            //verify(isolationHandler).createGetItemRequest(TABLE_NAME, NON_TRANSIENT_APPLIED_ITEM);

            _mockClient.Verify(x => x.GetItemAsync(GetItemRequest, It.IsAny<CancellationToken>()));
            //verify(mockClient).GetItemAsync(GET_ITEM_REQUEST);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void handleItemRetriesWhenUnknownCompletedTransaction()
        [Fact]
        public virtual void HandleItemRetriesWhenUnknownCompletedTransaction()
        {
            Setup();
            _mockIsolationHandler.Setup(x => x.LoadTransaction(TxId)).Returns(_mockTx.Object);
            //doReturn(mockTx).when(isolationHandler).loadTransaction(TX_ID);
            Assert.Throws<UnknownCompletedTransactionException>(
                () => _isolationHandler.GetOldCommittedItemAsync(_mockTx.Object, TableName, Key).Wait());
            //doThrow(typeof(UnknownCompletedTransactionException)).when(isolationHandler).getOldCommittedItem(mockTx, TABLE_NAME, KEY);
            _mockTxManager
                .Setup(x => x.CreateKeyMapAsync(TableName, NonTransientAppliedItem, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(Key));
            //when(mockTxManager.CreateKeyMapAsync(TABLE_NAME, NON_TRANSIENT_APPLIED_ITEM)).thenReturn(KEY);
            _mockClient
                .Setup(x => x.GetItemAsync(GetItemRequest, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new GetItemResponse
                {
                    Item = NonTransientAppliedItem
                }));
            //when(mockClient.GetItemAsync(GET_ITEM_REQUEST)).thenReturn(new GetItemResponse
            //{
            //    Item = NON_TRANSIENT_APPLIED_ITEM
            //});
            bool caughtException = false;
            try
            {
                _isolationHandler.HandleItemAsync(NonTransientAppliedItem, TableName, 1, CancellationToken.None).Wait();
            }
            catch (TransactionException)
            {
                caughtException = true;
            }

            AssertTrue(caughtException);

            _mockIsolationHandler.Verify(x => x.LoadTransaction(TxId), Times.Exactly(2));
            //verify(isolationHandler, times(2)).loadTransaction(TX_ID);

            _mockIsolationHandler.Verify(x => x.CreateGetItemRequestAsync(
                TableName, NonTransientAppliedItem, It.IsAny<CancellationToken>()));
            //verify(isolationHandler).createGetItemRequest(TABLE_NAME, NON_TRANSIENT_APPLIED_ITEM);

            _mockClient.Verify(x => x.GetItemAsync(GetItemRequest, It.IsAny<CancellationToken>()));
            //verify(mockClient).GetItemAsync(GET_ITEM_REQUEST);
        }
    }

}