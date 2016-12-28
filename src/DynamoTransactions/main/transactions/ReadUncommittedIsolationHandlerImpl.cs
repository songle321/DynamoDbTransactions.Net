using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.Model;
using static com.amazonaws.services.dynamodbv2.transactions.Transaction;
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
    /// An isolation handler for reading items at the uncommitted
    /// (Transaction.IsolationLevel.UNCOMMITTED) level. It will
    /// only filter out transient items.
    /// </summary>
    public class ReadUncommittedIsolationHandlerImpl : ReadIsolationHandler
    {
        private static readonly Log LOG = LogFactory.getLog(typeof(ReadUncommittedIsolationHandlerImpl));

        /// <summary>
        /// Given an item, return whatever is there. The returned item may contain changes that will later be rolled back.
        /// If the item was inserted only for acquiring a lock (and the item will be gone after the transaction), the returned
        /// item will be null. </summary>
        /// <param name="item"> The item that the client read. </param>
        /// <param name="attributesToGet"> The attributes to get from the table. If null or empty, will
        ///                        fetch all attributes. </param>
        /// <param name="tableName"> the table that contains the item </param>
        /// <returns> the item itself, unless it is transient and not applied. </returns>
        public virtual async Task<Dictionary<string, AttributeValue>> HandleItemAsync(Dictionary<string, AttributeValue> item, List<string> attributesToGet, string tableName, CancellationToken cancellationToken)
        {
            // If the item doesn't exist, it's not locked
            if (item == null)
            {
                return null;
            }

            // If the item is transient, return a null item
            // But if the change is applied, return it even if it was a transient item (deleteAsync and lock do not apply)
            if (isTransient(item) && !isApplied(item))
            {
                return null;
            }
            return item;
        }

    }

}