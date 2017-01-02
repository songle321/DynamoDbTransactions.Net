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
private readonly string _n;
		private readonly string _s;
		private readonly sbyte[] _b;
		private readonly List<string> _ns;
		private readonly List<string> _ss;
		private readonly List<sbyte[]> _bs;

		public ImmutableAttributeValue(AttributeValue av)
		{
			_s = av.S;
			_n = av.N;
			_b = (sbyte[]) av.B?.ToArray().Clone();
			_ns = av.NS != null ? new List<string>(av.NS) : null;
			_ss = av.SS != null ? new List<string>(av.SS) : null;
			_bs = av.BS != null ? new List<sbyte[]>(av.BS.Count) : null;

			if (av.BS != null)
			{
				foreach (var buf in av.BS)
				{
					if (buf != null)
					{
						_bs.Add((sbyte[]) buf.ToArray().Clone());
					}
					else
					{
						_bs.Add(null);
					}
				}
			}
		}

		public override int GetHashCode()
		{
			const int prime = 31;
			int result = 1;
			result = prime * result + _b.GetHashCode();
			result = prime * result + (_bs?.GetHashCode() ?? 0);
			result = prime * result + ((string.ReferenceEquals(_n, null)) ? 0 : _n.GetHashCode());
			result = prime * result + (_ns?.GetHashCode() ?? 0);
			result = prime * result + ((string.ReferenceEquals(_s, null)) ? 0 : _s.GetHashCode());
			result = prime * result + (_ss?.GetHashCode() ?? 0);
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
			if (!_b.SequenceEqual(other._b))
			{
				return false;
			}
			if (_bs == null)
			{
				if (other._bs != null)
				{
					return false;
				}
			}
//JAVA TO C# CONVERTER WARNING: LINQ 'SequenceEqual' is not always identical to Java AbstractList 'equals':
//ORIGINAL LINE: else if (!bs.equals(other.bs))
			else if (!_bs.Zip(other._bs, (x, y) => x.SequenceEqual(y)).All(x => x))
			{
				// Note: this else if block is not auto-generated
				if (other._bs == null)
				{
					return false;
				}
				if (_bs.Count != other._bs.Count)
				{
					return false;
				}
				for (int i = 0; i < _bs.Count; i++)
				{
					if (_bs[i].SequenceEqual(other._bs[i]))
					{
						return false;
					}
				}
				return true;
			}
			if (string.ReferenceEquals(_n, null))
			{
				if (!string.ReferenceEquals(other._n, null))
				{
					return false;
				}
			}
			else if (!_n.Equals(other._n))
			{
				return false;
			}
			if (_ns == null)
			{
				if (other._ns != null)
				{
					return false;
				}
			}
//JAVA TO C# CONVERTER WARNING: LINQ 'SequenceEqual' is not always identical to Java AbstractList 'equals':
//ORIGINAL LINE: else if (!ns.equals(other.ns))
			else if (!_ns.SequenceEqual(other._ns))
			{
				return false;
			}
			if (string.ReferenceEquals(_s, null))
			{
				if (!string.ReferenceEquals(other._s, null))
				{
					return false;
				}
			}
			else if (!_s.Equals(other._s))
			{
				return false;
			}
			if (_ss == null)
			{
				if (other._ss != null)
				{
					return false;
				}
			}
//JAVA TO C# CONVERTER WARNING: LINQ 'SequenceEqual' is not always identical to Java AbstractList 'equals':
//ORIGINAL LINE: else if (!ss.equals(other.ss))
			else if (!_ss.SequenceEqual(other._ss))
			{
				return false;
			}
			return true;
		}

	}

 }