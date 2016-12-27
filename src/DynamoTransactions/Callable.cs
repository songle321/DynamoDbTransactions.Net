using System;

namespace com.amazonaws.services.dynamodbv2.transactions
{
    internal class Callable<T>
    {
        internal T call<T>()
        {
            throw new NotImplementedException();
        }
    }
}