using System;
using System.Collections.Generic;
using Amazon.DynamoDBv2.Model;

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
 namespace com.amazonaws.services.dynamodbv2.transactions.exceptions
 {
	public class InvalidRequestException : TransactionException
	{
private const long SerialVersionUid = 4622315126910271817L;

		private readonly string _tableName;
		private readonly Dictionary<string, AttributeValue> _key;
		private readonly Request _request;

		public InvalidRequestException(string message, string txId, string tableName, Dictionary<string, AttributeValue> key, Request request) : this(message, txId, tableName, key, request, null)
		{
		}

		public InvalidRequestException(string message, string txId, string tableName, Dictionary<string, AttributeValue> key, Request request, Exception t) : base(((!string.ReferenceEquals(message, null)) ? ": " + message : "Invalid request") + " for transaction " + txId + " table " + tableName + " key " + key, t)
		{
			this._tableName = tableName;
			this._key = key;
			this._request = request;
		}

		public virtual string TableName
		{
			get
			{
				return _tableName;
			}
		}

		public virtual Dictionary<string, AttributeValue> Key
		{
			get
			{
				return _key;
			}
		}

		public virtual Request Request
		{
			get
			{
				return _request;
			}
		}

	}

 }