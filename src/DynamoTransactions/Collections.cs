using System.Collections.Generic;
using Amazon.DynamoDBv2.Model;

namespace com.amazonaws.services.dynamodbv2.transactions
{
    internal class Collections
    {
        public static Dictionary<TKey, TValue> singletonMap<TKey, TValue>(TKey id,
                TValue attributeValue)
            => new Dictionary<TKey, TValue> {{id, attributeValue}};
    }
}