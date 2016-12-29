using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using com.amazonaws.services.dynamodbv2.transactions.exceptions;
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
private static readonly ISet<string> VALID_RETURN_VALUES = new HashSet<string>(new[] { "ALL_OLD", "ALL_NEW", "NONE" });

        protected int rid;

        [JsonIgnore]
        protected internal abstract string TableName { get; }

        protected internal abstract Task<Dictionary<string, AttributeValue>> getKeyAsync(TransactionManager txManager);

        [JsonIgnore]
        protected internal abstract string ReturnValues { get; }

        protected internal abstract Task doValidateAsync(string txId, TransactionManager txManager);

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
internal GetItemRequest request;

            public virtual GetItemRequest Request
            {
                get
                {
                    return request;
                }
                set
                {
                    this.request = value;
                }
            }


            protected internal override string TableName
            {
                get
                {
                    return request.TableName;
                }
            }

            protected internal override string ReturnValues
            {
                get
                {
                    return null;
                }
            }

            protected internal override async Task<Dictionary<string, AttributeValue>> getKeyAsync(TransactionManager txManager)
            {
                return request.Key;
            }

            protected internal override async Task doValidateAsync(string txId, TransactionManager txManager)
            {
                await validateAttributesAsync(this, request.Key, txId, txManager);
                await validateAttributesAsync(this, request.AttributesToGet, txId, txManager);
            }
        }

        [JsonObject]
        public class UpdateItem : Request
        {
internal UpdateItemRequest request;

            public virtual UpdateItemRequest Request
            {
                get
                {
                    return request;
                }
                set
                {
                    this.request = value;
                }
            }


            protected internal override string TableName
            {
                get
                {
                    return request.TableName;
                }
            }

            protected internal override string ReturnValues
            {
                get
                {
                    return request.ReturnValues;
                }
            }

            protected internal override async Task<Dictionary<string, AttributeValue>> getKeyAsync(TransactionManager txManager)
            {
                return request.Key;
            }

            protected internal override async Task doValidateAsync(string txId, TransactionManager txManager)
            {
                validateAttributesAsync(this, request.Key, txId, txManager);
                if (request.AttributeUpdates != null)
                {
                    validateAttributesAsync(this, request.AttributeUpdates, txId, txManager);
                }
                if (request.ReturnConsumedCapacity != null)
                {
                    throw new InvalidRequestException("ReturnConsumedCapacity is not currently supported", txId, request.TableName, null, this);
                }
                if (request.ReturnItemCollectionMetrics != null)
                {
                    throw new InvalidRequestException("ReturnItemCollectionMetrics is not currently supported", txId, request.TableName, null, this);
                }
                if (request.Expected != null)
                {
                    throw new InvalidRequestException("Requests with conditions are not currently supported", txId, request.TableName, await getKeyAsync(txManager), this);
                }
                if (request.ConditionExpression != null)
                {
                    throw new InvalidRequestException("Requests with conditions are not currently supported", txId, request.TableName, await getKeyAsync(txManager), this);
                }
                if (request.UpdateExpression != null)
                {
                    throw new InvalidRequestException("Requests with expressions are not currently supported", txId, request.TableName, await getKeyAsync(txManager), this);
                }
                if (request.ExpressionAttributeNames != null)
                {
                    throw new InvalidRequestException("Requests with expressions are not currently supported", txId, TableName, await getKeyAsync(txManager), this);
                }
                if (request.ExpressionAttributeValues != null)
                {
                    throw new InvalidRequestException("Requests with expressions are not currently supported", txId, TableName, await getKeyAsync(txManager), this);
                }
            }
        }

        [JsonObject]
        public class DeleteItem : Request
        {
internal DeleteItemRequest request;

            public virtual DeleteItemRequest Request
            {
                get
                {
                    return request;
                }
                set
                {
                    this.request = value;
                }
            }


            protected internal override string TableName
            {
                get
                {
                    return request.TableName;
                }
            }

            protected internal override string ReturnValues
            {
                get
                {
                    return request.ReturnValues;
                }
            }

            protected internal override async Task<Dictionary<string, AttributeValue>> getKeyAsync(TransactionManager txManager)
            {
                return request.Key;
            }

            protected internal override async Task doValidateAsync(string txId, TransactionManager txManager)
            {
                validateAttributesAsync(this, request.Key, txId, txManager);
                if (request.ReturnConsumedCapacity != null)
                {
                    throw new InvalidRequestException("ReturnConsumedCapacity is not currently supported", txId, TableName, null, this);
                }
                if (request.ReturnItemCollectionMetrics != null)
                {
                    throw new InvalidRequestException("ReturnItemCollectionMetrics is not currently supported", txId, TableName, null, this);
                }
                if (request.Expected != null)
                {
                    throw new InvalidRequestException("Requests with conditions are not currently supported", txId, TableName, await getKeyAsync(txManager), this);
                }
                if (request.ConditionExpression != null)
                {
                    throw new InvalidRequestException("Requests with conditions are not currently supported", txId, TableName, await getKeyAsync(txManager), this);
                }
                if (request.ExpressionAttributeNames != null)
                {
                    throw new InvalidRequestException("Requests with expressions are not currently supported", txId, TableName, await getKeyAsync(txManager), this);
                }
                if (request.ExpressionAttributeValues != null)
                {
                    throw new InvalidRequestException("Requests with expressions are not currently supported", txId, TableName, await getKeyAsync(txManager), this);
                }
            }
        }

        [JsonObject]
        public class PutItem : Request
        {
            [JsonIgnore]
            internal Dictionary<string, AttributeValue> key = null;

            internal PutItemRequest request;

            public virtual PutItemRequest Request
            {
                get
                {
                    return request;
                }
                set
                {
                    this.request = value;
                }
            }


            protected internal override string TableName
            {
                get
                {
                    return request.TableName;
                }
            }

            protected internal override string ReturnValues
            {
                get
                {
                    return request.ReturnValues;
                }
            }

            protected internal override async Task<Dictionary<string, AttributeValue>> getKeyAsync(TransactionManager txManager)
            {
                if (key == null)
                {
                    key = await getKeyFromItemAsync(TableName, request.Item, txManager);
                }
                return key;
            }

            protected internal override async Task doValidateAsync(string txId, TransactionManager txManager)
            {
                if (request == null || request.Item == null)
                {
                    throw new InvalidRequestException("PutItem must contain an Item", txId, TableName, null, this);
                }
                validateAttributesAsync(this, request.Item, txId, txManager);
                if (request.ReturnConsumedCapacity != null)
                {
                    throw new InvalidRequestException("ReturnConsumedCapacity is not currently supported", txId, TableName, null, this);
                }
                if (request.ReturnItemCollectionMetrics != null)
                {
                    throw new InvalidRequestException("ReturnItemCollectionMetrics is not currently supported", txId, TableName, null, this);
                }
                if (request.Expected != null)
                {
                    throw new InvalidRequestException("Requests with conditions are not currently supported", txId, TableName, await getKeyAsync(txManager), this);
                }
                if (request.ConditionExpression != null)
                {
                    throw new InvalidRequestException("Requests with conditions are not currently supported", txId, TableName, await getKeyAsync(txManager), this);
                }
                if (request.ExpressionAttributeNames != null)
                {
                    throw new InvalidRequestException("Requests with expressions are not currently supported", txId, TableName, await getKeyAsync(txManager), this);
                }
                if (request.ExpressionAttributeValues != null)
                {
                    throw new InvalidRequestException("Requests with expressions are not currently supported", txId, TableName, await getKeyAsync(txManager), this);
                }
            }
        }

        /*
		 * Validation helpers
		 */
        
        public virtual async Task validateAsync(string txId, TransactionManager txManager)
        {
            if (string.ReferenceEquals(TableName, null))
            {
                throw new InvalidRequestException("TableName must not be null", txId, null, null, this);
            }
            Dictionary<string, AttributeValue> key = await getKeyAsync(txManager);
            if (key == null || key.Count == 0)
            {
                throw new InvalidRequestException("The request key cannot be empty", txId, TableName, key, this);
            }

            validateReturnValues(ReturnValues, txId, this);

            doValidateAsync(txId, txManager);
        }

        private static void validateReturnValues(string returnValues, string txId, Request request)
        {
            if (string.ReferenceEquals(returnValues, null) || VALID_RETURN_VALUES.Contains(returnValues))
            {
                return;
            }

            throw new InvalidRequestException("Unsupported ReturnValues: " + returnValues, txId, request.TableName, null, request);
        }

        private static async Task validateAttributesAsync<T>(Request request, Dictionary<string, T> attributes, string txId, TransactionManager txManager)
        {
            //JAVA TO C# CONVERTER WARNING: Java wildcard generics have no direct equivalent in .NET:
            //ORIGINAL LINE: for(java.util.Map.Entry<String, ?> entry : attributes.entrySet())
            foreach (KeyValuePair<string, T> entry in attributes.SetOfKeyValuePairs())
            {
                if (entry.Key.StartsWith("_Tx"))
                {
                    throw new InvalidRequestException("Request must not contain the reserved attribute " + entry.Key, txId, request.TableName, await request.getKeyAsync(txManager), request);
                }
            }
        }

        private static async Task validateAttributesAsync(Request request, List<string> attributes, string txId, TransactionManager txManager)
        {
            if (attributes == null)
            {
                return;
            }
            foreach (string attr in attributes)
            {
                if (attr.StartsWith("_Tx", StringComparison.Ordinal))
                {
                    throw new InvalidRequestException("Request must not contain the reserved attribute " + attr, txId, request.TableName, await request.getKeyAsync(txManager), request);
                }
            }
        }

        protected internal static async Task<Dictionary<string, AttributeValue>> getKeyFromItemAsync(string tableName, Dictionary<string, AttributeValue> item, TransactionManager txManager)
        {
            if (item == null)
            {
                throw new InvalidRequestException("PutItem must contain an Item", null, tableName, null, null);
            }
            Dictionary<string, AttributeValue> newKey = new Dictionary<string, AttributeValue>();
            List<KeySchemaElement> schema = await txManager.GetTableSchemaAsync(tableName);
            foreach (KeySchemaElement schemaElement in schema)
            {
                AttributeValue val = item[schemaElement.AttributeName];
                if (val == null)
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
        /// <returns> a map for use in an expected clause to ensure the item does not exist </returns>
        protected internal virtual async Task<Dictionary<string, ExpectedAttributeValue>> getExpectNotExistsAsync(TransactionManager txManager)
        {
            Dictionary<string, AttributeValue> key = await getKeyAsync(txManager);
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
        /// <returns> a map for use in an expected clause to ensure the item exists </returns>
        protected internal virtual async Task<Dictionary<string, ExpectedAttributeValue>> getExpectExists(TransactionManager txManager)
        {
            Dictionary<string, AttributeValue> key = await getKeyAsync(txManager);
            Dictionary<string, ExpectedAttributeValue> expected = new Dictionary<string, ExpectedAttributeValue>(key.Count);
            foreach (KeyValuePair<string, AttributeValue> entry in key.SetOfKeyValuePairs())
            {
                expected[entry.Key] = new ExpectedAttributeValue {Exists = false};
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

        protected internal static MemoryStream serialize(string txId, object request)
        {
            byte[] requestBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(request));
            return new MemoryStream(requestBytes);
        }

        protected internal static Request deserialize(string txId, MemoryStream rawRequest)
        {
            byte[] requestBytes = rawRequest.ToArray();
            string json = Encoding.UTF8.GetString(requestBytes);
            return JsonConvert.DeserializeObject<Request>(json);
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