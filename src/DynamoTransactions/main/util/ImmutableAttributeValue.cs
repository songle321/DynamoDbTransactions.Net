using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.DynamoDBv2.Model;

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
 namespace com.amazonaws.services.dynamodbv2.util
 {
	/// <summary>
	/// An immutable class that can be used in map keys.  Does a copy of the attribute value
	/// to prevent any member from being mutated.
	/// </summary>
	public class ImmutableAttributeValue
	{
private readonly string n;
		private readonly string s;
		private readonly sbyte[] b;
		private readonly List<string> ns;
		private readonly List<string> ss;
		private readonly List<sbyte[]> bs;

		public ImmutableAttributeValue(AttributeValue av)
		{
			s = av.S;
			n = av.N;
			b = (sbyte[]) av.B?.ToArray().Clone();
			ns = av.NS != null ? new List<string>(av.NS) : null;
			ss = av.SS != null ? new List<string>(av.SS) : null;
			bs = av.BS != null ? new List<sbyte[]>(av.BS.Count) : null;

			if (av.BS != null)
			{
				foreach (var buf in av.BS)
				{
					if (buf != null)
					{
						bs.Add((sbyte[]) buf.ToArray().Clone());
					}
					else
					{
						bs.Add(null);
					}
				}
			}
		}

		public override int GetHashCode()
		{
			const int prime = 31;
			int result = 1;
			result = prime * result + b.GetHashCode();
			result = prime * result + (bs?.GetHashCode() ?? 0);
			result = prime * result + ((string.ReferenceEquals(n, null)) ? 0 : n.GetHashCode());
			result = prime * result + (ns?.GetHashCode() ?? 0);
			result = prime * result + ((string.ReferenceEquals(s, null)) ? 0 : s.GetHashCode());
			result = prime * result + (ss?.GetHashCode() ?? 0);
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
			ImmutableAttributeValue other = (ImmutableAttributeValue) obj;
			if (!b.SequenceEqual(other.b))
			{
				return false;
			}
			if (bs == null)
			{
				if (other.bs != null)
				{
					return false;
				}
			}
//JAVA TO C# CONVERTER WARNING: LINQ 'SequenceEqual' is not always identical to Java AbstractList 'equals':
//ORIGINAL LINE: else if (!bs.equals(other.bs))
			else if (!bs.Zip(other.bs, (x, y) => x.SequenceEqual(y)).All(x => x))
			{
				// Note: this else if block is not auto-generated
				if (other.bs == null)
				{
					return false;
				}
				if (bs.Count != other.bs.Count)
				{
					return false;
				}
				for (int i = 0; i < bs.Count; i++)
				{
					if (bs[i].SequenceEqual(other.bs[i]))
					{
						return false;
					}
				}
				return true;
			}
			if (string.ReferenceEquals(n, null))
			{
				if (!string.ReferenceEquals(other.n, null))
				{
					return false;
				}
			}
			else if (!n.Equals(other.n))
			{
				return false;
			}
			if (ns == null)
			{
				if (other.ns != null)
				{
					return false;
				}
			}
//JAVA TO C# CONVERTER WARNING: LINQ 'SequenceEqual' is not always identical to Java AbstractList 'equals':
//ORIGINAL LINE: else if (!ns.equals(other.ns))
			else if (!ns.SequenceEqual(other.ns))
			{
				return false;
			}
			if (string.ReferenceEquals(s, null))
			{
				if (!string.ReferenceEquals(other.s, null))
				{
					return false;
				}
			}
			else if (!s.Equals(other.s))
			{
				return false;
			}
			if (ss == null)
			{
				if (other.ss != null)
				{
					return false;
				}
			}
//JAVA TO C# CONVERTER WARNING: LINQ 'SequenceEqual' is not always identical to Java AbstractList 'equals':
//ORIGINAL LINE: else if (!ss.equals(other.ss))
			else if (!ss.SequenceEqual(other.ss))
			{
				return false;
			}
			return true;
		}

	}

 }