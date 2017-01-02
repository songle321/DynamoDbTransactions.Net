using System;
using Microsoft.Extensions.Logging;

namespace com.amazonaws.services.dynamodbv2.transactions
{
    internal class LogFactory
    {
        private static ILoggerFactory Factory { get; } = new LoggerFactory();

        public static Log GetLog(Type type)
        {
            return new Log(LogFactory.Factory.CreateLogger(type));
        }
    }
}