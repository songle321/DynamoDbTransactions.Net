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
        private const string TABLE_NAME = "Dummy";
        private const string HASH_ATTR_NAME = "Foo";
        private static readonly List<KeySchemaElement> HASH_SCHEMA = Arrays.asList(new KeySchemaElement
        {
            AttributeName = HASH_ATTR_NAME,
            KeyType = KeyType.HASH
        });

        internal static readonly Dictionary<string, AttributeValue> JSON_M_ATTR_VAL = new Dictionary<string, AttributeValue>();
        private static readonly Dictionary<string, ExpectedAttributeValue> NONNULL_EXPECTED_ATTR_VALUES = new Dictionary<string, ExpectedAttributeValue>();
        private static readonly Dictionary<string, string> NONNULL_EXP_ATTR_NAMES = new Dictionary<string, string>();
        private static readonly Dictionary<string, AttributeValue> NONNULL_EXP_ATTR_VALUES = new Dictionary<string, AttributeValue>();
        private static readonly Dictionary<string, AttributeValue> BASIC_ITEM = new Dictionary<string, AttributeValue>();

        static RequestTest()
        {
            JSON_M_ATTR_VAL["attr_s"] = new AttributeValue
            {
                S = "s"
            };
            JSON_M_ATTR_VAL["attr_n"] = new AttributeValue
            {
                N = "1"
            };
            JSON_M_ATTR_VAL["attr_b"] = new AttributeValue
            {
                B = new MemoryStream(Encoding.ASCII.GetBytes("asdf"))
            };
            JSON_M_ATTR_VAL["attr_ss"] = new AttributeValue
            {
                SS = { "a", "b" }
            };
            JSON_M_ATTR_VAL["attr_ns"] = new AttributeValue
            {
                NS = { "1", "2" }
            };
            JSON_M_ATTR_VAL["attr_bs"] = new AttributeValue
            {
                BS =
                {
                    new MemoryStream(Encoding.ASCII.GetBytes("asdf")),
                    new MemoryStream(Encoding.ASCII.GetBytes("ghjk"))
                }
            };
            JSON_M_ATTR_VAL["attr_bool"] = new AttributeValue
            {
                BOOL = true
            };
            JSON_M_ATTR_VAL["attr_l"] = new AttributeValue
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
            JSON_M_ATTR_VAL["attr_null"] = new AttributeValue
            {
                NULL = true
            };

            BASIC_ITEM[HASH_ATTR_NAME] = new AttributeValue("a");
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void validPut()
        [Fact]
        public virtual void validPut()
        {
            Request.PutItem r = new Request.PutItem();
            Dictionary<string, AttributeValue> item = new Dictionary<string, AttributeValue>();
            item[HASH_ATTR_NAME] = new AttributeValue("a");
            r.Request = new PutItemRequest
            {
                TableName = TABLE_NAME,
                Item = item
            };
            r.validateAsync("1", new MockTransactionManager(this, HASH_SCHEMA)).Wait();
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void putNullTableName()
        [Fact]
        public virtual void putNullTableName()
        {
            Dictionary<string, AttributeValue> item = new Dictionary<string, AttributeValue>();
            item[HASH_ATTR_NAME] = new AttributeValue("a");

            invalidRequestTest(new PutItemRequest { Item = item }, "TableName must not be null");
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void putNullItem()
        [Fact]
        public virtual void putNullItem()
        {
            invalidRequestTest(new PutItemRequest
            {
                TableName = TABLE_NAME
            },
            "PutItem must contain an Item");
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void putMissingKey()
        [Fact]
        public virtual void putMissingKey()
        {
            Dictionary<string, AttributeValue> item = new Dictionary<string, AttributeValue>();
            item["other-attr"] = new AttributeValue("a");

            invalidRequestTest(new PutItemRequest
            {
                TableName = TABLE_NAME,
                Item = item
            },
            "PutItem request must contain the key attribute");
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void putExpected()
        [Fact]
        public virtual void putExpected()
        {
            var request = new PutItemRequest
            {
                TableName = BasicPutRequest.TableName,
                Item = BasicPutRequest.Item,
                Expected = NONNULL_EXPECTED_ATTR_VALUES
            };
            invalidRequestTest(request, "Requests with conditions");
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void putConditionExpression()
        [Fact]
        public virtual void putConditionExpression()
        {
            var request = new PutItemRequest
            {
                TableName = BasicPutRequest.TableName,
                Item = BasicPutRequest.Item,
                ConditionExpression = "attribute_not_exists (some_field)"
            };
            invalidRequestTest(request, "Requests with conditions");
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void putExpressionAttributeNames()
        [Fact]
        public virtual void putExpressionAttributeNames()
        {
            var request = new PutItemRequest
            {
                TableName = BasicPutRequest.TableName,
                Item = BasicPutRequest.Item,
                ExpressionAttributeNames = NONNULL_EXP_ATTR_NAMES
            };
            invalidRequestTest(request, "Requests with expressions");
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void putExpressionAttributeValues()
        [Fact]
        public virtual void putExpressionAttributeValues()
        {
            var request = new PutItemRequest
            {
                TableName = BasicPutRequest.TableName,
                Item = BasicPutRequest.Item,
                ExpressionAttributeValues = NONNULL_EXP_ATTR_VALUES
            };
            invalidRequestTest(request, "Requests with expressions");
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void updateExpected()
        [Fact]
        public virtual void updateExpected()
        {
            var request = new PutItemRequest
            {
                TableName = BasicPutRequest.TableName,
                Item = BasicPutRequest.Item,
                Expected = NONNULL_EXPECTED_ATTR_VALUES
            };
            invalidRequestTest(request, "Requests with conditions");
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void updateConditionExpression()
        [Fact]
        public virtual void updateConditionExpression()
        {
            var request = new PutItemRequest
            {
                TableName = BasicPutRequest.TableName,
                Item = BasicPutRequest.Item,
                ConditionExpression = "attribute_not_exists(some_field)"
            };
            invalidRequestTest(request, "Requests with conditions");
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void updateUpdateExpression()
        [Fact]
        public virtual void updateUpdateExpression()
        {
            var request = new UpdateItemRequest
            {
                Key = BasicUpdateRequest.Key,
                TableName = BasicUpdateRequest.TableName,
                UpdateExpression = "REMOVE some_field"
            };
            invalidRequestTest(request, "Requests with expressions");
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void updateExpressionAttributeNames()
        [Fact]
        public virtual void updateExpressionAttributeNames()
        {
            var request = new UpdateItemRequest
            {
                Key = BasicUpdateRequest.Key,
                TableName = BasicUpdateRequest.TableName,
                ExpressionAttributeNames = NONNULL_EXP_ATTR_NAMES
            };
            invalidRequestTest(request, "Requests with expressions");
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void updateExpressionAttributeValues()
        [Fact]
        public virtual void updateExpressionAttributeValues()
        {
            var request = new UpdateItemRequest
            {
                Key = BasicUpdateRequest.Key,
                TableName = BasicUpdateRequest.TableName,
                ExpressionAttributeValues = NONNULL_EXP_ATTR_VALUES
            };
            invalidRequestTest(request, "Requests with expressions");
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void deleteExpected()
        [Fact]
        public virtual void deleteExpected()
        {
            var request = new UpdateItemRequest
            {
                Key = BasicUpdateRequest.Key,
                TableName = BasicUpdateRequest.TableName,
                Expected = NONNULL_EXPECTED_ATTR_VALUES
            };
            invalidRequestTest(request, "Requests with conditions");
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void deleteConditionExpression()
        [Fact]
        public virtual void deleteConditionExpression()
        {
            var request = new DeleteItemRequest
            {
                Key = BasicDeleteRequest.Key,
                TableName = BasicDeleteRequest.TableName,
                ConditionExpression = "attribute_not_exists (some_field)"
            };
            invalidRequestTest(request, "Requests with conditions");
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void deleteExpressionAttributeNames()
        [Fact]
        public virtual void deleteExpressionAttributeNames()
        {
            var request = new DeleteItemRequest
            {
                Key = BasicDeleteRequest.Key,
                TableName = BasicDeleteRequest.TableName,
                ExpressionAttributeNames = NONNULL_EXP_ATTR_NAMES
            };
            invalidRequestTest(request, "Requests with expressions");
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void deleteExpressionAttributeValues()
        [Fact]
        public virtual void deleteExpressionAttributeValues()
        {
            var request = new DeleteItemRequest
            {
                Key = BasicDeleteRequest.Key,
                TableName = BasicDeleteRequest.TableName,
                ExpressionAttributeValues = NONNULL_EXP_ATTR_VALUES
            };
            invalidRequestTest(request, "Requests with expressions");
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void validUpdate()
        [Fact]
        public virtual void validUpdate()
        {
            Request.UpdateItem r = new Request.UpdateItem();
            Dictionary<string, AttributeValue> item = new Dictionary<string, AttributeValue>();
            item[HASH_ATTR_NAME] = new AttributeValue("a");
            r.Request = new UpdateItemRequest
            {
                TableName = TABLE_NAME,
                Key = item
            };
            r.validateAsync("1", new MockTransactionManager(this, HASH_SCHEMA)).Wait();
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void validDelete()
        [Fact]
        public virtual void validDelete()
        {
            Request.DeleteItem r = new Request.DeleteItem();
            Dictionary<string, AttributeValue> item = new Dictionary<string, AttributeValue>();
            item[HASH_ATTR_NAME] = new AttributeValue("a");
            r.Request = new DeleteItemRequest
            {
                TableName = TABLE_NAME,
                Key = item
            };
            r.validateAsync("1", new MockTransactionManager(this, HASH_SCHEMA));
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void validLock()
        [Fact]
        public virtual void validLock()
        {
            Request.GetItem r = new Request.GetItem();
            Dictionary<string, AttributeValue> item = new Dictionary<string, AttributeValue>();
            item[HASH_ATTR_NAME] = new AttributeValue("a");
            r.Request = new GetItemRequest
            {
                TableName = TABLE_NAME,
                Key = item
            };
            r.validateAsync("1", new MockTransactionManager(this, HASH_SCHEMA));
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void roundTripGetString()
        [Fact]
        public virtual void roundTripGetString()
        {
            Request.GetItem r1 = new Request.GetItem();
            Dictionary<string, AttributeValue> item = new Dictionary<string, AttributeValue>();
            item[HASH_ATTR_NAME] = new AttributeValue("a");
            r1.Request = new GetItemRequest
            {
                TableName = TABLE_NAME,
                Key = item
            };
            byte[] r1Bytes = Request.serialize("123", r1).ToArray();
            Request r2 = Request.deserialize("123", new MemoryStream(r1Bytes));
            byte[] r2Bytes = Request.serialize("123", r2).ToArray();

            assertArrayEquals(r1Bytes, r2Bytes);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void roundTripPutAll()
        [Fact]
        public virtual void roundTripPutAll()
        {
            Request.PutItem r1 = new Request.PutItem();
            Dictionary<string, AttributeValue> item = new Dictionary<string, AttributeValue>();
            item[HASH_ATTR_NAME] = new AttributeValue("a");
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
                TableName = TABLE_NAME,
                Item = item,
                ReturnValues = "ALL_OLD"
            };
            byte[] r1Bytes = Request.serialize("123", r1).ToArray();
            Request r2 = Request.deserialize("123", new MemoryStream(r1Bytes));

            assertEquals(r1.Request, ((Request.PutItem)r2).Request);
            byte[] r2Bytes = Request.serialize("123", r2).ToArray();

            assertArrayEquals(r1Bytes, r2Bytes);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void roundTripUpdateAll()
        [Fact]
        public virtual void roundTripUpdateAll()
        {
            Request.UpdateItem r1 = new Request.UpdateItem();
            Dictionary<string, AttributeValue> key = new Dictionary<string, AttributeValue>();
            key[HASH_ATTR_NAME] = new AttributeValue("a");

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
                TableName = TABLE_NAME,
                Key = key,
                AttributeUpdates = updates
            };
            byte[] r1Bytes = Request.serialize("123", r1).ToArray();
            Request r2 = Request.deserialize("123", new MemoryStream(r1Bytes));
            byte[] r2Bytes = Request.serialize("123", r2).ToArray();

            assertArrayEquals(r1Bytes, r2Bytes);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void roundTripPutAllJSON()
        [Fact]
        public virtual void roundTripPutAllJSON()
        {
            Request.PutItem r1 = new Request.PutItem();
            Dictionary<string, AttributeValue> item = new Dictionary<string, AttributeValue>();
            item[HASH_ATTR_NAME] = new AttributeValue("a");
            item["json_attr"] = new AttributeValue
            {
                M = JSON_M_ATTR_VAL
            };
            r1.Request = new PutItemRequest
            {
                TableName = TABLE_NAME,
                Item = item,
                ReturnValues = "ALL_OLD"
            };
            byte[] r1Bytes = Request.serialize("123", r1).ToArray();
            Request r2 = Request.deserialize("123", new MemoryStream(r1Bytes));

            assertEquals(r1.Request, ((Request.PutItem)r2).Request);
            byte[] r2Bytes = Request.serialize("123", r2).ToArray();

            assertArrayEquals(r1Bytes, r2Bytes);
        }

        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void roundTripUpdateAllJSON()
        [Fact]
        public virtual void roundTripUpdateAllJSON()
        {
            Request.UpdateItem r1 = new Request.UpdateItem();
            Dictionary<string, AttributeValue> key = new Dictionary<string, AttributeValue>();
            key[HASH_ATTR_NAME] = new AttributeValue("a");

            Dictionary<string, AttributeValueUpdate> updates = new Dictionary<string, AttributeValueUpdate>();
            updates["attr_m"] = new AttributeValueUpdate
            {
                Action = "PUT",
                Value = new AttributeValue
                {
                    M = JSON_M_ATTR_VAL,
                }
            };
            r1.Request = new UpdateItemRequest
            {
                TableName = TABLE_NAME,
                Key = key,
                AttributeUpdates = updates
            };
            byte[] r1Bytes = Request.serialize("123", r1).ToArray();
            Request r2 = Request.deserialize("123", new MemoryStream(r1Bytes));
            byte[] r2Bytes = Request.serialize("123", r2).ToArray();

            assertArrayEquals(r1Bytes, r2Bytes);
        }

        private PutItemRequest BasicPutRequest
        {
            get
            {
                return new PutItemRequest
                {
                    Item = BASIC_ITEM,
                    TableName = TABLE_NAME
                };
            }
        }

        private UpdateItemRequest BasicUpdateRequest
        {
            get
            {
                return new UpdateItemRequest
                {
                    Key = BASIC_ITEM,
                    TableName = TABLE_NAME
                };
            }
        }

        private DeleteItemRequest BasicDeleteRequest
        {
            get
            {
                return new DeleteItemRequest
                {
                    Key = BASIC_ITEM,
                    TableName = TABLE_NAME
                };
            }
        }

        private void invalidRequestTest(PutItemRequest request, string expectedExceptionMessage)
        {
            Request.PutItem r = new Request.PutItem();
            r.Request = request;
            try
            {
                r.validateAsync("1", new MockTransactionManager(this, HASH_SCHEMA)).Wait();
                fail();
            }
            catch (InvalidRequestException e)
            {
                assertTrue(e.Message.Contains(expectedExceptionMessage));
            }
        }

        private void invalidRequestTest(UpdateItemRequest request, string expectedExceptionMessage)
        {
            Request.UpdateItem r = new Request.UpdateItem();
            r.Request = request;
            try
            {
                r.validateAsync("1", new MockTransactionManager(this, HASH_SCHEMA)).Wait();
                fail();
            }
            catch (InvalidRequestException e)
            {
                assertTrue(e.Message.Contains(expectedExceptionMessage));
            }
        }

        private void invalidRequestTest(DeleteItemRequest request, string expectedExceptionMessage)
        {
            Request.DeleteItem r = new Request.DeleteItem();
            r.Request = request;
            try
            {
                r.validateAsync("1", new MockTransactionManager(this, HASH_SCHEMA)).Wait();
                fail();
            }
            catch (InvalidRequestException e)
            {
                assertTrue(e.Message.Contains(expectedExceptionMessage));
            }
        }

        protected internal class MockTransactionManager : TransactionManager
        {
            private readonly RequestTest outerInstance;


            internal readonly List<KeySchemaElement> keySchema;

            public MockTransactionManager(RequestTest outerInstance, List<KeySchemaElement> keySchema) 
                : base(new AmazonDynamoDBClient(new AmazonDynamoDBConfig
            {
                ServiceURL = "http://localhost:8000/"
            }), "Dummy", "DummyOther")
            {
                this.outerInstance = outerInstance;
                this.keySchema = keySchema;
            }

            //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available inG .NET:
            //ORIGINAL LINE: @Override protected java.util.List<com.amazonaws.services.dynamodbv2.model.KeySchemaElement> GetTableSchemaAsync(String tableName) throws com.amazonaws.services.dynamodbv2.model.ResourceNotFoundException
            protected internal override Task<List<KeySchemaElement>> GetTableSchemaAsync(string tableName, CancellationToken cancellationToken)
            {
                return Task.FromResult(keySchema);
            }
        }
    }


}