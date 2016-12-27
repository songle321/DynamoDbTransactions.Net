using System.Collections.Generic;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;

// <summary>
// Copyright 2013-2014 Amazon.com, Inc. or its affiliates. All Rights Reserved.
// 
// Licensed under the Amazon Software License (the "License"). You may not use
// this file except in compliance with the License. A copy of the License is
// located at
// 
// http://aws.amazon.com/asl/
// 
// or in the "license" file accompanying this file. This file is distributed on
// an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, express or
// implied. See the License for the specific language governing permissions and
// limitations under the License.
// </summary>
namespace com.amazonaws.services.dynamodbv2.transactions
{

	/// <summary>
	/// Facade to support the DynamoDBMapper doing a read using a specific isolation
	/// level. Used by <seealso cref="TransactionManager#load(Object, IsolationLevel)"/>.
	/// </summary>
	public class TransactionManagerDynamoDBFacade : IAmazonDynamoDB
	{

		private readonly TransactionManager txManager;
		private readonly Transaction.IsolationLevel isolationLevel;
		private readonly ReadIsolationHandler isolationHandler;

		public TransactionManagerDynamoDBFacade(TransactionManager txManager, Transaction.IsolationLevel isolationLevel)
		{
			this.txManager = txManager;
			this.isolationLevel = isolationLevel;
			this.isolationHandler = txManager.getReadIsolationHandler(isolationLevel);
		}

		/// <summary>
		/// Returns versions of the items can be read at the specified isolation level stripped of
		/// special attributes. </summary>
		/// <param name="items"> The items to check </param>
		/// <param name="tableName"> The table that contains the item </param>
		/// <param name="attributesToGet"> The attributes to get from the table. If null or empty, will
		///                        fetch all attributes. </param>
		/// <returns> Versions of the items that can be read at the isolation level stripped of special attributes </returns>
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
//ORIGINAL LINE: private java.util.List<java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue>> handleItems(final java.util.List<java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue>> items, final String tableName, final java.util.List<String> attributesToGet)
		private IList<IDictionary<string, AttributeValue>> handleItems(IList<IDictionary<string, AttributeValue>> items, string tableName, IList<string> attributesToGet)
		{
			IList<IDictionary<string, AttributeValue>> result = new List<IDictionary<string, AttributeValue>>();
			foreach (IDictionary<string, AttributeValue> item in items)
			{
				IDictionary<string, AttributeValue> handledItem = isolationHandler.handleItem(item, attributesToGet, tableName);
				/// <summary>
				/// If the item is null, BatchGetItems, Scan, and Query should exclude the item from
				/// the returned list. This is based on the DynamoDB documentation.
				/// </summary>
				if (handledItem != null)
				{
					Transaction.stripSpecialAttributes(handledItem);
					result.Add(handledItem);
				}
			}
			return result;
		}

		private ICollection<string> addSpecialAttributes(ICollection<string> attributesToGet)
		{
			if (attributesToGet == null)
			{
				return null;
			}
			ISet<string> result = new HashSet<string>(attributesToGet);
			result.addAll(Transaction.SPECIAL_ATTR_NAMES);
			return result;
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.GetItemResponse getItem(com.amazonaws.services.dynamodbv2.model.GetItemRequest request) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override GetItemResponse getItem(GetItemRequest request)
		{
			return txManager.getItem(request, isolationLevel);
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.GetItemResponse getItem(String tableName, java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> key) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override GetItemResponse getItem(string tableName, IDictionary<string, AttributeValue> key)
		{
			return getItem(new GetItemRequest()
					.withTableName(tableName).withKey(key));
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.GetItemResponse getItem(String tableName, java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> key, Nullable<bool> consistentRead) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override GetItemResponse getItem(string tableName, IDictionary<string, AttributeValue> key, bool? consistentRead)
		{
			return getItem(new GetItemRequest()
					.withTableName(tableName).withKey(key).withConsistentRead(consistentRead));
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.BatchGetItemResponse batchGetItem(com.amazonaws.services.dynamodbv2.model.BatchGetItemRequest request) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override BatchGetItemResponse batchGetItem(BatchGetItemRequest request)
		{
			foreach (KeysAndAttributes keysAndAttributes in request.RequestItems.values())
			{
				ICollection<string> attributesToGet = keysAndAttributes.AttributesToGet;
				keysAndAttributes.AttributesToGet = addSpecialAttributes(attributesToGet);
			}
			BatchGetItemResponse result = txManager.Client.batchGetItem(request);
			IDictionary<string, IList<IDictionary<string, AttributeValue>>> responses = new Dictionary<string, IList<IDictionary<string, AttributeValue>>>();
			foreach (KeyValuePair<string, IList<IDictionary<string, AttributeValue>>> e in result.Responses.entrySet())
			{
				string tableName = e.Key;
				IList<string> attributesToGet = request.RequestItems.get(tableName).AttributesToGet;
				IList<IDictionary<string, AttributeValue>> items = handleItems(e.Value, tableName, attributesToGet);
				responses[tableName] = items;
			}
			result.Responses = responses;
			return result;
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.BatchGetItemResponse batchGetItem(java.util.Map<String, com.amazonaws.services.dynamodbv2.model.KeysAndAttributes> requestItems, String returnConsumedCapacity) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override BatchGetItemResponse batchGetItem(IDictionary<string, KeysAndAttributes> requestItems, string returnConsumedCapacity)
		{
			BatchGetItemRequest request = (new BatchGetItemRequest()).withRequestItems(requestItems).withReturnConsumedCapacity(returnConsumedCapacity);
			return batchGetItem(request);
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.BatchGetItemResponse batchGetItem(java.util.Map<String, com.amazonaws.services.dynamodbv2.model.KeysAndAttributes> requestItems) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override BatchGetItemResponse batchGetItem(IDictionary<string, KeysAndAttributes> requestItems)
		{
			BatchGetItemRequest request = (new BatchGetItemRequest()).withRequestItems(requestItems);
			return batchGetItem(request);
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.ScanResponse scan(com.amazonaws.services.dynamodbv2.model.ScanRequest request) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override ScanResponse scan(ScanRequest request)
		{
			ICollection<string> attributesToGet = addSpecialAttributes(request.AttributesToGet);
			request.AttributesToGet = attributesToGet;
			ScanResponse result = txManager.Client.scan(request);
			IList<IDictionary<string, AttributeValue>> items = handleItems(result.Items, request.TableName, request.AttributesToGet);
			result.Items = items;
			return result;
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.ScanResponse scan(String tableName, java.util.List<String> attributesToGet) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override ScanResponse scan(string tableName, IList<string> attributesToGet)
		{
			ScanRequest request = (new ScanRequest()).withTableName(tableName).withAttributesToGet(attributesToGet);
			return scan(request);
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.ScanResponse scan(String tableName, java.util.Map<String, com.amazonaws.services.dynamodbv2.model.Condition> scanFilter) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override ScanResponse scan(string tableName, IDictionary<string, Condition> scanFilter)
		{
			ScanRequest request = (new ScanRequest()).withTableName(tableName).withScanFilter(scanFilter);
			return scan(request);
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.ScanResponse scan(String tableName, java.util.List<String> attributesToGet, java.util.Map<String, com.amazonaws.services.dynamodbv2.model.Condition> scanFilter) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override ScanResponse scan(string tableName, IList<string> attributesToGet, IDictionary<string, Condition> scanFilter)
		{
			ScanRequest request = (new ScanRequest()).withTableName(tableName).withAttributesToGet(attributesToGet).withScanFilter(scanFilter);
			return scan(request);
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.QueryResponse query(com.amazonaws.services.dynamodbv2.model.QueryRequest request) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override QueryResponse query(QueryRequest request)
		{
			ICollection<string> attributesToGet = addSpecialAttributes(request.AttributesToGet);
			request.AttributesToGet = attributesToGet;
			QueryResponse result = txManager.Client.query(request);
			IList<IDictionary<string, AttributeValue>> items = handleItems(result.Items, request.TableName, request.AttributesToGet);
			result.Items = items;
			return result;
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.PutItemResponse putItem(com.amazonaws.services.dynamodbv2.model.PutItemRequest request) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override PutItemResponse putItem(PutItemRequest request)
		{
			throw new System.NotSupportedException("Use the underlying client instance instead");
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.UpdateItemResponse updateItem(com.amazonaws.services.dynamodbv2.model.UpdateItemRequest request) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override UpdateItemResponse updateItem(UpdateItemRequest request)
		{
			throw new System.NotSupportedException("Use the underlying client instance instead");
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.DeleteItemResponse deleteItem(com.amazonaws.services.dynamodbv2.model.DeleteItemRequest request) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override DeleteItemResponse deleteItem(DeleteItemRequest request)
		{
			throw new System.NotSupportedException("Use the underlying client instance instead");
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.BatchWriteItemResponse batchWriteItem(com.amazonaws.services.dynamodbv2.model.BatchWriteItemRequest arg0) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override BatchWriteItemResponse batchWriteItem(BatchWriteItemRequest arg0)
		{
			throw new System.NotSupportedException("Use the underlying client instance instead");
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.CreateTableResponse createTable(com.amazonaws.services.dynamodbv2.model.CreateTableRequest arg0) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override CreateTableResponse createTable(CreateTableRequest arg0)
		{
			throw new System.NotSupportedException("Use the underlying client instance instead");
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.DeleteTableResponse deleteTable(com.amazonaws.services.dynamodbv2.model.DeleteTableRequest arg0) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override DeleteTableResponse deleteTable(DeleteTableRequest arg0)
		{
			throw new System.NotSupportedException("Use the underlying client instance instead");
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.DescribeTableResponse describeTable(com.amazonaws.services.dynamodbv2.model.DescribeTableRequest arg0) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override DescribeTableResponse describeTable(DescribeTableRequest arg0)
		{
			throw new System.NotSupportedException("Use the underlying client instance instead");
		}

		public override ResponseMetadata getCachedResponseMetadata(AmazonWebServiceRequest arg0)
		{
			throw new System.NotSupportedException("Use the underlying client instance instead");
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.ListTablesResponse listTables() throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override ListTablesResponse listTables()
		{
			throw new System.NotSupportedException("Use the underlying client instance instead");
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.ListTablesResponse listTables(com.amazonaws.services.dynamodbv2.model.ListTablesRequest arg0) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override ListTablesResponse listTables(ListTablesRequest arg0)
		{
			throw new System.NotSupportedException("Use the underlying client instance instead");
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void setEndpoint(String arg0) throws IllegalArgumentException
		public override string Endpoint
		{
			set
			{
				throw new System.NotSupportedException("Use the underlying client instance instead");
			}
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void setRegion(com.amazonaws.regions.Region arg0) throws IllegalArgumentException
		public override Region Region
		{
			set
			{
				throw new System.NotSupportedException("Use the underlying client instance instead");
			}
		}

		public override void shutdown()
		{
			throw new System.NotSupportedException("Use the underlying client instance instead");
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.UpdateTableResponse updateTable(com.amazonaws.services.dynamodbv2.model.UpdateTableRequest arg0) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override UpdateTableResponse updateTable(UpdateTableRequest arg0)
		{
			throw new System.NotSupportedException("Use the underlying client instance instead");
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.UpdateTableResponse updateTable(String tableName, com.amazonaws.services.dynamodbv2.model.ProvisionedThroughput provisionedThroughput) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override UpdateTableResponse updateTable(string tableName, ProvisionedThroughput provisionedThroughput)
		{
			throw new System.NotSupportedException("Use the underlying client instance instead");
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.DeleteTableResponse deleteTable(String tableName) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override DeleteTableResponse deleteTable(string tableName)
		{
			throw new System.NotSupportedException("Use the underlying client instance instead");
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.BatchWriteItemResponse batchWriteItem(java.util.Map<String, java.util.List<com.amazonaws.services.dynamodbv2.model.WriteRequest>> requestItems) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override BatchWriteItemResponse batchWriteItem(IDictionary<string, IList<WriteRequest>> requestItems)
		{
			throw new System.NotSupportedException("Use the underlying client instance instead");
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.DescribeTableResponse describeTable(String tableName) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override DescribeTableResponse describeTable(string tableName)
		{
			throw new System.NotSupportedException("Use the underlying client instance instead");
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.DeleteItemResponse deleteItem(String tableName, java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> key) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override DeleteItemResponse deleteItem(string tableName, IDictionary<string, AttributeValue> key)
		{
			throw new System.NotSupportedException("Use the underlying client instance instead");
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.DeleteItemResponse deleteItem(String tableName, java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> key, String returnValues) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override DeleteItemResponse deleteItem(string tableName, IDictionary<string, AttributeValue> key, string returnValues)
		{
			throw new System.NotSupportedException("Use the underlying client instance instead");
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.CreateTableResponse createTable(java.util.List<com.amazonaws.services.dynamodbv2.model.AttributeDefinition> attributeDefinitions, String tableName, java.util.List<com.amazonaws.services.dynamodbv2.model.KeySchemaElement> keySchema, com.amazonaws.services.dynamodbv2.model.ProvisionedThroughput provisionedThroughput) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override CreateTableResponse createTable(IList<AttributeDefinition> attributeDefinitions, string tableName, IList<KeySchemaElement> keySchema, ProvisionedThroughput provisionedThroughput)
		{
			throw new System.NotSupportedException("Use the underlying client instance instead");
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.PutItemResponse putItem(String tableName, java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> item) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override PutItemResponse putItem(string tableName, IDictionary<string, AttributeValue> item)
		{
			throw new System.NotSupportedException("Use the underlying client instance instead");
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.PutItemResponse putItem(String tableName, java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> item, String returnValues) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override PutItemResponse putItem(string tableName, IDictionary<string, AttributeValue> item, string returnValues)
		{
			throw new System.NotSupportedException("Use the underlying client instance instead");
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.ListTablesResponse listTables(String exclusiveStartTableName) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override ListTablesResponse listTables(string exclusiveStartTableName)
		{
			throw new System.NotSupportedException("Use the underlying client instance instead");
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.ListTablesResponse listTables(String exclusiveStartTableName, Nullable<int> limit) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override ListTablesResponse listTables(string exclusiveStartTableName, int? limit)
		{
			throw new System.NotSupportedException("Use the underlying client instance instead");
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.ListTablesResponse listTables(Nullable<int> limit) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override ListTablesResponse listTables(int? limit)
		{
			throw new System.NotSupportedException("Use the underlying client instance instead");
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.UpdateItemResponse updateItem(String tableName, java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> key, java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValueUpdate> attributeUpdates) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override UpdateItemResponse updateItem(string tableName, IDictionary<string, AttributeValue> key, IDictionary<string, AttributeValueUpdate> attributeUpdates)
		{
			throw new System.NotSupportedException("Use the underlying client instance instead");
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.UpdateItemResponse updateItem(String tableName, java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> key, java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValueUpdate> attributeUpdates, String returnValues) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override UpdateItemResponse updateItem(string tableName, IDictionary<string, AttributeValue> key, IDictionary<string, AttributeValueUpdate> attributeUpdates, string returnValues)
		{
			throw new System.NotSupportedException("Use the underlying client instance instead");
		}

	}

}