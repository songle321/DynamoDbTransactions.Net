using System.Collections.Generic;
using Amazon.DynamoDBv2.Model;

namespace com.amazonaws.services.dynamodbv2.transactions
{
    public class Arrays
    {
        public static List<T> AsList<T>(params T[] keySchemaElement) => new List<T>(keySchemaElement);
    }
}