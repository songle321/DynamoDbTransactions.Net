using System.Collections.Generic;
using Amazon.DynamoDBv2.Model;

namespace com.amazonaws.services.dynamodbv2.transactions
{
    internal class Arrays
    {
        public static List<T> asList<T>(params T[] keySchemaElement) => new List<T>(keySchemaElement);
    }
}