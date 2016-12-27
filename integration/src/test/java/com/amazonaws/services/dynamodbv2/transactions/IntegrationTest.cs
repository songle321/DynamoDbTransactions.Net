using System;
using System.Collections.Generic;

/// <summary>
/// Copyright 2015-2015 Amazon.com, Inc. or its affiliates. All Rights Reserved.
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

	using AWSCredentials = com.amazonaws.auth.AWSCredentials;
	using BasicAWSCredentials = com.amazonaws.auth.BasicAWSCredentials;
	using PropertiesCredentials = com.amazonaws.auth.PropertiesCredentials;
	using ProfileCredentialsProvider = com.amazonaws.auth.profile.ProfileCredentialsProvider;
	using DynamoDBMapperConfig = com.amazonaws.services.dynamodbv2.datamodeling.DynamoDBMapperConfig;
	using DynamoDB = com.amazonaws.services.dynamodbv2.document.DynamoDB;
	using Table = com.amazonaws.services.dynamodbv2.document.Table;
	using AttributeDefinition = com.amazonaws.services.dynamodbv2.model.AttributeDefinition;
	using AttributeValue = com.amazonaws.services.dynamodbv2.model.AttributeValue;
	using CreateTableRequest = com.amazonaws.services.dynamodbv2.model.CreateTableRequest;
	using GetItemRequest = com.amazonaws.services.dynamodbv2.model.GetItemRequest;
	using GetItemResult = com.amazonaws.services.dynamodbv2.model.GetItemResult;
	using KeySchemaElement = com.amazonaws.services.dynamodbv2.model.KeySchemaElement;
	using KeyType = com.amazonaws.services.dynamodbv2.model.KeyType;
	using ProvisionedThroughput = com.amazonaws.services.dynamodbv2.model.ProvisionedThroughput;
	using ResourceInUseException = com.amazonaws.services.dynamodbv2.model.ResourceInUseException;
	using ReturnConsumedCapacity = com.amazonaws.services.dynamodbv2.model.ReturnConsumedCapacity;
	using ScalarAttributeType = com.amazonaws.services.dynamodbv2.model.ScalarAttributeType;
	using TransactionNotFoundException = com.amazonaws.services.dynamodbv2.transactions.exceptions.TransactionNotFoundException;
	using ImmutableKey = com.amazonaws.services.dynamodbv2.util.ImmutableKey;
	using AfterClass = org.junit.AfterClass;
	using BeforeClass = org.junit.BeforeClass;
	using Ignore = org.junit.Ignore;


//JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to C#:
//	import static org.junit.Assert.assertEquals;
//JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to C#:
//	import static org.junit.Assert.assertFalse;
//JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to C#:
//	import static org.junit.Assert.assertNotNull;
//JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to C#:
//	import static org.junit.Assert.assertNull;
//JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to C#:
//	import static org.junit.Assert.assertTrue;
//JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to C#:
//	import static org.junit.Assert.fail;

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Ignore public class IntegrationTest
	public class IntegrationTest
	{

		protected internal static readonly FailingAmazonDynamoDBClient dynamodb;
		protected internal static readonly DynamoDB documentDynamoDB;
		private const string DYNAMODB_ENDPOINT = "http://dynamodb.us-west-2.amazonaws.com";
		private const string DYNAMODB_ENDPOINT_PROPERTY = "dynamodb-local.endpoint";

		protected internal const string ID_ATTRIBUTE = "Id";
		protected internal const string HASH_TABLE_NAME = "TransactionsIntegrationTest_Hash";
		protected internal const string HASH_RANGE_TABLE_NAME = "TransactionsIntegrationTest_HashRange";
		protected internal const string LOCK_TABLE_NAME = "TransactionsIntegrationTest_Transactions";
		protected internal const string IMAGES_TABLE_NAME = "TransactionsIntegrationTest_ItemImages";
		protected internal static readonly string TABLE_NAME_PREFIX = new SimpleDateFormat("yyyy-MM-dd'T'HH-mm-ss").format(DateTime.Now);

		protected internal static readonly string INTEG_LOCK_TABLE_NAME = TABLE_NAME_PREFIX + "_" + LOCK_TABLE_NAME;
		protected internal static readonly string INTEG_IMAGES_TABLE_NAME = TABLE_NAME_PREFIX + "_" + IMAGES_TABLE_NAME;
		protected internal static readonly string INTEG_HASH_TABLE_NAME = TABLE_NAME_PREFIX + "_" + HASH_TABLE_NAME;
		protected internal static readonly string INTEG_HASH_RANGE_TABLE_NAME = TABLE_NAME_PREFIX + "_" + HASH_RANGE_TABLE_NAME;

		protected internal readonly TransactionManager manager;

		public IntegrationTest()
		{
			manager = new TransactionManager(dynamodb, INTEG_LOCK_TABLE_NAME, INTEG_IMAGES_TABLE_NAME);
		}

		public IntegrationTest(DynamoDBMapperConfig config)
		{
			manager = new TransactionManager(dynamodb, INTEG_LOCK_TABLE_NAME, INTEG_IMAGES_TABLE_NAME, config);
		}

		static IntegrationTest()
		{
			AWSCredentials credentials;
			string endpoint = System.getProperty(DYNAMODB_ENDPOINT_PROPERTY);
			if (!string.ReferenceEquals(endpoint, null))
			{
				credentials = new BasicAWSCredentials("local", "local");
			}
			else
			{
				endpoint = DYNAMODB_ENDPOINT;
				try
				{
					credentials = new PropertiesCredentials(typeof(TransactionsIntegrationTest).getResourceAsStream("AwsCredentials.properties"));
					if (credentials.AWSAccessKeyId.Empty)
					{
						Console.Error.WriteLine("No credentials supplied in AwsCredentials.properties, will try with default credentials file");
						credentials = (new ProfileCredentialsProvider()).Credentials;
					}
				}
				catch (IOException e)
				{
					Console.Error.WriteLine("Could not load credentials from built-in credentials file.");
					throw new Exception(e);
				}
			}

			dynamodb = new FailingAmazonDynamoDBClient(credentials);
			dynamodb.Endpoint = endpoint;

			documentDynamoDB = new DynamoDB(dynamodb);
		}

		protected internal IDictionary<string, AttributeValue> key0;
		protected internal IDictionary<string, AttributeValue> item0;

		protected internal virtual IDictionary<string, AttributeValue> newKey(string tableName)
		{
			IDictionary<string, AttributeValue> key = new Dictionary<string, AttributeValue>();
			key[ID_ATTRIBUTE] = (new AttributeValue()).withS("val_" + GlobalRandom.NextDouble);
			if (INTEG_HASH_RANGE_TABLE_NAME.Equals(tableName))
			{
				key["RangeAttr"] = (new AttributeValue()).withN(Convert.ToString(GlobalRandom.NextDouble));
			}
			else if (!INTEG_HASH_TABLE_NAME.Equals(tableName))
			{
				throw new System.ArgumentException();
			}
			return key;
		}

		private static void waitForTableToBecomeAvailable(string tableName)
		{
			Table tableToWaitFor = documentDynamoDB.getTable(tableName);
			try
			{
				Console.WriteLine("Waiting for " + tableName + " to become ACTIVE...");
				tableToWaitFor.waitForActive();
			}
			catch (Exception)
			{
				throw new Exception("Table " + tableName + " never went active");
			}
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @BeforeClass public static void createTables() throws InterruptedException
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
		public static void createTables()
		{
			try
			{
				CreateTableRequest createHash = (new CreateTableRequest()).withTableName(INTEG_HASH_TABLE_NAME).withAttributeDefinitions((new AttributeDefinition()).withAttributeName(ID_ATTRIBUTE).withAttributeType(ScalarAttributeType.S)).withKeySchema((new KeySchemaElement()).withAttributeName(ID_ATTRIBUTE).withKeyType(KeyType.HASH)).withProvisionedThroughput((new ProvisionedThroughput()).withReadCapacityUnits(5L).withWriteCapacityUnits(5L));
				dynamodb.createTable(createHash);
			}
			catch (ResourceInUseException)
			{
				Console.Error.WriteLine("Warning: " + INTEG_HASH_TABLE_NAME + " was already in use");
			}

			try
			{
				TransactionManager.verifyOrCreateTransactionTable(dynamodb, INTEG_LOCK_TABLE_NAME, 10L, 10L, 5L * 60);
				TransactionManager.verifyOrCreateTransactionImagesTable(dynamodb, INTEG_IMAGES_TABLE_NAME, 10L, 10L, 5L * 60);
			}
			catch (ResourceInUseException)
			{
				Console.Error.WriteLine("Warning: " + INTEG_HASH_TABLE_NAME + " was already in use");
			}

			waitForTableToBecomeAvailable(INTEG_HASH_TABLE_NAME);
			waitForTableToBecomeAvailable(INTEG_LOCK_TABLE_NAME);
			waitForTableToBecomeAvailable(INTEG_IMAGES_TABLE_NAME);
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @AfterClass public static void deleteTables() throws InterruptedException
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
		public static void deleteTables()
		{
			try
			{
				Table hashTable = documentDynamoDB.getTable(INTEG_HASH_TABLE_NAME);
				Table lockTable = documentDynamoDB.getTable(INTEG_LOCK_TABLE_NAME);
				Table imagesTable = documentDynamoDB.getTable(INTEG_IMAGES_TABLE_NAME);

				Console.WriteLine("Issuing DeleteTable request for " + INTEG_HASH_TABLE_NAME);
				hashTable.delete();
				Console.WriteLine("Issuing DeleteTable request for " + INTEG_LOCK_TABLE_NAME);
				lockTable.delete();
				Console.WriteLine("Issuing DeleteTable request for " + INTEG_IMAGES_TABLE_NAME);
				imagesTable.delete();

				Console.WriteLine("Waiting for " + INTEG_HASH_TABLE_NAME + " to be deleted...this may take a while...");
				hashTable.waitForDelete();
				Console.WriteLine("Waiting for " + INTEG_LOCK_TABLE_NAME + " to be deleted...this may take a while...");
				lockTable.waitForDelete();
				Console.WriteLine("Waiting for " + INTEG_IMAGES_TABLE_NAME + " to be deleted...this may take a while...");
				imagesTable.waitForDelete();
			}
			catch (Exception e)
			{
				Console.Error.WriteLine("DeleteTable request failed for some table");
				Console.Error.WriteLine(e.Message);
			}
		}

		protected internal virtual void assertItemLocked(string tableName, IDictionary<string, AttributeValue> key, IDictionary<string, AttributeValue> expected, string owner, bool isTransient, bool isApplied)
		{
			assertItemLocked(tableName, key, expected, owner, isTransient, isApplied, true);
		}

		protected internal virtual void assertItemLocked(string tableName, IDictionary<string, AttributeValue> key, IDictionary<string, AttributeValue> expected, string owner, bool isTransient, bool isApplied, bool checkTxItem)
		{
			IDictionary<string, AttributeValue> item = getItem(tableName, key);
			assertNotNull(item);
			assertEquals(owner, item[Transaction.AttributeName.TXID.ToString()].S);
			if (isTransient)
			{
				assertTrue("item is not transient, and should have been", item.ContainsKey(Transaction.AttributeName.TRANSIENT.ToString()));
				assertEquals("item is not transient, and should have been", "1", item[Transaction.AttributeName.TRANSIENT.ToString()].S);
			}
			else
			{
				assertNull("item is transient, and should not have been", item[Transaction.AttributeName.TRANSIENT.ToString()]);
			}
			if (isApplied)
			{
				assertTrue("item is not applied, and should have been", item.ContainsKey(Transaction.AttributeName.APPLIED.ToString()));
				assertEquals("item is not applied, and should have been", "1", item[Transaction.AttributeName.APPLIED.ToString()].S);
			}
			else
			{
				assertNull("item is applied, and should not have been", item[Transaction.AttributeName.APPLIED.ToString()]);
			}
			assertTrue(item.ContainsKey(Transaction.AttributeName.DATE.ToString()));
			if (expected != null)
			{
				item.Remove(Transaction.AttributeName.TXID.ToString());
				item.Remove(Transaction.AttributeName.TRANSIENT.ToString());
				item.Remove(Transaction.AttributeName.APPLIED.ToString());
				item.Remove(Transaction.AttributeName.DATE.ToString());
				assertEquals(expected, item);
			}
			// Also verify that it is locked in the tx record
			if (checkTxItem)
			{
				TransactionItem txItem = new TransactionItem(owner, manager, false);
				assertTrue(txItem.RequestMap.containsKey(tableName));
				assertTrue(txItem.RequestMap.get(tableName).containsKey(new ImmutableKey(key)));
			}
		}

		protected internal virtual void assertItemLocked(string tableName, IDictionary<string, AttributeValue> key, string owner, bool isTransient, bool isApplied)
		{
			assertItemLocked(tableName, key, null, owner, isTransient, isApplied);
		}

		protected internal virtual void assertItemNotLocked(string tableName, IDictionary<string, AttributeValue> key, IDictionary<string, AttributeValue> expected, bool shouldExist)
		{
			IDictionary<string, AttributeValue> item = getItem(tableName, key);
			if (shouldExist)
			{
				assertNotNull("Item does not exist in the table, but it should", item);
				assertNull(item[Transaction.AttributeName.TRANSIENT.ToString()]);
				assertNull(item[Transaction.AttributeName.TXID.ToString()]);
				assertNull(item[Transaction.AttributeName.APPLIED.ToString()]);
				assertNull(item[Transaction.AttributeName.DATE.ToString()]);
			}
			else
			{
				assertNull("Item should have been null: " + item, item);
			}

			if (expected != null)
			{
				item.Remove(Transaction.AttributeName.TXID.ToString());
				item.Remove(Transaction.AttributeName.TRANSIENT.ToString());
				assertEquals(expected, item);
			}
		}

		protected internal virtual void assertItemNotLocked(string tableName, IDictionary<string, AttributeValue> key, bool shouldExist)
		{
			assertItemNotLocked(tableName, key, null, shouldExist);
		}

		protected internal virtual void assertTransactionDeleted(Transaction t)
		{
			try
			{
				manager.resumeTransaction(t.Id);
				fail();
			}
			catch (TransactionNotFoundException e)
			{
				assertTrue(e.Message.contains("Transaction not found"));
			}
		}

		protected internal virtual void assertNoSpecialAttributes(IDictionary<string, AttributeValue> item)
		{
			foreach (string attrName in Transaction.SPECIAL_ATTR_NAMES)
			{
				if (item.ContainsKey(attrName))
				{
					fail("Should not have contained attribute " + attrName + " " + item);
				}
			}
		}

		protected internal virtual void assertOldItemImage(string txId, string tableName, IDictionary<string, AttributeValue> key, IDictionary<string, AttributeValue> item, bool shouldExist)
		{
			Transaction t = manager.resumeTransaction(txId);
			IDictionary<string, Dictionary<ImmutableKey, Request>> requests = t.TxItem.RequestMap;
			Request r = requests[tableName][new ImmutableKey(key)];
			IDictionary<string, AttributeValue> image = t.TxItem.loadItemImage(r.Rid);
			if (shouldExist)
			{
				assertNotNull(image);
				image.Remove(Transaction.AttributeName.TXID.ToString());
				image.Remove(Transaction.AttributeName.IMAGE_ID.ToString());
				image.Remove(Transaction.AttributeName.DATE.ToString());
				assertFalse(image.ContainsKey(Transaction.AttributeName.TRANSIENT.ToString()));
				assertEquals(item, image); // TODO does not work for Set AttributeValue types (DynamoDB does not preserve ordering)
			}
			else
			{
				assertNull(image);
			}
		}

		protected internal virtual IDictionary<string, AttributeValue> getItem(string tableName, IDictionary<string, AttributeValue> key)
		{
			GetItemResult result = dynamodb.getItem(new GetItemRequest()
				.withTableName(tableName).withKey(key).withReturnConsumedCapacity(ReturnConsumedCapacity.TOTAL).withConsistentRead(true));
			return result.Item;
		}

	}
}