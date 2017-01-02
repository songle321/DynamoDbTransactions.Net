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
	/// Indicates that the transaction record no longer exists (or never did)
	/// </summary>
	public class TransactionNotFoundException : TransactionException
	{
private const long SerialVersionUid = 1482803351154923519L;

		public TransactionNotFoundException(string txId) : base(txId, "Transaction not found")
		{
		}
	}

 }