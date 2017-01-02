using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using com.amazonaws.services.dynamodbv2.transactions;
using com.amazonaws.services.dynamodbv2.transactions.exceptions;
using Xunit;
using static DynamoTransactions.Integration.AssertStatic;

// <summary>
// Copyright 2013-2013 Amazon.com, Inc. or its affiliates. All Rights Reserved.
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
    public class RequestTest
    {
        private const string TableName = "Dummy";
        private const string HashAttrName = "Foo";
        private static readonly List<KeySchemaElement> HashSchema = Arrays.AsList(new KeySchemaElement
        {
            AttributeName = HashAttrName,
            KeyType = KeyType.HASH
        });

        internal static readonly Dictionary<string, AttributeValue> JsonMAttrVal = new Dictionary<string, AttributeValue>();
        private static readonly Dictionary<string, ExpectedAttributeValue> NonnullExpectedAttrValues = new Dictionary<string, ExpectedAttributeValue>();
        private static readonly Dictionary<string, string> NonnullExpAttrNames = new Dictionary<string, string>();
        private static readonly Dictionary<string, AttributeValue> NonnullExpAttrValues = new Dictionary<string, AttributeValue>();
        private static readonly Dictionary<string, AttributeValue> BasicItem = new Dictionary<string, AttributeValue>();

        static RequestTest()
        {
            JsonMAttrVal["attr_s"] = new AttributeValue
            {
                S = "s"
            };
            JsonMAttrVal["attr_n"] = new AttributeValue
            {
                N = "1"
            };
            JsonMAttrVal["attr_b"] = new AttributeValue
            {
                B = new MemoryStream(Encoding.ASCII.GetBytes("asdf"))
            };
            JsonMAttrVal["attr_ss"] = new AttributeValue
            {
                SS = { "a", "b" }
            };
            JsonMAttrVal["attr_ns"] = new AttributeValue
            {
                NS = { "1", "2" }
            };
            JsonMAttrVal["attr_bs"] = new AttributeValue
            {
                BS =
                {
                    new MemoryStream(Encoding.ASCII.GetBytes("asdf")),
                    new MemoryStream(Encoding.ASCII.GetBytes("ghjk"))
                }
            };
            JsonMAttrVal["attr_bool"] = new AttributeValue
            {
                BOOL = true
            };
            JsonMAttrVal["attr_l"] = new AttributeValue
            {
                L =
                {
                    new AttributeValue
                    {
                        S = "s",
                    },
                    new AttributeValue
                    {
                        N = "1",
                    },
                    new AttributeValue
                    {
                        B = new MemoryStream(Encoding.UTF8.GetBytes("asdf"))
                    },
                    new AttributeValue
                    {
                        BOOL = true,
                    },
                    new AttributeValue
                    {
                        NULL = true
                    }
                }
            };
            JsonMAttrVal["attr_null"] = new AttributeValue
            {
                NULL = true
            };

            BasicItem[HashAttrName] = new AttributeValue("a");
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void validPut()
        [Fact]
        public virtual void ValidPut()
        {
            Request.PutItem r = new Request.PutItem();
            Dictionary<string, AttributeValue> item = new Dictionary<string, AttributeValue>();
            item[HashAttrName] = new AttributeValue("a");
            r.Request = new PutItemRequest
            {
                TableName = TableName,
                Item = item
            };
            r.ValidateAsync("1", new MockTransactionManager(this, HashSchema)).Wait();
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void putNullTableName()
        [Fact]
        public virtual void PutNullTableName()
        {
            Dictionary<string, AttributeValue> item = new Dictionary<string, AttributeValue>();
            item[HashAttrName] = new AttributeValue("a");

            InvalidRequestTest(new PutItemRequest { Item = item }, "TableName must not be null");
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void putNullItem()
        [Fact]
        public virtual void PutNullItem()
        {
            InvalidRequestTest(new PutItemRequest
            {
                TableName = TableName
            },
            "PutItem must contain an Item");
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void putMissingKey()
        [Fact]
        public virtual void PutMissingKey()
        {
            Dictionary<string, AttributeValue> item = new Dictionary<string, AttributeValue>();
            item["other-attr"] = new AttributeValue("a");

            InvalidRequestTest(new PutItemRequest
            {
                TableName = TableName,
                Item = item
            },
            "PutItem request must contain the key attribute");
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void putExpected()
        [Fact]
        public virtual void PutExpected()
        {
            var request = new PutItemRequest
            {
                TableName = BasicPutRequest.TableName,
                Item = BasicPutRequest.Item,
                Expected = NonnullExpectedAttrValues
            };
            InvalidRequestTest(request, "Requests with conditions");
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void putConditionExpression()
        [Fact]
        public virtual void PutConditionExpression()
        {
            var request = new PutItemRequest
            {
                TableName = BasicPutRequest.TableName,
                Item = BasicPutRequest.Item,
                ConditionExpression = "attribute_not_exists (some_field)"
            };
            InvalidRequestTest(request, "Requests with conditions");
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void putExpressionAttributeNames()
        [Fact]
        public virtual void PutExpressionAttributeNames()
        {
            var request = new PutItemRequest
            {
                TableName = BasicPutRequest.TableName,
                Item = BasicPutRequest.Item,
                ExpressionAttributeNames = NonnullExpAttrNames
            };
            InvalidRequestTest(request, "Requests with expressions");
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void putExpressionAttributeValues()
        [Fact]
        public virtual void PutExpressionAttributeValues()
        {
            var request = new PutItemRequest
            {
                TableName = BasicPutRequest.TableName,
                Item = BasicPutRequest.Item,
                ExpressionAttributeValues = NonnullExpAttrValues
            };
            InvalidRequestTest(request, "Requests with expressions");
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void updateExpected()
        [Fact]
        public virtual void UpdateExpected()
        {
            var request = new PutItemRequest
            {
                TableName = BasicPutRequest.TableName,
                Item = BasicPutRequest.Item,
                Expected = NonnullExpectedAttrValues
            };
            InvalidRequestTest(request, "Requests with conditions");
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void updateConditionExpression()
        [Fact]
        public virtual void UpdateConditionExpression()
        {
            var request = new PutItemRequest
            {
                TableName = BasicPutRequest.TableName,
                Item = BasicPutRequest.Item,
                ConditionExpression = "attribute_not_exists(some_field)"
            };
            InvalidRequestTest(request, "Requests with conditions");
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void updateUpdateExpression()
        [Fact]
        public virtual void UpdateUpdateExpression()
        {
            var request = new UpdateItemRequest
            {
                Key = BasicUpdateRequest.Key,
                TableName = BasicUpdateRequest.TableName,
                UpdateExpression = "REMOVE some_field"
            };
            InvalidRequestTest(request, "Requests with expressions");
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void updateExpressionAttributeNames()
        [Fact]
        public virtual void UpdateExpressionAttributeNames()
        {
            var request = new UpdateItemRequest
            {
                Key = BasicUpdateRequest.Key,
                TableName = BasicUpdateRequest.TableName,
                ExpressionAttributeNames = NonnullExpAttrNames
            };
            InvalidRequestTest(request, "Requests with expressions");
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void updateExpressionAttributeValues()
        [Fact]
        public virtual void UpdateExpressionAttributeValues()
        {
            var request = new UpdateItemRequest
            {
                Key = BasicUpdateRequest.Key,
                TableName = BasicUpdateRequest.TableName,
                ExpressionAttributeValues = NonnullExpAttrValues
            };
            InvalidRequestTest(request, "Requests with expressions");
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void deleteExpected()
        [Fact]
        public virtual void DeleteExpected()
        {
            var request = new UpdateItemRequest
            {
                Key = BasicUpdateRequest.Key,
                TableName = BasicUpdateRequest.TableName,
                Expected = NonnullExpectedAttrValues
            };
            InvalidRequestTest(request, "Requests with conditions");
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void deleteConditionExpression()
        [Fact]
        public virtual void DeleteConditionExpression()
        {
            var request = new DeleteItemRequest
            {
                Key = BasicDeleteRequest.Key,
                TableName = BasicDeleteRequest.TableName,
                ConditionExpression = "attribute_not_exists (some_field)"
            };
            InvalidRequestTest(request, "Requests with conditions");
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void deleteExpressionAttributeNames()
        [Fact]
        public virtual void DeleteExpressionAttributeNames()
        {
            var request = new DeleteItemRequest
            {
                Key = BasicDeleteRequest.Key,
                TableName = BasicDeleteRequest.TableName,
                ExpressionAttributeNames = NonnullExpAttrNames
            };
            InvalidRequestTest(request, "Requests with expressions");
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void deleteExpressionAttributeValues()
        [Fact]
        public virtual void DeleteExpressionAttributeValues()
        {
            var request = new DeleteItemRequest
            {
                Key = BasicDeleteRequest.Key,
                TableName = BasicDeleteRequest.TableName,
                ExpressionAttributeValues = NonnullExpAttrValues
            };
            InvalidRequestTest(request, "Requests with expressions");
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void validUpdate()
        [Fact]
        public virtual void ValidUpdate()
        {
            Request.UpdateItem r = new Request.UpdateItem();
            Dictionary<string, AttributeValue> item = new Dictionary<string, AttributeValue>();
            item[HashAttrName] = new AttributeValue("a");
            r.Request = new UpdateItemRequest
            {
                TableName = TableName,
                Key = item
            };
            r.ValidateAsync("1", new MockTransactionManager(this, HashSchema)).Wait();
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void validDelete()
        [Fact]
        public virtual void ValidDelete()
        {
            Request.DeleteItem r = new Request.DeleteItem();
            Dictionary<string, AttributeValue> item = new Dictionary<string, AttributeValue>();
            item[HashAttrName] = new AttributeValue("a");
            r.Request = new DeleteItemRequest
            {
                TableName = TableName,
                Key = item
            };
            r.ValidateAsync("1", new MockTransactionManager(this, HashSchema));
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void validLock()
        [Fact]
        public virtual void ValidLock()
        {
            Request.GetItem r = new Request.GetItem();
            Dictionary<string, AttributeValue> item = new Dictionary<string, AttributeValue>();
            item[HashAttrName] = new AttributeValue("a");
            r.Request = new GetItemRequest
            {
                TableName = TableName,
                Key = item
            };
            r.ValidateAsync("1", new MockTransactionManager(this, HashSchema));
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void roundTripGetString()
        [Fact]
        public virtual void RoundTripGetString()
        {
            Request.GetItem r1 = new Request.GetItem();
            Dictionary<string, AttributeValue> item = new Dictionary<string, AttributeValue>();
            item[HashAttrName] = new AttributeValue("a");
            r1.Request = new GetItemRequest
            {
                TableName = TableName,
                Key = item
            };
            byte[] r1Bytes = Request.Serialize("123", r1).ToArray();
            Request r2 = Request.Deserialize("123", new MemoryStream(r1Bytes));
            byte[] r2Bytes = Request.Serialize("123", r2).ToArray();

            AssertArrayEquals(r1Bytes, r2Bytes);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void roundTripPutAll()
        [Fact]
        public virtual void RoundTripPutAll()
        {
            Request.PutItem r1 = new Request.PutItem();
            Dictionary<string, AttributeValue> item = new Dictionary<string, AttributeValue>();
            item[HashAttrName] = new AttributeValue("a");
            item["attr_ss"] = new AttributeValue
            {
                SS = { "a", "b" }
            };
            item["attr_n"] = new AttributeValue
            {
                N = "1"
            };
            item["attr_ns"] = new AttributeValue
            {
                NS = { "1", "2" }
            };
            item["attr_b"] = new AttributeValue
            {
                B = new MemoryStream(Encoding.ASCII.GetBytes("asdf"))
            };
            item["attr_bs"] = new AttributeValue
            {
                BS =
                {
                    new MemoryStream(Encoding.ASCII.GetBytes("asdf")), new MemoryStream(Encoding.ASCII.GetBytes("asdf"))
                }
            };
            r1.Request = new PutItemRequest
            {
                TableName = TableName,
                Item = item,
                ReturnValues = "ALL_OLD"
            };
            byte[] r1Bytes = Request.Serialize("123", r1).ToArray();
            Request r2 = Request.Deserialize("123", new MemoryStream(r1Bytes));

            AssertEquals(r1.Request, ((Request.PutItem)r2).Request);
            byte[] r2Bytes = Request.Serialize("123", r2).ToArray();

            AssertArrayEquals(r1Bytes, r2Bytes);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void roundTripUpdateAll()
        [Fact]
        public virtual void RoundTripUpdateAll()
        {
            Request.UpdateItem r1 = new Request.UpdateItem();
            Dictionary<string, AttributeValue> key = new Dictionary<string, AttributeValue>();
            key[HashAttrName] = new AttributeValue("a");

            Dictionary<string, AttributeValueUpdate> updates = new Dictionary<string, AttributeValueUpdate>();
            updates["attr_ss"] = new AttributeValueUpdate
            {
                Action = "PUT",
                Value = new AttributeValue
                {
                    SS = { "a", "b", }
                }
            };
            updates["attr_n"] = new AttributeValueUpdate
            {
                Action = "PUT",
                Value = new AttributeValue
                {
                    N = "1"
                }
            };
            updates["attr_ns"] = new AttributeValueUpdate
            {
                Action = "PUT",
                Value = new AttributeValue
                {
                    NS = { "1", "2" }
                }
            };
            updates["attr_b"] = new AttributeValueUpdate
            {
                Action = "PUT",
                Value = new AttributeValue
                {
                    B = new MemoryStream(Encoding.ASCII.GetBytes("asdf"))
                }
            };
            updates["attr_bs"] = new AttributeValueUpdate
            {
                Action = "PUT",
                Value = new AttributeValue
                {
                    BS =
                    {
                        new MemoryStream(Encoding.ASCII.GetBytes("asdf")), new MemoryStream(Encoding.ASCII.GetBytes("asdf"))
                    }
                }
            };
            r1.Request = new UpdateItemRequest
            {
                TableName = TableName,
                Key = key,
                AttributeUpdates = updates
            };
            byte[] r1Bytes = Request.Serialize("123", r1).ToArray();
            Request r2 = Request.Deserialize("123", new MemoryStream(r1Bytes));
            byte[] r2Bytes = Request.Serialize("123", r2).ToArray();

            AssertArrayEquals(r1Bytes, r2Bytes);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void roundTripPutAllJSON()
        [Fact]
        public virtual void RoundTripPutAllJson()
        {
            Request.PutItem r1 = new Request.PutItem();
            Dictionary<string, AttributeValue> item = new Dictionary<string, AttributeValue>();
            item[HashAttrName] = new AttributeValue("a");
            item["json_attr"] = new AttributeValue
            {
                M = JsonMAttrVal
            };
            r1.Request = new PutItemRequest
            {
                TableName = TableName,
                Item = item,
                ReturnValues = "ALL_OLD"
            };
            byte[] r1Bytes = Request.Serialize("123", r1).ToArray();
            Request r2 = Request.Deserialize("123", new MemoryStream(r1Bytes));

            AssertEquals(r1.Request, ((Request.PutItem)r2).Request);
            byte[] r2Bytes = Request.Serialize("123", r2).ToArray();

            AssertArrayEquals(r1Bytes, r2Bytes);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void roundTripUpdateAllJSON()
        [Fact]
        public virtual void RoundTripUpdateAllJson()
        {
            Request.UpdateItem r1 = new Request.UpdateItem();
            Dictionary<string, AttributeValue> key = new Dictionary<string, AttributeValue>();
            key[HashAttrName] = new AttributeValue("a");

            Dictionary<string, AttributeValueUpdate> updates = new Dictionary<string, AttributeValueUpdate>();
            updates["attr_m"] = new AttributeValueUpdate
            {
                Action = "PUT",
                Value = new AttributeValue
                {
                    M = JsonMAttrVal,
                }
            };
            r1.Request = new UpdateItemRequest
            {
                TableName = TableName,
                Key = key,
                AttributeUpdates = updates
            };
            byte[] r1Bytes = Request.Serialize("123", r1).ToArray();
            Request r2 = Request.Deserialize("123", new MemoryStream(r1Bytes));
            byte[] r2Bytes = Request.Serialize("123", r2).ToArray();

            AssertArrayEquals(r1Bytes, r2Bytes);
        }

        private PutItemRequest BasicPutRequest
        {
            get
            {
                return new PutItemRequest
                {
                    Item = BasicItem,
                    TableName = TableName
                };
            }
        }

        private UpdateItemRequest BasicUpdateRequest
        {
            get
            {
                return new UpdateItemRequest
                {
                    Key = BasicItem,
                    TableName = TableName
                };
            }
        }

        private DeleteItemRequest BasicDeleteRequest
        {
            get
            {
                return new DeleteItemRequest
                {
                    Key = BasicItem,
                    TableName = TableName
                };
            }
        }

        private void InvalidRequestTest(PutItemRequest request, string expectedExceptionMessage)
        {
            Request.PutItem r = new Request.PutItem();
            r.Request = request;
            try
            {
                r.ValidateAsync("1", new MockTransactionManager(this, HashSchema)).Wait();
                Fail();
            }
            catch (InvalidRequestException e)
            {
                AssertTrue(e.Message.Contains(expectedExceptionMessage));
            }
        }

        private void InvalidRequestTest(UpdateItemRequest request, string expectedExceptionMessage)
        {
            Request.UpdateItem r = new Request.UpdateItem();
            r.Request = request;
            try
            {
                r.ValidateAsync("1", new MockTransactionManager(this, HashSchema)).Wait();
                Fail();
            }
            catch (InvalidRequestException e)
            {
                AssertTrue(e.Message.Contains(expectedExceptionMessage));
            }
        }

        private void InvalidRequestTest(DeleteItemRequest request, string expectedExceptionMessage)
        {
            Request.DeleteItem r = new Request.DeleteItem();
            r.Request = request;
            try
            {
                r.ValidateAsync("1", new MockTransactionManager(this, HashSchema)).Wait();
                Fail();
            }
            catch (InvalidRequestException e)
            {
                AssertTrue(e.Message.Contains(expectedExceptionMessage));
            }
        }

        protected internal class MockTransactionManager : TransactionManager
        {
            private readonly RequestTest _outerInstance;


            internal readonly List<KeySchemaElement> KeySchema;

            public MockTransactionManager(RequestTest outerInstance, List<KeySchemaElement> keySchema) 
                : base(new AmazonDynamoDBClient(new AmazonDynamoDBConfig
            {
                ServiceURL = "http://localhost:8000/"
            }), "Dummy", "DummyOther")
            {
                this._outerInstance = outerInstance;
                this.KeySchema = keySchema;
            }

            //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available inG .NET:
            //ORIGINAL LINE: @Override protected java.util.List<com.amazonaws.services.dynamodbv2.model.KeySchemaElement> GetTableSchemaAsync(String tableName) throws com.amazonaws.services.dynamodbv2.model.ResourceNotFoundException
            protected internal override Task<List<KeySchemaElement>> GetTableSchemaAsync(string tableName, CancellationToken cancellationToken)
            {
                return Task.FromResult(KeySchema);
            }
        }
    }


}