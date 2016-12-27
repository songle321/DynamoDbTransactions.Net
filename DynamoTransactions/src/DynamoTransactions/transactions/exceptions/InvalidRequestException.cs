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
 namespace com.amazonaws.services.dynamodbv2.transactions.exceptions
 {

	using AttributeValue = com.amazonaws.services.dynamodbv2.model.AttributeValue;

	public class InvalidRequestException : TransactionException
	{

		private const long serialVersionUID = 4622315126910271817L;

		private readonly string tableName;
		private readonly IDictionary<string, AttributeValue> key;
		private readonly Request request;

		public InvalidRequestException(string message, string txId, string tableName, IDictionary<string, AttributeValue> key, Request request) : this(message, txId, tableName, key, request, null)
		{
		}

		public InvalidRequestException(string message, string txId, string tableName, IDictionary<string, AttributeValue> key, Request request, Exception t) : base(((!string.ReferenceEquals(message, null)) ? ": " + message : "Invalid request") + " for transaction " + txId + " table " + tableName + " key " + key, t)
		{
			this.tableName = tableName;
			this.key = key;
			this.request = request;
		}

		public virtual string TableName
		{
			get
			{
				return tableName;
			}
		}

		public virtual IDictionary<string, AttributeValue> Key
		{
			get
			{
				return key;
			}
		}

		public virtual Request Request
		{
			get
			{
				return request;
			}
		}

	}

 }