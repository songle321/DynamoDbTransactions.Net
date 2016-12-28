using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

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
 namespace com.amazonaws.services.dynamodbv2.util
 {
	public class TableHelper
	{
private readonly AmazonDynamoDBClient client;

		public TableHelper(AmazonDynamoDBClient client)
		{
			if (client == null)
			{
				throw new System.ArgumentException("client must not be null");
			}
			this.client = client;
		}

		public virtual async Task<string> verifyTableExistsAsync(string tableName, List<AttributeDefinition> definitions, List<KeySchemaElement> keySchema, List<LocalSecondaryIndex> localIndexes)
		{
			DescribeTableResponse describe = await client.DescribeTableAsync(new DescribeTableRequest(tableName));
			if (!(new HashSet<AttributeDefinition>(definitions)).Equals(new HashSet<AttributeDefinition>(describe.Table.AttributeDefinitions)))
			{
				throw new ResourceInUseException("Table " + tableName + " had the wrong AttributesToGet." + " Expected: " + definitions + " " + " Was: " + describe.Table.AttributeDefinitions);
			}

//JAVA TO C# CONVERTER WARNING: LINQ 'SequenceEqual' is not always identical to Java AbstractList 'equals':
//ORIGINAL LINE: if(! keySchema.equals(describe.getTable().getKeySchema()))
			if (!keySchema.SequenceEqual(describe.Table.KeySchema))
			{
				throw new ResourceInUseException("Table " + tableName + " had the wrong KeySchema." + " Expected: " + keySchema + " " + " Was: " + describe.Table.KeySchema);
			}

			List<LocalSecondaryIndex> theirLSIs = null;
			if (describe.Table.LocalSecondaryIndexes != null)
			{
				theirLSIs = new List<LocalSecondaryIndex>();
				foreach (LocalSecondaryIndexDescription description in describe.Table.LocalSecondaryIndexes)
				{
					LocalSecondaryIndex lsi = new LocalSecondaryIndex
                    {
                        IndexName = description.IndexName,
                        KeySchema = description.KeySchema,
                        Projection = description.Projection
                    };
					theirLSIs.Add(lsi);
				}
			}

			if (localIndexes != null)
			{
				if (!(new HashSet<LocalSecondaryIndex>(localIndexes)).Equals(new HashSet<LocalSecondaryIndex>(theirLSIs)))
				{
					throw new ResourceInUseException("Table " + tableName + " did not have the expected LocalSecondaryIndexes." + " Expected: " + localIndexes + " Was: " + theirLSIs);
				}
			}
			else
			{
				if (theirLSIs != null)
				{
					throw new ResourceInUseException("Table " + tableName + " had local secondary indexes, but expected none." + " Indexes: " + theirLSIs);
				}
			}

			return describe.Table.TableStatus;
		}

		/// <summary>
		/// Verifies that the table exists with the specified schema, and creates it if it does not exist.
		/// </summary>
		/// <param name="tableName"> </param>
		/// <param name="definitions"> </param>
		/// <param name="keySchema"> </param>
		/// <param name="localIndexes"> </param>
		/// <param name="provisionedThroughput"> </param>
		/// <param name="waitTimeSeconds"> </param>
		/// <exception cref="InterruptedException">  </exception>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void verifyOrCreateTableAsync(String tableName, java.util.List<com.amazonaws.services.dynamodbv2.model.AttributeDefinition> definitions, java.util.List<com.amazonaws.services.dynamodbv2.model.KeySchemaElement> keySchema, java.util.List<com.amazonaws.services.dynamodbv2.model.LocalSecondaryIndex> localIndexes, com.amazonaws.services.dynamodbv2.model.ProvisionedThroughput provisionedThroughput, Nullable<long> waitTimeSeconds) throws InterruptedException
		public virtual async Task verifyOrCreateTableAsync(string tableName, List<AttributeDefinition> definitions, List<KeySchemaElement> keySchema, List<LocalSecondaryIndex> localIndexes, ProvisionedThroughput provisionedThroughput, long? waitTimeSeconds)
		{
if (waitTimeSeconds != null && waitTimeSeconds < 0)
			{
				throw new System.ArgumentException("Invalid waitTimeSeconds " + waitTimeSeconds);
			}

			string status = null;
			try
			{
				status = await verifyTableExistsAsync(tableName, definitions, keySchema, localIndexes);
			}
			catch (ResourceNotFoundException)
			{
				status = (await client.CreateTableAsync(new CreateTableRequest
				{
				    TableName = tableName,
                    AttributeDefinitions = definitions,
                    KeySchema = keySchema,
                    LocalSecondaryIndexes = localIndexes,
                    ProvisionedThroughput = provisionedThroughput
				})).TableDescription.TableStatus;
			}

			if (waitTimeSeconds != null && !TableStatus.ACTIVE.ToString().Equals(status))
			{
				await waitForTableActiveAsync(tableName, definitions, keySchema, localIndexes, waitTimeSeconds.Value);
			}
		}

		public virtual async Task waitForTableActiveAsync(string tableName, long waitTimeSeconds)
		{
			if (waitTimeSeconds < 0)
			{
				throw new System.ArgumentException("Invalid waitTimeSeconds " + waitTimeSeconds);
			}

			long startTimeMs = DateTimeHelperClass.CurrentUnixTimeMillis();
			long elapsedMs = 0;
			do
			{
				DescribeTableResponse describe = await client.DescribeTableAsync(new DescribeTableRequest
				{
				    TableName = tableName
				});
				string status = describe.Table.TableStatus;
				if (TableStatus.ACTIVE.ToString().Equals(status))
				{
					return;
				}
				if (TableStatus.DELETING.ToString().Equals(status))
				{
					throw new ResourceInUseException("Table " + tableName + " is " + status + ", and waiting for it to become ACTIVE is not useful.");
				}
				Thread.Sleep(10 * 1000);
				elapsedMs = DateTimeHelperClass.CurrentUnixTimeMillis() - startTimeMs;
			} while (elapsedMs / 1000.0 < waitTimeSeconds);

			throw new ResourceInUseException("Table " + tableName + " did not become ACTIVE after " + waitTimeSeconds + " seconds.");
		}

		public virtual async Task waitForTableActiveAsync(string tableName, List<AttributeDefinition> definitions, List<KeySchemaElement> keySchema, List<LocalSecondaryIndex> localIndexes, long waitTimeSeconds)
		{
if (waitTimeSeconds < 0)
			{
				throw new System.ArgumentException("Invalid waitTimeSeconds " + waitTimeSeconds);
			}

			long startTimeMs = DateTimeHelperClass.CurrentUnixTimeMillis();
			long elapsedMs = 0;
			do
			{
				string status = await verifyTableExistsAsync(tableName, definitions, keySchema, localIndexes);
				if (TableStatus.ACTIVE.ToString().Equals(status))
				{
					return;
				}
				if (TableStatus.DELETING.ToString().Equals(status))
				{
					throw new ResourceInUseException("Table " + tableName + " is " + status + ", and waiting for it to become ACTIVE is not useful.");
				}
				Thread.Sleep(10 * 1000);
				elapsedMs = DateTimeHelperClass.CurrentUnixTimeMillis() - startTimeMs;
			} while (elapsedMs / 1000.0 < waitTimeSeconds);

			throw new ResourceInUseException("Table " + tableName + " did not become ACTIVE after " + waitTimeSeconds + " seconds.");
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void waitForTableDeletedAsync(String tableName, long waitTimeSeconds) throws InterruptedException
		public virtual async Task waitForTableDeletedAsync(string tableName, long waitTimeSeconds)
		{
if (waitTimeSeconds < 0)
			{
				throw new System.ArgumentException("Invalid waitTimeSeconds " + waitTimeSeconds);
			}

			long startTimeMs = DateTimeHelperClass.CurrentUnixTimeMillis();
			long elapsedMs = 0;
			do
			{
				try
				{
					DescribeTableResponse describe = await client.DescribeTableAsync(new DescribeTableRequest
					{
					    TableName = tableName
					});
					string status = describe.Table.TableStatus;
					if (!TableStatus.DELETING.ToString().Equals(status))
					{
						throw new ResourceInUseException("Table " + tableName + " is " + status + ", and waiting for it to not exist is only useful if it is DELETING.");
					}
				}
				catch (ResourceNotFoundException)
				{
					return;
				}
				Thread.Sleep(10 * 1000);
				elapsedMs = DateTimeHelperClass.CurrentUnixTimeMillis() - startTimeMs;
			} while (elapsedMs / 1000.0 < waitTimeSeconds);

			throw new ResourceInUseException("Table " + tableName + " was not deleted after " + waitTimeSeconds + " seconds.");
		}
	}

 }