using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;

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
	/// <summary>
	/// A very primitive fault-injection client.
	/// 
	/// @author dyanacek
	/// </summary>
	public class FailingAmazonDynamoDbClient : AmazonDynamoDBClient
	{

		public class FailedYourRequestException : Exception
		{
			internal const long SerialVersionUid = -7191808024168281212L;
		}

		// Any requests added to this set will throw a FailedYourRequestException when called.
		public readonly ISet<AmazonWebServiceRequest> RequestsToFail = new HashSet<AmazonWebServiceRequest>();

		// Any requests added to this set will return a null item when called
		public readonly ISet<GetItemRequest> GetRequestsToTreatAsDeleted = new HashSet<GetItemRequest>();

		// Any requests with keys in this set will return the queue of responses in order. When the end of the queue is reached
		// further requests will be passed to the DynamoDB client.
		public readonly Dictionary<GetItemRequest, LinkedList<GetItemResponse>> GetRequestsToStub = 
            new Dictionary<GetItemRequest, LinkedList<GetItemResponse>>();

		/// <summary>
		/// Resets the client to the stock DynamoDB client (all requests will call DynamoDB)
		/// </summary>
		public virtual void Reset()
		{
			RequestsToFail.Clear();
			GetRequestsToTreatAsDeleted.Clear();
			GetRequestsToStub.Clear();
		}

		public FailingAmazonDynamoDbClient(AWSCredentials credentials, AmazonDynamoDBConfig clientConfig) : base(credentials, clientConfig)
		{
		}

		public new async Task<GetItemResponse> GetItemAsync(GetItemRequest getItemRequest, CancellationToken cancellationToken)
		{
			if (RequestsToFail.Contains(getItemRequest))
			{
				throw new FailedYourRequestException();
			}
			if (GetRequestsToTreatAsDeleted.Contains(getItemRequest))
			{
				return new GetItemResponse();
			}
			LinkedList<GetItemResponse> stubbedResults = GetRequestsToStub[getItemRequest];
			if (stubbedResults != null && stubbedResults.Count > 0)
			{
				//return stubbedResults.RemoveFirst();
				return stubbedResults.First.Value;
			}
			return await base.GetItemAsync(getItemRequest, cancellationToken);
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.UpdateItemResponse updateItemAsync(com.amazonaws.services.dynamodbv2.model.UpdateItemRequest updateItemRequest) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public new async Task<UpdateItemResponse> UpdateItemAsync(UpdateItemRequest updateItemRequest, CancellationToken cancellationToken)
		{
			if (RequestsToFail.Contains(updateItemRequest))
			{
				throw new FailedYourRequestException();
			}
			return await base.UpdateItemAsync(updateItemRequest, cancellationToken);
		}
	}

}