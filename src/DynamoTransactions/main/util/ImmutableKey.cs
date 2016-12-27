using System.Collections.Generic;
using System.Collections.ObjectModel;
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
 namespace com.amazonaws.services.dynamodbv2.util
 {
	/// <summary>
	/// An immutable, write-only key map for storing DynamoDB a primary key value as a map key in other maps.  
	/// </summary>
	public class ImmutableKey
	{

		private readonly IReadOnlyDictionary<string, ImmutableAttributeValue> key;

		public ImmutableKey(Dictionary<string, AttributeValue> mutableKey)
		{
			if (mutableKey == null)
			{
				this.key = null;
			}
			else
			{
				Dictionary<string, ImmutableAttributeValue> keyBuilder = new Dictionary<string, ImmutableAttributeValue>(mutableKey.Count);
				foreach (KeyValuePair<string, AttributeValue> e in mutableKey.SetOfKeyValuePairs())
				{
					keyBuilder[e.Key] = new ImmutableAttributeValue(e.Value);
				}
				this.key = new ReadOnlyDictionary<string, ImmutableAttributeValue>(keyBuilder);
			}
		}

		public override int GetHashCode()
		{
			const int prime = 31;
			int result = 1;
			result = prime * result + ((key == null) ? 0 : key.GetHashCode());
			return result;
		}

		public override bool Equals(object obj)
		{
			if (this == obj)
			{
				return true;
			}
			if (obj == null)
			{
				return false;
			}
			if (this.GetType() != obj.GetType())
			{
				return false;
			}
			ImmutableKey other = (ImmutableKey) obj;
			if (key == null)
			{
				if (other.key != null)
				{
					return false;
				}
			}
			else if (!key.Equals(other.key))
			{
				return false;
			}
			return true;
		}
	}

 }