using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.Model;

// <summary>
// Copyright 2013-2016 Amazon.com, Inc. or its affiliates. All Rights Reserved.
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
	/// An isolation handler takes an item and returns a version
	/// of the item that can be read at the implemented isolaction
	/// level.
	/// </summary>
	public interface ReadIsolationHandler
	{

		/// <summary>
		/// Returns a version of the item can be read at the isolation level implemented by
		/// the handler. This is possibly null if the item is transient. It might not be latest
		/// version if the isolation level is committed. </summary>
		/// <param name="item"> The item to check </param>
		/// <param name="attributesToGet"> The attributes to get from the table. If null or empty, will
		///                        fetch all attributes. </param>
		/// <param name="tableName"> The table that contains the item </param>
		/// <returns> A version of the item that can be read at the isolation level. </returns>
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
//ORIGINAL LINE: public java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> HandleItemAsync(final java.util.Map<String, com.amazonaws.services.dynamodbv2.model.AttributeValue> item, final java.util.List<String> attributesToGet, final String tableName);
		Task<Dictionary<string, AttributeValue>> HandleItemAsync(Dictionary<string, AttributeValue> item, List<string> attributesToGet, string tableName, CancellationToken cancellationToken);

	}

}