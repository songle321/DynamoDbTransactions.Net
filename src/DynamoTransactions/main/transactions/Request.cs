using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using com.amazonaws.services.dynamodbv2.transactions.exceptions;
using DynamoTransactions;
using Newtonsoft.Json;

// <summary>
// Copyright 2013-2014 Amazon.com, Inc. or its affiliates. All Rights Reserved.
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
    /// <summary>
    /// Represents a write or lock request within a transaction - either a PutItem, UpdateItem, DeleteItem, or a LockItem request used for read locks
    /// </summary>
    [JsonObject]
    public abstract class Request
    {
        private const string GetItemType = "GetItem";
        private const string UpdateItemType = "UpdateItem";
        private const string DeleteItemType = "DeleteItem";
        private const string PutItemType = "PutItem";
        private static readonly ISet<string> ValidReturnValues = new HashSet<string>(new[] { "ALL_OLD", "ALL_NEW", "NONE" });

        protected int rid;

        [JsonIgnore]
        protected internal abstract string TableName { get; }

        protected internal abstract Task<Dictionary<string, AttributeValue>> GetKeyAsync(TransactionManager txManager);

        [JsonIgnore]
        protected internal abstract string ReturnValues { get; }

        protected internal abstract Task DoValidateAsync(string txId, TransactionManager txManager);

        /*
		 * Request implementations
		 * 
		 */

        public virtual int Rid
        {
            get
            {
                return rid;
            }
            set
            {
                this.rid = value;
            }
        }

        [JsonObject]
        public class GetItem : Request
        {
            [JsonProperty("@type")]
            public string Type { get; } = GetItemType;

            public virtual GetItemRequest Request { get; set; }

            protected internal override string TableName => Request.TableName;

            protected internal override string ReturnValues { get; } = null;

            protected internal override async Task<Dictionary<string, AttributeValue>> GetKeyAsync(TransactionManager txManager)
            {
                return Request.Key;
            }

            protected internal override async Task DoValidateAsync(string txId, TransactionManager txManager)
            {
                await ValidateAttributesAsync(this, Request.Key, txId, txManager);
                await ValidateAttributesAsync(this, Request.AttributesToGet, txId, txManager);
            }
        }

        [JsonObject]
        public class UpdateItem : Request
        {
            [JsonProperty("@type")]
            public string Type { get; } = UpdateItemType;

            public virtual UpdateItemRequest Request { get; set; }

            protected internal override string TableName => Request.TableName;

            protected internal override string ReturnValues => Request.ReturnValues;

            protected internal override async Task<Dictionary<string, AttributeValue>> GetKeyAsync(TransactionManager txManager)
            {
                return Request.Key;
            }

            protected internal override async Task DoValidateAsync(string txId, TransactionManager txManager)
            {
                await ValidateAttributesAsync(this, Request.Key, txId, txManager);
                if (Request.AttributeUpdates != null)
                {
                    await ValidateAttributesAsync(this, Request.AttributeUpdates, txId, txManager);
                }
                if (Request.ReturnConsumedCapacity != null)
                {
                    throw new InvalidRequestException("ReturnConsumedCapacity is not currently supported", txId, Request.TableName, null, this);
                }
                if (Request.ReturnItemCollectionMetrics != null)
                {
                    throw new InvalidRequestException("ReturnItemCollectionMetrics is not currently supported", txId, Request.TableName, null, this);
                }
                if (Request.Expected != null && Request.Expected.Any())
                {
                    throw new InvalidRequestException("Requests with conditions are not currently supported", txId, Request.TableName, await GetKeyAsync(txManager), this);
                }
                if (Request.ConditionExpression != null && Request.ConditionExpression.Any())
                {
                    throw new InvalidRequestException("Requests with conditions are not currently supported", txId, Request.TableName, await GetKeyAsync(txManager), this);
                }
                if (Request.UpdateExpression != null && Request.UpdateExpression.Any())
                {
                    throw new InvalidRequestException("Requests with expressions are not currently supported", txId, Request.TableName, await GetKeyAsync(txManager), this);
                }
                if (Request.ExpressionAttributeNames != null && Request.ExpressionAttributeNames.Any())
                {
                    throw new InvalidRequestException("Requests with expressions are not currently supported", txId, TableName, await GetKeyAsync(txManager), this);
                }
                if (Request.ExpressionAttributeValues != null && Request.ExpressionAttributeValues.Any())
                {
                    throw new InvalidRequestException("Requests with expressions are not currently supported", txId, TableName, await GetKeyAsync(txManager), this);
                }
            }
        }

        [JsonObject]
        public class DeleteItem : Request
        {
            [JsonProperty("@type")]
            public string Type { get; } = DeleteItemType;

            public virtual DeleteItemRequest Request { get; set; }

            protected internal override string TableName => Request.TableName;

            protected internal override string ReturnValues => Request.ReturnValues;

            protected internal override async Task<Dictionary<string, AttributeValue>> GetKeyAsync(TransactionManager txManager)
            {
                return Request.Key;
            }

            protected internal override async Task DoValidateAsync(string txId, TransactionManager txManager)
            {
                await ValidateAttributesAsync(this, Request.Key, txId, txManager);
                if (Request.ReturnConsumedCapacity != null)
                {
                    throw new InvalidRequestException("ReturnConsumedCapacity is not currently supported", txId, TableName, null, this);
                }
                if (Request.ReturnItemCollectionMetrics != null)
                {
                    throw new InvalidRequestException("ReturnItemCollectionMetrics is not currently supported", txId, TableName, null, this);
                }
                if (Request.Expected != null && Request.Expected.Any())
                {
                    throw new InvalidRequestException("Requests with conditions are not currently supported", txId, TableName, await GetKeyAsync(txManager), this);
                }
                if (Request.ConditionExpression != null && Request.ConditionExpression.Any())
                {
                    throw new InvalidRequestException("Requests with conditions are not currently supported", txId, TableName, await GetKeyAsync(txManager), this);
                }
                if (Request.ExpressionAttributeNames != null && Request.ExpressionAttributeNames.Any())
                {
                    throw new InvalidRequestException("Requests with expressions are not currently supported", txId, TableName, await GetKeyAsync(txManager), this);
                }
                if (Request.ExpressionAttributeValues != null && Request.ExpressionAttributeValues.Any())
                {
                    throw new InvalidRequestException("Requests with expressions are not currently supported", txId, TableName, await GetKeyAsync(txManager), this);
                }
            }
        }

        [JsonObject]
        public class PutItem : Request
        {
            [JsonProperty("@type")]
            public string Type { get; } = PutItemType;

            [JsonIgnore]
            internal Dictionary<string, AttributeValue> Key = null;

            public virtual PutItemRequest Request { get; set; }


            protected internal override string TableName => Request.TableName;

            protected internal override string ReturnValues => Request.ReturnValues;

            protected internal override async Task<Dictionary<string, AttributeValue>> GetKeyAsync(TransactionManager txManager)
            {
                if (Key == null)
                {
                    Key = await GetKeyFromItemAsync(TableName, Request.Item, txManager);
                }
                return Key;
            }

            protected internal override async Task DoValidateAsync(string txId, TransactionManager txManager)
            {
                if (Request == null || Request.Item == null || !Request.Item.Any())
                {
                    throw new InvalidRequestException("PutItem must contain an Item", txId, TableName, null, this);
                }
                await ValidateAttributesAsync(this, Request.Item, txId, txManager);
                if (Request.ReturnConsumedCapacity != null)
                {
                    throw new InvalidRequestException("ReturnConsumedCapacity is not currently supported", txId, TableName, null, this);
                }
                if (Request.ReturnItemCollectionMetrics != null)
                {
                    throw new InvalidRequestException("ReturnItemCollectionMetrics is not currently supported", txId, TableName, null, this);
                }
                if (Request.Expected != null && Request.Expected.Any())
                {
                    throw new InvalidRequestException("Requests with conditions are not currently supported", txId, TableName, await GetKeyAsync(txManager), this);
                }
                if (Request.ConditionExpression != null && Request.ConditionExpression.Any())
                {
                    throw new InvalidRequestException("Requests with conditions are not currently supported", txId, TableName, await GetKeyAsync(txManager), this);
                }
                if (Request.ExpressionAttributeNames != null && Request.ExpressionAttributeNames.Any())
                {
                    throw new InvalidRequestException("Requests with expressions are not currently supported", txId, TableName, await GetKeyAsync(txManager), this);
                }
                if (Request.ExpressionAttributeValues != null && Request.ExpressionAttributeValues.Any())
                {
                    throw new InvalidRequestException("Requests with expressions are not currently supported", txId, TableName, await GetKeyAsync(txManager), this);
                }
            }
        }

        [JsonObject]
        public class TypeMetadata
        {
            [JsonProperty("@type")]
            public string Type { get; set; }
        }

        /*
		 * Validation helpers
		 */

        public virtual async Task ValidateAsync(string txId, TransactionManager txManager)
        {
            if (string.ReferenceEquals(TableName, null))
            {
                throw new InvalidRequestException("TableName must not be null", txId, null, null, this);
            }
            Dictionary<string, AttributeValue> key = await GetKeyAsync(txManager);
            if (key == null || key.Count == 0)
            {
                throw new InvalidRequestException("The request key cannot be empty", txId, TableName, key, this);
            }

            ValidateReturnValues(ReturnValues, txId, this);

            await DoValidateAsync(txId, txManager);
        }

        private static void ValidateReturnValues(string returnValues, string txId, Request request)
        {
            if (string.ReferenceEquals(returnValues, null) || ValidReturnValues.Contains(returnValues))
            {
                return;
            }

            throw new InvalidRequestException("Unsupported ReturnValues: " + returnValues, txId, request.TableName, null, request);
        }

        private static async Task ValidateAttributesAsync<T>(Request request, Dictionary<string, T> attributes, string txId, TransactionManager txManager)
        {
            //JAVA TO C# CONVERTER WARNING: Java wildcard generics have no direct equivalent in .NET:
            //ORIGINAL LINE: for(java.util.Map.Entry<String, ?> entry : attributes.entrySet())
            foreach (KeyValuePair<string, T> entry in attributes.SetOfKeyValuePairs())
            {
                if (entry.Key.StartsWith("_Tx"))
                {
                    throw new InvalidRequestException("Request must not contain the reserved attribute " + entry.Key, txId, request.TableName, await request.GetKeyAsync(txManager), request);
                }
            }
        }

        private static async Task ValidateAttributesAsync(Request request, List<string> attributes, string txId, TransactionManager txManager)
        {
            if (attributes == null)
            {
                return;
            }
            foreach (string attr in attributes)
            {
                if (attr.StartsWith("_Tx", StringComparison.Ordinal))
                {
                    throw new InvalidRequestException("Request must not contain the reserved attribute " + attr, txId, request.TableName, await request.GetKeyAsync(txManager), request);
                }
            }
        }

        protected internal static async Task<Dictionary<string, AttributeValue>> GetKeyFromItemAsync(string tableName, Dictionary<string, AttributeValue> item, TransactionManager txManager)
        {
            if (item == null || !item.Any())
            {
                throw new InvalidRequestException("PutItem must contain an Item", null, tableName, null, null);
            }
            Dictionary<string, AttributeValue> newKey = new Dictionary<string, AttributeValue>();
            List<KeySchemaElement> schema = await txManager.GetTableSchemaAsync(tableName);
            foreach (KeySchemaElement schemaElement in schema)
            {
                AttributeValue val;
                if (!item.TryGetValue(schemaElement.AttributeName, out val))
                {
                    throw new InvalidRequestException("PutItem request must contain the key attribute " + schemaElement.AttributeName, null, tableName, null, null);
                }
                newKey[schemaElement.AttributeName] = item[schemaElement.AttributeName];
            }
            return newKey;
        }

        /// <summary>
        /// Returns a new copy of Map that can be used in a write on the item to ensure it does not exist </summary>
        /// <param name="txManager"> </param>
        /// <returns> a RequestTypeMap for use in an expected clause to ensure the item does not exist </returns>
        protected internal virtual async Task<Dictionary<string, ExpectedAttributeValue>> GetExpectNotExistsAsync(TransactionManager txManager)
        {
            Dictionary<string, AttributeValue> key = await GetKeyAsync(txManager);
            Dictionary<string, ExpectedAttributeValue> expected = new Dictionary<string, ExpectedAttributeValue>(key.Count);
            foreach (KeyValuePair<string, AttributeValue> entry in key.SetOfKeyValuePairs())
            {
                expected[entry.Key] = new ExpectedAttributeValue { Exists = false };
            }
            return expected;
        }

        /// <summary>
        /// Returns a new copy of Map that can be used in a write on the item to ensure it exists </summary>
        /// <param name="txManager"> </param>
        /// <returns> a RequestTypeMap for use in an expected clause to ensure the item exists </returns>
        protected internal virtual async Task<Dictionary<string, ExpectedAttributeValue>> GetExpectExists(TransactionManager txManager)
        {
            Dictionary<string, AttributeValue> key = await GetKeyAsync(txManager);
            Dictionary<string, ExpectedAttributeValue> expected = new Dictionary<string, ExpectedAttributeValue>(key.Count);
            foreach (KeyValuePair<string, AttributeValue> entry in key.SetOfKeyValuePairs())
            {
                expected[entry.Key] = new ExpectedAttributeValue { Exists = false };
            }
            return expected;
        }

        /*
		 * Serialization configuration related code
		 */

        static Request()
        {
            //MAPPER = new ObjectMapper();
            //MAPPER.disable(SerializationFeature.INDENT_OUTPUT);
            //MAPPER.SerializationInclusion = Include.NON_NULL;
            //MAPPER.addMixInAnnotations(typeof(GetItemRequest), typeof(RequestMixIn));
            //MAPPER.addMixInAnnotations(typeof(PutItemRequest), typeof(RequestMixIn));
            //MAPPER.addMixInAnnotations(typeof(UpdateItemRequest), typeof(RequestMixIn));
            //MAPPER.addMixInAnnotations(typeof(DeleteItemRequest), typeof(RequestMixIn));
            //MAPPER.addMixInAnnotations(typeof(AttributeValueUpdate), typeof(AttributeValueUpdateMixIn));
            //MAPPER.addMixInAnnotations(typeof(ExpectedAttributeValue), typeof(ExpectedAttributeValueMixIn));

            //// Deal with serializing of byte[].
            //SimpleModule module = new SimpleModule("custom", Version.unknownVersion());
            //module.addSerializer(typeof(ByteBuffer), new ByteBufferSerializer());
            //module.addDeserializer(typeof(ByteBuffer), new ByteBufferDeserializer());
            //MAPPER.registerModule(module);
        }

        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            DefaultValueHandling = DefaultValueHandling.Ignore,
            Converters = { new AttributeValueJsonConverter(), new MemoryStreamJsonConverter() }
        };

        private static readonly Dictionary<string, Type> RequestTypeMap = new Dictionary<string, Type>
        {
            {GetItemType, typeof(GetItem)},
            {UpdateItemType, typeof(UpdateItem)},
            {DeleteItemType, typeof(DeleteItem)},
            {PutItemType, typeof(PutItem)},
        };

        protected internal static MemoryStream Serialize(string txId, object request)
        {
            string json = JsonConvert.SerializeObject(request, JsonSettings);
            byte[] requestBytes = Encoding.UTF8.GetBytes(json);
            return new MemoryStream(requestBytes);
        }

        protected internal static Request Deserialize(string txId, MemoryStream rawRequest)
        {
            byte[] requestBytes = rawRequest.ToArray();
            string json = Encoding.UTF8.GetString(requestBytes);

            var typeMetadata = JsonConvert.DeserializeObject<TypeMetadata>(json, JsonSettings);
            return (Request)JsonConvert.DeserializeObject(json, RequestTypeMap[typeMetadata.Type], JsonSettings);
        }

        //private class ByteBufferSerializer : JsonSerializer<ByteBuffer>
        //{
        //    //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //    //ORIGINAL LINE: @Override public void serialize(ByteBuffer value, com.fasterxml.jackson.core.JsonGenerator jgen, com.fasterxml.jackson.databind.SerializerProvider provider) throws java.io.IOException, com.fasterxml.jackson.core.JsonProcessingException
        //    public override void serialize(ByteBuffer value, JsonGenerator jgen, SerializerProvider provider)
        //    {
        //        // value is never null, according to JsonSerializer contract
        //        jgen.writeBinary(value.array());
        //    }

        //}

        //private class ByteBufferDeserializer : JsonDeserializer<ByteBuffer>
        //{
        //    //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //    //ORIGINAL LINE: @Override public ByteBuffer deserialize(com.fasterxml.jackson.core.JsonParser jp, com.fasterxml.jackson.databind.DeserializationContext ctxt) throws java.io.IOException, com.fasterxml.jackson.core.JsonProcessingException
        //    public override ByteBuffer deserialize(JsonParser jp, DeserializationContext ctxt)
        //    {
        //        // never called for null literal, according to JsonDeserializer contract
        //        return ByteBuffer.wrap(jp.BinaryValue);
        //    }

        //}
    }
}