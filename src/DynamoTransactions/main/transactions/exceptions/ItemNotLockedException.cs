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
	/// <summary>
	/// Indicates that the transaction could not get the lock because it is owned by another transaction.
	/// </summary>
	public class ItemNotLockedException : TransactionException
	{
private const long SerialVersionUid = -2992047273290608776L;

		private readonly string _txId;
		private readonly string _lockOwnerTxId;
		private readonly string _tableName;
		private readonly Dictionary<string, AttributeValue> _item;

		public ItemNotLockedException(string txId, string lockTxId, string tableName, Dictionary<string, AttributeValue> item) : this(txId, lockTxId, tableName, item, null)
		{
		}

		public ItemNotLockedException(string txId, string lockOwnerTxId, string tableName, Dictionary<string, AttributeValue> item, Exception t) : base(txId, "Item is not locked by our transaction, is locked by " + lockOwnerTxId + " for table " + tableName + ", item: " + item)
		{
			this._txId = txId;
			this._lockOwnerTxId = lockOwnerTxId;
			this._tableName = tableName;
			this._item = item;
		}

		public override string TxId
		{
			get
			{
				return _txId;
			}
		}

		public virtual string LockOwnerTxId
		{
			get
			{
				return _lockOwnerTxId;
			}
		}

		public virtual Dictionary<string, AttributeValue> Item
		{
			get
			{
				return _item;
			}
		}

		public virtual string TableName
		{
			get
			{
				return _tableName;
			}
		}

	}

 }