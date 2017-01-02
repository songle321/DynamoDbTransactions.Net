using System;
using Amazon.Runtime.Internal.Util;
using Microsoft.Extensions.Logging;

namespace com.amazonaws.services.dynamodbv2.transactions
{
    internal class Log
    {
        public Log(Microsoft.Extensions.Logging.ILogger logger)
        {
            Logger = logger;
        }

        private Microsoft.Extensions.Logging.ILogger Logger { get; set; }

        public void Debug(string s)
        {
            Logger.LogDebug(s);
        }

        public bool DebugEnabled { get; set; }
        public bool WarnEnabled { get; set; }

        internal void Debug(string v, Exception e2)
        {
            throw new NotImplementedException();
        }

        public void Info(string s)
        {
            Logger.LogInformation(s);
            throw new NotImplementedException();
        }

        public void Warn(string s)
        {
            Logger.LogWarning(s);
        }
    }
}