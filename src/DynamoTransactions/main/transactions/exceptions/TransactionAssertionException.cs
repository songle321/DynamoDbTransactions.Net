using System.Text;

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
public class TransactionAssertionException : TransactionException
	{
private const long SerialVersionUid = -894664265849460781L;

		public TransactionAssertionException(string txId, string message) : base(txId, message)
		{
		}

		/// <summary>
		/// Throws an assertion exception with a message constructed from to toString() of each data pair, if the assertion is false. </summary>
		/// <param name="assertion"> </param>
		/// <param name="txId"> </param>
		/// <param name="message"> </param>
		/// <param name="data"> </param>
		public static void TxAssert(bool assertion, string txId, string message, params object[] data)
		{
			if (!assertion)
			{
				if (data != null)
				{
					StringBuilder sb = new StringBuilder();
					foreach (object d in data)
					{
						sb.Append(d);
						sb.Append(", ");
					}
					message = message + " - " + sb.ToString();
				}

				throw new TransactionAssertionException(txId, message);
			}
		}

		public static void TxFail(string txId, string message, params object[] data)
		{
			TxAssert(false, txId, message, data);
		}
	}

 }