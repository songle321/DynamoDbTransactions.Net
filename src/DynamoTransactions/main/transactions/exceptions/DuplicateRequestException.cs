﻿// <summary>
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


	public class DuplicateRequestException : TransactionException
	{

		private const long serialVersionUID = 5461061207526371210L;

		public DuplicateRequestException(string txId, string tableName, string key) : base(txId, "Duplicate request for table name " + tableName + " for key " + key)
		{
		}

	}

 }