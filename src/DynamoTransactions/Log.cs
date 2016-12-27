using System;
using com.amazonaws.services.dynamodbv2.transactions.exceptions;

namespace com.amazonaws.services.dynamodbv2.transactions
{
    internal class Log
    {
        public void debug(string s)
        {
            throw new System.NotImplementedException();
        }

        public bool DebugEnabled { get; set; }
        public bool WarnEnabled { get; set; }

        internal void debug(string v, Exception e2)
        {
            throw new NotImplementedException();
        }

        public void info(string s)
        {
            throw new NotImplementedException();
        }

        public void warn(string s)
        {
            throw new NotImplementedException();
        }
    }
}