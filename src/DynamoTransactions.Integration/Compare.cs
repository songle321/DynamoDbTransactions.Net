using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.Model;

namespace DynamoTransactions.Integration
{
    public static class Compare
    {
        public static bool Equals(AttributeValue value1, AttributeValue value2) =>
            value1.BOOL == value2.BOOL &&
            value1.IsBOOLSet == value2.IsBOOLSet &&
            value1.IsLSet == value2.IsLSet &&
            value1.IsMSet == value2.IsMSet &&
            value1.NULL == value2.NULL &&
            Equals(value1.B, value2.B) &&
            value1.BS.Zip(value2.BS, Equals).All(x => x) &&
            value1.L.Zip(value2.L, Equals).All(x => x) &&
            value1.M.OrderBy(x => x.Key)
                .Zip(value2.M.OrderBy(x => x.Key), (x, y) => x.Key == y.Key && Equals(x.Value, y.Value)).All(x => x) &&
            value1.N == value2.N &&
            value1.NS.SequenceEqual(value2.NS) &&
            value1.S == value2.S &&
            value1.SS.SequenceEqual(value2.SS);

        public static bool Equals(MemoryStream value1,MemoryStream value2) =>
            value1.ToArray().SequenceEqual(value2.ToArray());
    }
}
