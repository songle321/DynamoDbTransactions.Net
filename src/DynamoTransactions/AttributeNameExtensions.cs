using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.Model;

namespace DynamoTransactions
{
    public static class AttributeNameExtensions
    {
        public static AttributeDefinition WithAttributeName(this AttributeDefinition initial, string value) =>
            new AttributeDefinition
            {
                AttributeName = value,
                AttributeType = initial.AttributeType
            };

        public static AttributeDefinition WithAttributeType(this AttributeDefinition initial, string value) =>
            new AttributeDefinition
            {
                AttributeName = initial.AttributeType,
                AttributeType = value
            };


    }
}
