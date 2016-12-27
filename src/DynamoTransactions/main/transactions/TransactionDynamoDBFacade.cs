using System.Collections.Generic;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;

/// <summary>
/// Copyright 2013-2014 Amazon.com, Inc. or its affiliates. All Rights Reserved.
/// 
/// Licensed under the Amazon Software License (the "License"). You may not use
/// this file except in compliance with the License. A copy of the License is
/// located at
/// 
/// http://aws.amazon.com/asl/
/// 
/// or in the "license" file accompanying this file. This file is distributed on
/// an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, express or
/// implied. See the License for the specific language governing permissions and
/// limitations under the License.
/// </summary>
namespace com.amazonaws.services.dynamodbv2.transactions
{
	/// <summary>
	/// Facade for <seealso cref="AmazonDynamoDBClient"/> that forwards requests to a
	/// <seealso cref="Transaction"/>, omitting conditional checks and consistent read options
	/// from each request. Only supports the operations needed by DynamoDBMapper for
	/// loading, saving or deleting items.
	/// </summary>
	public class TransactionDynamoDBFacade : IAmazonDynamoDB
	{

		private readonly Transaction txn;
		private readonly TransactionManager txManager;

		public TransactionDynamoDBFacade(Transaction txn, TransactionManager txManager)
		{
			this.txn = txn;
			this.txManager = txManager;
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.DeleteItemResponse deleteItem(com.amazonaws.services.dynamodbv2.model.DeleteItemRequest request) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override DeleteItemResponse deleteItem(DeleteItemRequest request)
		{
			Dictionary<string, ExpectedAttributeValue> expectedValues = request.Expected;
			checkExpectedValues(request.TableName, request.Key, expectedValues);

			// conditional checks are handled by the above call
			request.Expected = null;
			return txn.deleteItem(request);
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.GetItemResponse GetItemAsync(com.amazonaws.services.dynamodbv2.model.GetItemRequest request) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override GetItemResponse getItem(GetItemRequest request)
		{
			return txn.getItem(request);
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.PutItemResponse putItem(com.amazonaws.services.dynamodbv2.model.PutItemRequest request) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override PutItemResponse putItem(PutItemRequest request)
		{
			Dictionary<string, ExpectedAttributeValue> expectedValues = request.Expected;
			checkExpectedValues(request.TableName, Request.getKeyFromItem(request.TableName, request.Item, txManager), expectedValues);

			// conditional checks are handled by the above call
			request.Expected = null;
			return txn.putItem(request);
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.UpdateItemResponse updateItem(com.amazonaws.services.dynamodbv2.model.UpdateItemRequest request) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override UpdateItemResponse updateItem(UpdateItemRequest request)
		{
			Dictionary<string, ExpectedAttributeValue> expectedValues = request.Expected;
			checkExpectedValues(request.TableName, request.Key, expectedValues);

			// conditional checks are handled by the above call
			request.Expected = null;
			return txn.updateItem(request);
		}

		private void checkExpectedValues(string tableName, Dictionary<string, AttributeValue> itemKey, Dictionary<string, ExpectedAttributeValue> expectedValues)
		{
			if (expectedValues != null && expectedValues.Count > 0)
			{
				foreach (KeyValuePair<string, ExpectedAttributeValue> entry in expectedValues.SetOfKeyValuePairs())
				{
					if ((entry.Value.Exists == null || entry.Value.Exists == true) && entry.Value.Value == null)
					{
						throw new System.ArgumentException("An explicit value is required when Exists is null or true, " + "but none was found in expected values for item with key " + itemKey + ": " + expectedValues);
					}
				}

				// simulate by loading the item and checking the values;
				// this also has the effect of locking the item, which gives the
				// same behavior
				GetItemResponse result = getItem(new GetItemRequest {

AttributesToGet = expectedValues.Keys,
Key = itemKey,
TableName = tableName,)
};
				Dictionary<string, AttributeValue> item = result.Item;
				try
				{
					checkExpectedValues(expectedValues, item);
				}
				catch (ConditionalCheckFailedException e)
				{
					throw new ConditionalCheckFailedException("Item " + itemKey + " had unexpected attributes: " + e.Message);
				}
			}
		}

		/// <summary>
		/// Checks a map of expected values against a map of actual values in a way
		/// that's compatible with how the DynamoDB service interprets the Expected
		/// parameter of PutItem, UpdateItem and DeleteItem.
		/// </summary>
		/// <param name="expectedValues">
		///            A description of the expected values. </param>
		/// <param name="item">
		///            The actual values. </param>
		/// <exception cref="ConditionalCheckFailedException">
		///             Thrown if the values do not match the expected values. </exception>
		public static void checkExpectedValues(Dictionary<string, ExpectedAttributeValue> expectedValues, Dictionary<string, AttributeValue> item)
		{
			foreach (KeyValuePair<string, ExpectedAttributeValue> entry in expectedValues.SetOfKeyValuePairs())
			{
				// if the attribute is expected to exist (null for isExists means
				// true)
				if ((entry.Value.Exists == null || entry.Value.Exists == true) && (item == null || item[entry.Key] == null || !expectedValueMatches(entry.Value.Value, item[entry.Key])))
						// but the item doesn't
								// or the attribute doesn't
								// or it doesn't have the expected value
				{
					throw new ConditionalCheckFailedException("expected attribute(s) " + expectedValues + " but found " + item);
				}
				else if (entry.Value.Exists != null && !entry.Value.Exists && item != null && item[entry.Key] != null)
				{
					// the attribute isn't expected to exist, but the item exists
					// and the attribute does too
					throw new ConditionalCheckFailedException("expected attribute(s) " + expectedValues + " but found " + item);
				}
			}
		}

		private static bool expectedValueMatches(AttributeValue expected, AttributeValue actual)
		{
			if (expected.N != null)
			{
				return actual.N != null && (new decimal(expected.N)).CompareTo(new decimal(actual.N)) == 0;
			}
			else if (expected.S != null || expected.B != null)
			{
				return expected.Equals(actual);
			}
			else
			{
				throw new System.ArgumentException("Expect condition using unsupported value type: " + expected);
			}
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.BatchGetItemResponse batchGetItem(com.amazonaws.services.dynamodbv2.model.BatchGetItemRequest arg0) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override BatchGetItemResponse batchGetItem(BatchGetItemRequest arg0)
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
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.QueryResponse query(com.amazonaws.services.dynamodbv2.model.QueryRequest arg0) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override QueryResponse query(QueryRequest arg0)
		{
			throw new System.NotSupportedException("Use the underlying client instance instead");
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.ScanResponse scan(com.amazonaws.services.dynamodbv2.model.ScanRequest arg0) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override ScanResponse scan(ScanRequest arg0)
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
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.ScanResponse scan(String tableName, java.util.List<String> attributesToGet) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override ScanResponse scan(string tableName, List<string> attributesToGet)
		{
			throw new System.NotSupportedException("Use the underlying client instance instead");
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.ScanResponse scan(String tableName, java.util.Map<String, com.amazonaws.services.dynamodbv2.model.Condition> scanFilter) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override ScanResponse scan(string tableName, Dictionary<string, Condition> scanFilter)
		{
			throw new System.NotSupportedException("Use the underlying client instance instead");
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.ScanResponse scan(String tableName, java.util.List<String> attributesToGet, java.util.Map<String, com.amazonaws.services.dynamodbv2.model.Condition> scanFilter) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override ScanResponse scan(string tableName, List<string> attributesToGet, Dictionary<string, Condition> scanFilter)
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
		public override BatchWriteItemResponse batchWriteItem(Dictionary<string, List<WriteRequest>> requestItems)
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
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.GetItemResponse GetItemAsync(String tableName, java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> key) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override GetItemResponse getItem(string tableName, Dictionary<string, AttributeValue> key)
		{
			return getItem(new GetItemRequest {

TableName = tableName,
Key = key,)
};
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.GetItemResponse GetItemAsync(String tableName, java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> key, Nullable<bool> consistentRead) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override GetItemResponse getItem(string tableName, Dictionary<string, AttributeValue> key, bool? consistentRead)
		{
			return getItem(new GetItemRequest {

TableName = tableName,
Key = key,
ConsistentRead = consistentRead,)
};
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.DeleteItemResponse deleteItem(String tableName, java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> key) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override DeleteItemResponse deleteItem(string tableName, Dictionary<string, AttributeValue> key)
		{
			return deleteItem(new DeleteItemRequest {

TableName = tableName,
Key = key,)
};
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.DeleteItemResponse deleteItem(String tableName, java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> key, String returnValues) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override DeleteItemResponse deleteItem(string tableName, Dictionary<string, AttributeValue> key, string returnValues)
		{
			return deleteItem(new DeleteItemRequest {

TableName = tableName,
Key = key,
ReturnValues = returnValues,)
};
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.CreateTableResponse createTable(java.util.List<com.amazonaws.services.dynamodbv2.model.AttributeDefinition> attributeDefinitions, String tableName, java.util.List<com.amazonaws.services.dynamodbv2.model.KeySchemaElement> keySchema, com.amazonaws.services.dynamodbv2.model.ProvisionedThroughput provisionedThroughput) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override CreateTableResponse createTable(List<AttributeDefinition> attributeDefinitions, string tableName, List<KeySchemaElement> keySchema, ProvisionedThroughput provisionedThroughput)
		{
			throw new System.NotSupportedException("Use the underlying client instance instead");
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.PutItemResponse putItem(String tableName, java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> item) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override PutItemResponse putItem(string tableName, Dictionary<string, AttributeValue> item)
		{
			return putItem(new PutItemRequest {

TableName = tableName,
Item = item,)
};
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.PutItemResponse putItem(String tableName, java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> item, String returnValues) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override PutItemResponse putItem(string tableName, Dictionary<string, AttributeValue> item, string returnValues)
		{
			return putItem(new PutItemRequest {

TableName = tableName,
Item = item,
ReturnValues = returnValues,)
};
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
		public override UpdateItemResponse updateItem(string tableName, Dictionary<string, AttributeValue> key, Dictionary<string, AttributeValueUpdate> attributeUpdates)
		{
			return updateItem(new UpdateItemRequest {

TableName = tableName,
Key = key,
AttributeUpdates = attributeUpdates,)
};
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.UpdateItemResponse updateItem(String tableName, java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> key, java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValueUpdate> attributeUpdates, String returnValues) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override UpdateItemResponse updateItem(string tableName, Dictionary<string, AttributeValue> key, Dictionary<string, AttributeValueUpdate> attributeUpdates, string returnValues)
		{
			return updateItem(new UpdateItemRequest {

TableName = tableName,
Key = key,
AttributeUpdates = attributeUpdates,
ReturnValues = returnValues,)
};
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.BatchGetItemResponse batchGetItem(java.util.Map<String, com.amazonaws.services.dynamodbv2.model.KeysAndAttributes> requestItems, String returnConsumedCapacity) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override BatchGetItemResponse batchGetItem(Dictionary<string, KeysAndAttributes> requestItems, string returnConsumedCapacity)
		{
			throw new System.NotSupportedException("Use the underlying client instance instead");
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.BatchGetItemResponse batchGetItem(java.util.Map<String, com.amazonaws.services.dynamodbv2.model.KeysAndAttributes> requestItems) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override BatchGetItemResponse batchGetItem(Dictionary<string, KeysAndAttributes> requestItems)
		{
			throw new System.NotSupportedException("Use the underlying client instance instead");
		}

		public override DescribeLimitsResponse describeLimits(DescribeLimitsRequest request)
		{
			throw new System.NotSupportedException("Use the underlying client instance instead");
		}

		public override AmazonDynamoDBWaiters waiters()
		{
			throw new System.NotSupportedException("Use the underlying client instance instead");
		}

	}

}