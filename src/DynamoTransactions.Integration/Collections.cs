using System.Collections.Generic;
using Amazon.DynamoDBv2.Model;

namespace com.amazonaws.services.dynamodbv2.transactions
{
    internal class Collections
    {
        public static Dictionary<string, AttributeValue> singletonMap(string id,
                AttributeValue attributeValue)
            => new Dictionary<string, AttributeValue> {{id, attributeValue}};
    }
}