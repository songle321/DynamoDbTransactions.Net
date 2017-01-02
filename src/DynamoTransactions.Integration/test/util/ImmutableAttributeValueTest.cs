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

using System.Collections.Generic;
using System.IO;
using Amazon.DynamoDBv2.Model;
using FluentAssertions;

namespace com.amazonaws.services.dynamodbv2.util
{
	public class ImmutableAttributeValueTest
	{
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testBSEquals()
		public virtual void TestBsEquals()
		{
			byte[] b1 = {0x01};
			byte[] b2 = {0x01};
			AttributeValue av1 = new AttributeValue {
                BS = {new MemoryStream(b1)}
            };
			AttributeValue av2 = new AttributeValue {
                BS = {new MemoryStream(b2)}
            };
			ImmutableAttributeValue iav1 = new ImmutableAttributeValue(av1);
			ImmutableAttributeValue iav2 = new ImmutableAttributeValue(av2);
			iav1.Should().Be(iav2);
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testBSNotEq()
		public virtual void TestBsNotEq()
        {
            byte[] b1 = { 0x01 };
            byte[] b2 = { 0x01 };
            AttributeValue av1 = new AttributeValue
            {
                BS = { new MemoryStream(b1) }
            };
            AttributeValue av2 = new AttributeValue
            {
                BS = { new MemoryStream(b2) }
            };
            ImmutableAttributeValue iav1 = new ImmutableAttributeValue(av1);
			ImmutableAttributeValue iav2 = new ImmutableAttributeValue(av2);
            iav1.Should().NotBe(iav2);
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testBSWithNull()
		public virtual void TestBsWithNull()
		{
			byte[] b1 = {0x01};
			byte[] b2 = {0x01};
            AttributeValue av1 = new AttributeValue
            {
                BS = { new MemoryStream(b1), new MemoryStream(b1) }
            };
            AttributeValue av2 = new AttributeValue
            {
                BS = { new MemoryStream(b2), new MemoryStream(b2) }
            };
			ImmutableAttributeValue iav1 = new ImmutableAttributeValue(av1);
			ImmutableAttributeValue iav2 = new ImmutableAttributeValue(av2);
			iav1.Should().NotBe(iav2);
		}

	}
}