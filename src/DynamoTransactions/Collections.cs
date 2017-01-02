using System.Collections.Generic;
using Amazon.DynamoDBv2.Model;

namespace com.amazonaws.services.dynamodbv2.transactions
{
    internal class Collections
    {
        public static Dictionary<TKey, TValue> SingletonMap<TKey, TValue>(TKey id,
                TValue attributeValue)
            => new Dictionary<TKey, TValue> { { id, attributeValue } };
    
        public static List<TValue> SingletonList<TValue>(TValue attributeValue)
            => new List<TValue> { attributeValue };
    }
}