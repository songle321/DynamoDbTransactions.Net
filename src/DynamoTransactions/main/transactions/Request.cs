using System;
using System.Collections.Generic;
using System.IO;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using com.amazonaws.services.dynamodbv2.transactions.exceptions;

/// <summary>
/// Copyright 2013-2014 Amazon.com, Inc. or its affiliates. All Rights Reserved.
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
	/// <summary>
	/// Represents a write or lock request within a transaction - either a PutItem, UpdateItem, DeleteItem, or a LockItem request used for read locks
	/// </summary>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @JsonTypeInfo(use = JsonTypeInfo.Id.NAME, include = JsonTypeInfo.As.PROPERTY, property = "type") @JsonSubTypes({ @Type(value = Request.PutItem.class, name = "PutItem"), @Type(value = Request.UpdateItem.class, name = "UpdateItem"), @Type(value = Request.DeleteItem.class, name = "DeleteItem"), @Type(value = Request.GetItem.class, name = "GetItem") }) public abstract class Request
	public abstract class Request
	{

		private static readonly ISet<string> VALID_RETURN_VALUES = Collections.unmodifiableSet(new HashSet<string>(Arrays.asList("ALL_OLD", "ALL_NEW", "NONE")));

		private int? rid;

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @JsonIgnore protected abstract String getTableName();
		protected internal abstract string TableName {get;}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @JsonIgnore protected abstract java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> getKey(TransactionManager txManager);
		protected internal abstract Dictionary<string, AttributeValue> getKey(TransactionManager txManager);

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @JsonIgnore protected abstract String getReturnValues();
		protected internal abstract string ReturnValues {get;set;}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @JsonIgnore protected abstract void doValidate(String txId, TransactionManager txManager);
		protected internal abstract void doValidate(string txId, TransactionManager txManager);

		/*
		 * Request implementations
		 * 
		 */

		public virtual int? Rid
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


//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @JsonTypeName(value="GetItem") public static class GetItem extends Request
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

			protected internal override IDictionary<string, AttributeValue> getKey(TransactionManager txManager)
			{
				return request.Key;
			}

			protected internal override void doValidate(string txId, TransactionManager txManager)
			{
				validateAttributes(this, request.Key, txId, txManager);
				validateAttributes(this, request.AttributesToGet, txId, txManager);
			}
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @JsonTypeName(value="UpdateItem") public static class UpdateItem extends Request
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

			protected internal override IDictionary<string, AttributeValue> getKey(TransactionManager txManager)
			{
				return request.Key;
			}

			protected internal override void doValidate(string txId, TransactionManager txManager)
			{
				validateAttributes(this, request.Key, txId, txManager);
				if (request.AttributeUpdates != null)
				{
					validateAttributes(this, request.AttributeUpdates, txId, txManager);
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
					throw new InvalidRequestException("Requests with conditions are not currently supported", txId, request.TableName, getKey(txManager), this);
				}
				if (request.ConditionExpression != null)
				{
					throw new InvalidRequestException("Requests with conditions are not currently supported", txId, request.TableName, getKey(txManager), this);
				}
				if (request.UpdateExpression != null)
				{
					throw new InvalidRequestException("Requests with expressions are not currently supported", txId, request.TableName, getKey(txManager), this);
				}
				if (request.ExpressionAttributeNames != null)
				{
					throw new InvalidRequestException("Requests with expressions are not currently supported", txId, TableName, getKey(txManager), this);
				}
				if (request.ExpressionAttributeValues != null)
				{
					throw new InvalidRequestException("Requests with expressions are not currently supported", txId, TableName, getKey(txManager), this);
				}
			}
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @JsonTypeName(value="DeleteItem") public static class DeleteItem extends Request
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

			protected internal override IDictionary<string, AttributeValue> getKey(TransactionManager txManager)
			{
				return request.Key;
			}

			protected internal override void doValidate(string txId, TransactionManager txManager)
			{
				validateAttributes(this, request.Key, txId, txManager);
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
					throw new InvalidRequestException("Requests with conditions are not currently supported", txId, TableName, getKey(txManager), this);
				}
				if (request.ConditionExpression != null)
				{
					throw new InvalidRequestException("Requests with conditions are not currently supported", txId, TableName, getKey(txManager), this);
				}
				if (request.ExpressionAttributeNames != null)
				{
					throw new InvalidRequestException("Requests with expressions are not currently supported", txId, TableName, getKey(txManager), this);
				}
				if (request.ExpressionAttributeValues != null)
				{
					throw new InvalidRequestException("Requests with expressions are not currently supported", txId, TableName, getKey(txManager), this);
				}
			}
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @JsonTypeName(value="PutItem") public static class PutItem extends Request
		public class PutItem : Request
		{
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @JsonIgnore private java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> key = null;
			internal IDictionary<string, AttributeValue> key = null;

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

			protected internal override IDictionary<string, AttributeValue> getKey(TransactionManager txManager)
			{
				if (key == null)
				{
					 key = getKeyFromItem(TableName, request.Item, txManager);
				}
				return key;
			}

			protected internal override void doValidate(string txId, TransactionManager txManager)
			{
				if (request == null || request.Item == null)
				{
					throw new InvalidRequestException("PutItem must contain an Item", txId, TableName, null, this);
				}
				validateAttributes(this, request.Item, txId, txManager);
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
					throw new InvalidRequestException("Requests with conditions are not currently supported", txId, TableName, getKey(txManager), this);
				}
				if (request.ConditionExpression != null)
				{
					throw new InvalidRequestException("Requests with conditions are not currently supported", txId, TableName, getKey(txManager), this);
				}
				if (request.ExpressionAttributeNames != null)
				{
					throw new InvalidRequestException("Requests with expressions are not currently supported", txId, TableName, getKey(txManager), this);
				}
				if (request.ExpressionAttributeValues != null)
				{
					throw new InvalidRequestException("Requests with expressions are not currently supported", txId, TableName, getKey(txManager), this);
				}
			}
		}

		/*
		 * Validation helpers
		 */

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @JsonIgnore public void validate(String txId, TransactionManager txManager)
		public virtual void validate(string txId, TransactionManager txManager)
		{
			if (string.ReferenceEquals(TableName, null))
			{
				throw new InvalidRequestException("TableName must not be null", txId, null, null, this);
			}
			IDictionary<string, AttributeValue> key = getKey(txManager);
			if (key == null || key.Count == 0)
			{
				throw new InvalidRequestException("The request key cannot be empty", txId, TableName, key, this);
			}

			validateReturnValues(ReturnValues, txId, this);

			doValidate(txId, txManager);
		}

		private static void validateReturnValues(string returnValues, string txId, Request request)
		{
			if (string.ReferenceEquals(returnValues, null) || VALID_RETURN_VALUES.Contains(returnValues))
			{
				return;
			}

			throw new InvalidRequestException("Unsupported ReturnValues: " + returnValues, txId, request.TableName, null, request);
		}

		private static void validateAttributes<T1>(Request request, IDictionary<T1> attributes, string txId, TransactionManager txManager)
		{
//JAVA TO C# CONVERTER WARNING: Java wildcard generics have no direct equivalent in .NET:
//ORIGINAL LINE: for(java.util.Map.Entry<String, ?> entry : attributes.entrySet())
			foreach (KeyValuePair<string, ?> entry in attributes.SetOfKeyValuePairs())
			{
				if (entry.Key.StartsWith("_Tx"))
				{
					throw new InvalidRequestException("Request must not contain the reserved attribute " + entry.Key, txId, request.TableName, request.getKey(txManager), request);
				}
			}
		}

		private static void validateAttributes(Request request, IList<string> attributes, string txId, TransactionManager txManager)
		{
			if (attributes == null)
			{
				return;
			}
			foreach (string attr in attributes)
			{
				if (attr.StartsWith("_Tx", StringComparison.Ordinal))
				{
					throw new InvalidRequestException("Request must not contain the reserved attribute " + attr, txId, request.TableName, request.getKey(txManager), request);
				}
			}
		}

		protected internal static Dictionary<string, AttributeValue> getKeyFromItem(string tableName, IDictionary<string, AttributeValue> item, TransactionManager txManager)
		{
			if (item == null)
			{
				throw new InvalidRequestException("PutItem must contain an Item", null, tableName, null, null);
			}
			IDictionary<string, AttributeValue> newKey = new Dictionary<string, AttributeValue>();
			IList<KeySchemaElement> schema = txManager.getTableSchema(tableName);
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
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @JsonIgnore protected java.util.Map<String, com.amazonaws.services.dynamodbv2.model.ExpectedAttributeValue> getExpectNotExists(TransactionManager txManager)
		protected internal virtual IDictionary<string, ExpectedAttributeValue> getExpectNotExists(TransactionManager txManager)
		{
			IDictionary<string, AttributeValue> key = getKey(txManager);
			IDictionary<string, ExpectedAttributeValue> expected = new Dictionary<string, ExpectedAttributeValue>(key.Count);
			foreach (KeyValuePair<string, AttributeValue> entry in key.SetOfKeyValuePairs())
			{
				expected[entry.Key] = (new ExpectedAttributeValue()).withExists(false);
			}
			return expected;
		}

		/// <summary>
		/// Returns a new copy of Map that can be used in a write on the item to ensure it exists </summary>
		/// <param name="txManager"> </param>
		/// <returns> a map for use in an expected clause to ensure the item exists </returns>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @JsonIgnore protected java.util.Map<String, com.amazonaws.services.dynamodbv2.model.ExpectedAttributeValue> getExpectExists(TransactionManager txManager)
		protected internal virtual Dictionary<string, ExpectedAttributeValue> getExpectExists(TransactionManager txManager)
		{
			IDictionary<string, AttributeValue> key = getKey(txManager);
			IDictionary<string, ExpectedAttributeValue> expected = new Dictionary<string, ExpectedAttributeValue>(key.Count);
			foreach (KeyValuePair<string, AttributeValue> entry in key.SetOfKeyValuePairs())
			{
				expected[entry.Key] = (new ExpectedAttributeValue()).withValue(entry.Value);
			}
			return expected;
		}

		/*
		 * Serialization configuration related code
		 */

		private static readonly ObjectMapper MAPPER;

		static Request()
		{
			MAPPER = new ObjectMapper();
			MAPPER.disable(SerializationFeature.INDENT_OUTPUT);
			MAPPER.SerializationInclusion = Include.NON_NULL;
			MAPPER.addMixInAnnotations(typeof(GetItemRequest), typeof(RequestMixIn));
			MAPPER.addMixInAnnotations(typeof(PutItemRequest), typeof(RequestMixIn));
			MAPPER.addMixInAnnotations(typeof(UpdateItemRequest), typeof(RequestMixIn));
			MAPPER.addMixInAnnotations(typeof(DeleteItemRequest), typeof(RequestMixIn));
			MAPPER.addMixInAnnotations(typeof(AttributeValueUpdate), typeof(AttributeValueUpdateMixIn));
			MAPPER.addMixInAnnotations(typeof(ExpectedAttributeValue), typeof(ExpectedAttributeValueMixIn));

			// Deal with serializing of byte[].
			SimpleModule module = new SimpleModule("custom", Version.unknownVersion());
			module.addSerializer(typeof(ByteBuffer), new ByteBufferSerializer());
			module.addDeserializer(typeof(ByteBuffer), new ByteBufferDeserializer());
			MAPPER.registerModule(module);
		};

		protected internal static MemoryStream serialize(string txId, object request)
		{
			try
			{
				byte[] requestBytes = MAPPER.writeValueAsBytes(request);
				return new MemoryStream(requestBytes);
			}
			catch (JsonGenerationException e)
			{
				throw new TransactionAssertionException(txId, "Failed to serialize request " + request + " " + e);
			}
			catch (JsonMappingException e)
			{
				throw new TransactionAssertionException(txId, "Failed to serialize request " + request + " " + e);
			}
			catch (IOException e)
			{
				throw new TransactionAssertionException(txId, "Failed to serialize request " + request + " " + e);
			}
		}



		protected internal static Request deserialize(string txId, MemoryStream rawRequest)
		{
			sbyte[] requestBytes = rawRequest.array();
			try
			{
				return MAPPER.readValue(requestBytes, 0, requestBytes.Length, typeof(Request));
			}
			catch (JsonParseException e)
			{
				throw new TransactionAssertionException(txId, "Failed to deserialize request " + rawRequest + " " + e);
			}
			catch (JsonMappingException e)
			{
				throw new TransactionAssertionException(txId, "Failed to deserialize request " + rawRequest + " " + e);
			}
			catch (IOException e)
			{
				throw new TransactionAssertionException(txId, "Failed to deserialize request " + rawRequest + " " + e);
			}
		}

		private abstract class AmazonWebServiceRequestMixIn
		{

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @JsonIgnore public abstract String getDelegationToken();
			public abstract string DelegationToken {get;set;}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @JsonIgnore public abstract void setDelegationToken(String delegationToken);

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @JsonIgnore public abstract void setRequestCredentials(com.amazonaws.auth.AWSCredentials credentials);
			public abstract AWSCredentials RequestCredentials {set;get;}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @JsonIgnore public abstract com.amazonaws.auth.AWSCredentials getRequestCredentials();

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @JsonIgnore public abstract java.util.Map<String, String> copyPrivateRequestParameters();
			public abstract IDictionary<string, string> copyPrivateRequestParameters();

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @JsonIgnore public abstract com.amazonaws.RequestClientOptions getRequestClientOptions();
			public abstract RequestClientOptions RequestClientOptions {get;}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @JsonIgnore public abstract com.amazonaws.event.ProgressListener getGeneralProgressListener();
			public abstract ProgressListener GeneralProgressListener {get;set;}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @JsonIgnore public abstract void setGeneralProgressListener(com.amazonaws.event.ProgressListener progressListener);

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @JsonIgnore public abstract int getReadLimit();
			public abstract int ReadLimit {get;}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @JsonIgnore public abstract java.util.Map<String, String> getCustomRequestHeaders();
			public abstract IDictionary<string, string> CustomRequestHeaders {get;}

		}

		private abstract class RequestMixIn : AmazonWebServiceRequestMixIn
		{

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @JsonIgnore public abstract String getDelegationToken();
			public override abstract string DelegationToken {get;set;}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @JsonIgnore public abstract void setDelegationToken(String delegationToken);

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @JsonIgnore public abstract void setRequestCredentials(com.amazonaws.auth.AWSCredentials credentials);
			public override abstract AWSCredentials RequestCredentials {set;get;}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @JsonIgnore public abstract com.amazonaws.auth.AWSCredentials getRequestCredentials();

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @JsonIgnore public abstract java.util.Map<String, String> copyPrivateRequestParameters();
			public override abstract IDictionary<string, string> copyPrivateRequestParameters();

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @JsonIgnore public abstract com.amazonaws.RequestClientOptions getRequestClientOptions();
			public override abstract RequestClientOptions RequestClientOptions {get;}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @JsonIgnore public abstract void setReturnValues(com.amazonaws.services.dynamodbv2.model.ReturnValue returnValue);

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @JsonProperty public abstract void setReturnValues(String returnValue);
			public abstract string ReturnValues {set;}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @JsonIgnore public abstract void setReturnConsumedCapacity(com.amazonaws.services.dynamodbv2.model.ReturnConsumedCapacity returnConsumedCapacity);
			public abstract ReturnConsumedCapacity ReturnConsumedCapacity {set;}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @JsonProperty public abstract void setReturnConsumedCapacity(String returnConsumedCapacity);
			public abstract string ReturnConsumedCapacity {set;}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @JsonIgnore public abstract void setReturnItemCollectionMetrics(com.amazonaws.services.dynamodbv2.model.ReturnItemCollectionMetrics returnItemCollectionMetrics);
			public abstract ReturnItemCollectionMetrics ReturnItemCollectionMetrics {set;}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @JsonProperty public abstract void setReturnItemCollectionMetrics(String returnItemCollectionMetrics);
			public abstract string ReturnItemCollectionMetrics {set;}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @JsonIgnore public abstract boolean isConsistentRead();
			public abstract bool ConsistentRead {get;}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @JsonProperty public abstract boolean getConsistentRead();
			public abstract bool ConsistentRead {get;}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @JsonIgnore public abstract void setConditionalOperator(com.amazonaws.services.dynamodbv2.model.ConditionalOperator conditionalOperator);
			public abstract ConditionalOperator ConditionalOperator {set;}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @JsonProperty public abstract void setConditionalOperator(String conditionalOperator);
			public abstract string ConditionalOperator {set;}

		}

		private abstract class AttributeValueUpdateMixIn
		{

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @JsonIgnore public abstract void setAction(com.amazonaws.services.dynamodbv2.model.AttributeAction attributeAction);
			public abstract AttributeAction Action {set;}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @JsonProperty public abstract void setAction(String attributeAction);
			public abstract string Action {set;}

		}

		private abstract class ExpectedAttributeValueMixIn
		{

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @JsonIgnore public abstract void setComparisonOperator(com.amazonaws.services.dynamodbv2.model.ComparisonOperator comparisonOperator);
			public abstract ComparisonOperator ComparisonOperator {set;}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @JsonProperty public abstract void setComparisonOperator(String comparisonOperator);
			public abstract string ComparisonOperator {set;}

		}

		private class ByteBufferSerializer : JsonSerializer<ByteBuffer>
		{

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void serialize(ByteBuffer value, com.fasterxml.jackson.core.JsonGenerator jgen, com.fasterxml.jackson.databind.SerializerProvider provider) throws java.io.IOException, com.fasterxml.jackson.core.JsonProcessingException
			public override void serialize(ByteBuffer value, JsonGenerator jgen, SerializerProvider provider)
			{
				// value is never null, according to JsonSerializer contract
				jgen.writeBinary(value.array());
			}

		}

		private class ByteBufferDeserializer : JsonDeserializer<ByteBuffer>
		{

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public ByteBuffer deserialize(com.fasterxml.jackson.core.JsonParser jp, com.fasterxml.jackson.databind.DeserializationContext ctxt) throws java.io.IOException, com.fasterxml.jackson.core.JsonProcessingException
			public override ByteBuffer deserialize(JsonParser jp, DeserializationContext ctxt)
			{
				// never called for null literal, according to JsonDeserializer contract
				return ByteBuffer.wrap(jp.BinaryValue);
			}

		}

	}
 }