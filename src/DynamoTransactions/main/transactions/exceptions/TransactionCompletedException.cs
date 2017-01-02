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
/// <summary>
	/// Thrown when a transaction is completed (either committed or rolled back) and it wasn't the expectation of the caller
	/// for this to happen .
	/// </summary>
	public class TransactionCompletedException : TransactionException
	{
private const long SerialVersionUid = -8170993155989412979L;

		public TransactionCompletedException(string txId, string message) : base(txId, message)
		{
		}
	}

 }