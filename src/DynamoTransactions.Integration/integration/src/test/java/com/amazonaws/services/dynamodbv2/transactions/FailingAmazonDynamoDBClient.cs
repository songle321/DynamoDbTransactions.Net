﻿using System;
using System.Collections.Generic;

/// <summary>
/// Copyright 2013-2013 Amazon.com, Inc. or its affiliates. All Rights Reserved.
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
	using GetItemRequest = com.amazonaws.services.dynamodbv2.model.GetItemRequest;
	using GetItemResponse = com.amazonaws.services.dynamodbv2.model.GetItemResult;
	using UpdateItemRequest = com.amazonaws.services.dynamodbv2.model.UpdateItemRequest;
	using UpdateItemResponse = com.amazonaws.services.dynamodbv2.model.UpdateItemResult;

	/// <summary>
	/// A very primitive fault-injection client.
	/// 
	/// @author dyanacek
	/// </summary>
	public class FailingAmazonDynamoDBClient : AmazonDynamoDBClient
	{

		public class FailedYourRequestException : Exception
		{
			internal const long serialVersionUID = -7191808024168281212L;
		}

		// Any requests added to this set will throw a FailedYourRequestException when called.
		public readonly ISet<AmazonWebServiceRequest> requestsToFail = new HashSet<AmazonWebServiceRequest>();

		// Any requests added to this set will return a null item when called
		public readonly ISet<GetItemRequest> getRequestsToTreatAsDeleted = new HashSet<GetItemRequest>();

		// Any requests with keys in this set will return the queue of responses in order. When the end of the queue is reached
		// further requests will be passed to the DynamoDB client.
		public readonly IDictionary<GetItemRequest, LinkedList<GetItemResult>> getRequestsToStub = new Dictionary<GetItemRequest, LinkedList<GetItemResult>>();

		/// <summary>
		/// Resets the client to the stock DynamoDB client (all requests will call DynamoDB)
		/// </summary>
		public virtual void reset()
		{
			requestsToFail.Clear();
			getRequestsToTreatAsDeleted.Clear();
			getRequestsToStub.Clear();
		}

		public FailingAmazonDynamoDBClient(AWSCredentials credentials) : base(credentials)
		{
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.GetItemResponse getItem(com.amazonaws.services.dynamodbv2.model.GetItemRequest getItemRequest) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override GetItemResponse getItem(GetItemRequest getItemRequest)
		{
			if (requestsToFail.Contains(getItemRequest))
			{
				throw new FailedYourRequestException();
			}
			if (getRequestsToTreatAsDeleted.Contains(getItemRequest))
			{
				return new GetItemResult();
			}
			LinkedList<GetItemResult> stubbedResults = getRequestsToStub[getItemRequest];
			if (stubbedResults != null && stubbedResults.Count > 0)
			{
				return stubbedResults.RemoveFirst();
			}
			return base.getItem(getItemRequest);
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public com.amazonaws.services.dynamodbv2.model.UpdateItemResponse updateItem(com.amazonaws.services.dynamodbv2.model.UpdateItemRequest updateItemRequest) throws com.amazonaws.AmazonServiceException, com.amazonaws.AmazonClientException
		public override UpdateItemResponse updateItem(UpdateItemRequest updateItemRequest)
		{
			if (requestsToFail.Contains(updateItemRequest))
			{
				throw new FailedYourRequestException();
			}
			return base.updateItem(updateItemRequest);
		}
	}

}