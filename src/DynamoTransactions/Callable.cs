using System;
using System.Threading.Tasks;

namespace com.amazonaws.services.dynamodbv2.transactions
{
    internal abstract class Callable<T>
    {
        public abstract Task<T> callAsync();
    }
}