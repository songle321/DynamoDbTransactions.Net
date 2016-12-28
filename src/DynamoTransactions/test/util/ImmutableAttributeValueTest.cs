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
//JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to C#:
//	import static org.junit.Assert.assertEquals;
//JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to C#:
//	import static org.junit.Assert.assertFalse;


	using Test = org.junit.Test;

	using AttributeValue = com.amazonaws.services.dynamodbv2.model.AttributeValue;

	public class ImmutableAttributeValueTest
	{
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testBSEquals()
		public virtual void testBSEquals()
		{
			sbyte[] b1 = new sbyte[] {(sbyte)0x01};
			sbyte[] b2 = new sbyte[] {(sbyte)0x01};
			AttributeValue av1 = new AttributeValue {
.withBS(ByteBuffer.wrap(b1))
};
			AttributeValue av2 = new AttributeValue {
.withBS(ByteBuffer.wrap(b2))
};
			ImmutableAttributeValue iav1 = new ImmutableAttributeValue(av1);
			ImmutableAttributeValue iav2 = new ImmutableAttributeValue(av2);
			assertEquals(iav1, iav2);
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testBSNotEq()
		public virtual void testBSNotEq()
		{
			sbyte[] b1 = new sbyte[] {(sbyte)0x01};
			sbyte[] b2 = new sbyte[] {(sbyte)0x02};
			AttributeValue av1 = new AttributeValue {
.withBS(ByteBuffer.wrap(b1))
};
			AttributeValue av2 = new AttributeValue {
.withBS(ByteBuffer.wrap(b2))
};
			ImmutableAttributeValue iav1 = new ImmutableAttributeValue(av1);
			ImmutableAttributeValue iav2 = new ImmutableAttributeValue(av2);
			assertFalse(iav1.Equals(iav2));
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testBSWithNull()
		public virtual void testBSWithNull()
		{
			sbyte[] b1 = new sbyte[] {(sbyte)0x01};
			sbyte[] b2 = new sbyte[] {(sbyte)0x01};
			AttributeValue av1 = (new AttributeValue()).withBS(ByteBuffer.wrap(b1), ByteBuffer.wrap(b1));
			AttributeValue av2 = (new AttributeValue()).withBS(ByteBuffer.wrap(b2), null);
			ImmutableAttributeValue iav1 = new ImmutableAttributeValue(av1);
			ImmutableAttributeValue iav2 = new ImmutableAttributeValue(av2);
			assertFalse(iav1.Equals(iav2));
		}

	}
}