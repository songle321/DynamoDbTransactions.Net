using System;
using System.Collections.Generic;
using System.Threading;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;

/// <summary>
/// Copyright 2014-2014 Amazon.com, Inc. or its affiliates. All Rights Reserved.
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
	/// Necessary to work around a limitation of the mapper. The mapper always gets
	/// created with a fresh reflection cache, which is expensive to repopulate.
	/// Using this class to route to a different facade for each request allows us to
	/// reuse the mapper and its underlying cache for each call to the mapper from a
	/// transaction or the transaction manager.
	/// </summary>
	public class ThreadLocalDynamoDBFacade : AmazonDynamoDBClient
	{

		private readonly ThreadLocal<AmazonDynamoDBClient> backend = new ThreadLocal<AmazonDynamoDBClient>();

		private AmazonDynamoDBClient Backend
		{
			get
			{
				if (backend.Value.get() == null)
				{
					throw new Exception("No backend to proxy");
				}
				return backend.get();
			}
			set
			{
				backend.set(value);
			}
		}


//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.BatchGetItemResponse batchGetItem(com.amazonaws.services.dynamodbv2.model.BatchGetItemRequest request) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override BatchGetItemResponse batchGetItem(BatchGetItemRequest request)
		{
			return Backend.batchGetItem(request);
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.BatchWriteItemResponse batchWriteItem(com.amazonaws.services.dynamodbv2.model.BatchWriteItemRequest request) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override BatchWriteItemResponse batchWriteItem(BatchWriteItemRequest request)
		{
			return Backend.batchWriteItem(request);
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.CreateTableResponse createTable(com.amazonaws.services.dynamodbv2.model.CreateTableRequest request) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override CreateTableResponse createTable(CreateTableRequest request)
		{
			return Backend.createTable(request);
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.DeleteItemResponse deleteItem(com.amazonaws.services.dynamodbv2.model.DeleteItemRequest request) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override DeleteItemResponse deleteItem(DeleteItemRequest request)
		{
			return Backend.deleteItem(request);
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.DeleteTableResponse deleteTable(com.amazonaws.services.dynamodbv2.model.DeleteTableRequest request) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override DeleteTableResponse deleteTable(DeleteTableRequest request)
		{
			return Backend.deleteTable(request);
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.DescribeTableResponse describeTable(com.amazonaws.services.dynamodbv2.model.DescribeTableRequest request) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override DescribeTableResponse describeTable(DescribeTableRequest request)
		{
			return Backend.describeTable(request);
		}

		public override ResponseMetadata getCachedResponseMetadata(AmazonWebServiceRequest request)
		{
			return Backend.getCachedResponseMetadata(request);
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.GetItemResponse getItem(com.amazonaws.services.dynamodbv2.model.GetItemRequest request) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override GetItemResponse getItem(GetItemRequest request)
		{
			return Backend.getItem(request);
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.ListTablesResponse listTables() throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override ListTablesResponse listTables()
		{
			return Backend.listTables();
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.ListTablesResponse listTables(com.amazonaws.services.dynamodbv2.model.ListTablesRequest request) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override ListTablesResponse listTables(ListTablesRequest request)
		{
			return Backend.listTables(request);
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.PutItemResponse putItem(com.amazonaws.services.dynamodbv2.model.PutItemRequest request) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override PutItemResponse putItem(PutItemRequest request)
		{
			return Backend.putItem(request);
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.QueryResponse query(com.amazonaws.services.dynamodbv2.model.QueryRequest request) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override QueryResponse query(QueryRequest request)
		{
			return Backend.query(request);
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.ScanResponse scan(com.amazonaws.services.dynamodbv2.model.ScanRequest request) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override ScanResponse scan(ScanRequest request)
		{
			return Backend.scan(request);
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void setEndpoint(String request) throws IllegalArgumentException
		public override string Endpoint
		{
			set
			{
				Backend.Endpoint = value;
			}
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void setRegion(com.amazonaws.regions.Region request) throws IllegalArgumentException
		public override Region Region
		{
			set
			{
				Backend.Region = value;
			}
		}

		public override void shutdown()
		{
			Backend.shutdown();
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.UpdateItemResponse updateItem(com.amazonaws.services.dynamodbv2.model.UpdateItemRequest request) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override UpdateItemResponse updateItem(UpdateItemRequest request)
		{
			return Backend.updateItem(request);
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.UpdateTableResponse updateTable(com.amazonaws.services.dynamodbv2.model.UpdateTableRequest request) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override UpdateTableResponse updateTable(UpdateTableRequest request)
		{
			return Backend.updateTable(request);
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.ScanResponse scan(String tableName, java.util.List<String> attributesToGet) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override ScanResponse scan(string tableName, IList<string> attributesToGet)
		{
			return Backend.scan(tableName, attributesToGet);
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.ScanResponse scan(String tableName, java.util.Map<String, com.amazonaws.services.dynamodbv2.model.Condition> scanFilter) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override ScanResponse scan(string tableName, IDictionary<string, Condition> scanFilter)
		{
			return Backend.scan(tableName, scanFilter);
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.ScanResponse scan(String tableName, java.util.List<String> attributesToGet, java.util.Map<String, com.amazonaws.services.dynamodbv2.model.Condition> scanFilter) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override ScanResponse scan(string tableName, IList<string> attributesToGet, IDictionary<string, Condition> scanFilter)
		{
			return Backend.scan(tableName, attributesToGet, scanFilter);
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.UpdateTableResponse updateTable(String tableName, com.amazonaws.services.dynamodbv2.model.ProvisionedThroughput provisionedThroughput) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override UpdateTableResponse updateTable(string tableName, ProvisionedThroughput provisionedThroughput)
		{
			return Backend.updateTable(tableName, provisionedThroughput);
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.DeleteTableResponse deleteTable(String tableName) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override DeleteTableResponse deleteTable(string tableName)
		{
			return Backend.deleteTable(tableName);
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.BatchWriteItemResponse batchWriteItem(java.util.Map<String, java.util.List<com.amazonaws.services.dynamodbv2.model.WriteRequest>> requestItems) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override BatchWriteItemResponse batchWriteItem(IDictionary<string, IList<WriteRequest>> requestItems)
		{
			return Backend.batchWriteItem(requestItems);
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.DescribeTableResponse describeTable(String tableName) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override DescribeTableResponse describeTable(string tableName)
		{
			return Backend.describeTable(tableName);
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.GetItemResponse getItem(String tableName, java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> key) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override GetItemResponse getItem(string tableName, IDictionary<string, AttributeValue> key)
		{
			return Backend.getItem(tableName, key);
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.GetItemResponse getItem(String tableName, java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> key, Nullable<bool> consistentRead) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override GetItemResponse getItem(string tableName, IDictionary<string, AttributeValue> key, bool? consistentRead)
		{
			return Backend.getItem(tableName, key, consistentRead);
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.DeleteItemResponse deleteItem(String tableName, java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> key) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override DeleteItemResponse deleteItem(string tableName, IDictionary<string, AttributeValue> key)
		{
			return Backend.deleteItem(tableName, key);
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.DeleteItemResponse deleteItem(String tableName, java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> key, String returnValues) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override DeleteItemResponse deleteItem(string tableName, IDictionary<string, AttributeValue> key, string returnValues)
		{
			return Backend.deleteItem(tableName, key, returnValues);
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.CreateTableResponse createTable(java.util.List<com.amazonaws.services.dynamodbv2.model.AttributeDefinition> attributeDefinitions, String tableName, java.util.List<com.amazonaws.services.dynamodbv2.model.KeySchemaElement> keySchema, com.amazonaws.services.dynamodbv2.model.ProvisionedThroughput provisionedThroughput) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override CreateTableResponse createTable(IList<AttributeDefinition> attributeDefinitions, string tableName, IList<KeySchemaElement> keySchema, ProvisionedThroughput provisionedThroughput)
		{
			return Backend.createTable(attributeDefinitions, tableName, keySchema, provisionedThroughput);
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.PutItemResponse putItem(String tableName, java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> item) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override PutItemResponse putItem(string tableName, IDictionary<string, AttributeValue> item)
		{
			return Backend.putItem(tableName, item);
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.PutItemResponse putItem(String tableName, java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> item, String returnValues) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override PutItemResponse putItem(string tableName, IDictionary<string, AttributeValue> item, string returnValues)
		{
			return Backend.putItem(tableName, item, returnValues);
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.ListTablesResponse listTables(String exclusiveStartTableName) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override ListTablesResponse listTables(string exclusiveStartTableName)
		{
			return Backend.listTables(exclusiveStartTableName);
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.ListTablesResponse listTables(String exclusiveStartTableName, Nullable<int> limit) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override ListTablesResponse listTables(string exclusiveStartTableName, int? limit)
		{
			return Backend.listTables(exclusiveStartTableName, limit);
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.ListTablesResponse listTables(Nullable<int> limit) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override ListTablesResponse listTables(int? limit)
		{
			return Backend.listTables(limit);
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.UpdateItemResponse updateItem(String tableName, java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> key, java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValueUpdate> attributeUpdates) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override UpdateItemResponse updateItem(string tableName, IDictionary<string, AttributeValue> key, IDictionary<string, AttributeValueUpdate> attributeUpdates)
		{
			return Backend.updateItem(tableName, key, attributeUpdates);
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.UpdateItemResponse updateItem(String tableName, java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> key, java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValueUpdate> attributeUpdates, String returnValues) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override UpdateItemResponse updateItem(string tableName, IDictionary<string, AttributeValue> key, IDictionary<string, AttributeValueUpdate> attributeUpdates, string returnValues)
		{
			return Backend.updateItem(tableName, key, attributeUpdates, returnValues);
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.BatchGetItemResponse batchGetItem(java.util.Map<String, com.amazonaws.services.dynamodbv2.model.KeysAndAttributes> requestItems, String returnConsumedCapacity) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override BatchGetItemResponse batchGetItem(IDictionary<string, KeysAndAttributes> requestItems, string returnConsumedCapacity)
		{
			return Backend.batchGetItem(requestItems, returnConsumedCapacity);
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.BatchGetItemResponse batchGetItem(java.util.Map<String, com.amazonaws.services.dynamodbv2.model.KeysAndAttributes> requestItems) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override BatchGetItemResponse batchGetItem(IDictionary<string, KeysAndAttributes> requestItems)
		{
			return Backend.batchGetItem(requestItems);
		}

		public override DescribeLimitsResponse describeLimits(DescribeLimitsRequest request)
		{
			return Backend.describeLimits(request);
		}

		public override AmazonDynamoDBWaiters waiters()
		{
			return Backend.waiters();
		}

	}

}