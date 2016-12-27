using System;

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

	public class TransactionException : Exception
	{

		private const long serialVersionUID = -3886636775903901771L;

		private readonly string txId;

		public TransactionException(string txId, string message) : base(txId + " - " + message)
		{
			this.txId = txId;
		}

		public TransactionException(string txId, string message, Exception t) : base(txId + " - " + message, t)
		{
			this.txId = txId;
		}

		public TransactionException(string txId, Exception t) : base(txId + " - " + ((t != null) ? t.Message : ""), t)
		{
			this.txId = txId;
		}

		public virtual string TxId
		{
			get
			{
				return txId;
			}
		}
	}

 }