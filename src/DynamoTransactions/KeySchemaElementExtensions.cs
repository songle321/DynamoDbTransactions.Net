using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.Model;

namespace DynamoTransactions
{
    public static class KeySchemaElementExtensions
    {
        public static KeySchemaElement WithAttributeName(this KeySchemaElement initial, string value) =>
            new KeySchemaElement
            {
                AttributeName = value,
                KeyType = initial.KeyType
            };

        public static KeySchemaElement WithKeyType(this KeySchemaElement initial, string value) =>
            new KeySchemaElement
            {
                AttributeName = initial.AttributeName,
                KeyType = value
            };


    }
}
