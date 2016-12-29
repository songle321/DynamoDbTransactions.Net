using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime.Internal.Transform;

namespace DynamoTransactions
{
    public static class AttributeDefinitionExtensions
    {
        public static AttributeValue withN(this AttributeValue initial, string value) =>
            new AttributeValue
            {
                N = value,
                B = initial.B,
                BS = initial.BS,
                L = initial.L,
                S = initial.S,
                BOOL = initial.BOOL,
                M = initial.M,
                NS = initial.NS,
                NULL = initial.NULL,
                SS = initial.SS
            };

        public static AttributeValue withB(this AttributeValue initial, MemoryStream value) =>
            new AttributeValue
            {
                N = initial.N,
                B = value,
                BS = initial.BS,
                L = initial.L,
                S = initial.S,
                BOOL = initial.BOOL,
                M = initial.M,
                NS = initial.NS,
                NULL = initial.NULL,
                SS = initial.SS
            };

        public static AttributeValue withBS(this AttributeValue initial, List<MemoryStream> value) =>
            new AttributeValue
            {
                N = initial.N,
                B = initial.B,
                BS = value,
                L = initial.L,
                S = initial.S,
                BOOL = initial.BOOL,
                M = initial.M,
                NS = initial.NS,
                NULL = initial.NULL,
                SS = initial.SS
            };

        public static AttributeValue withL(this AttributeValue initial, List<AttributeValue> value) =>
            new AttributeValue
            {
                N = initial.N,
                B = initial.B,
                BS = initial.BS,
                L = value,
                S = initial.S,
                BOOL = initial.BOOL,
                M = initial.M,
                NS = initial.NS,
                NULL = initial.NULL,
                SS = initial.SS
            };

        public static AttributeValue withS(this AttributeValue initial, string value) =>
            new AttributeValue
            {
                N = initial.N,
                B = initial.B,
                BS = initial.BS,
                L = initial.L,
                S = value,
                BOOL = initial.BOOL,
                M = initial.M,
                NS = initial.NS,
                NULL = initial.NULL,
                SS = initial.SS
            };

        public static AttributeValue withBOOL(this AttributeValue initial, bool value) =>
            new AttributeValue
            {
                N = initial.N,
                B = initial.B,
                BS = initial.BS,
                L = initial.L,
                S = initial.S,
                BOOL = value,
                M = initial.M,
                NS = initial.NS,
                NULL = initial.NULL,
                SS = initial.SS
            };

        public static AttributeValue withM(this AttributeValue initial, Dictionary<string, AttributeValue> value) =>
            new AttributeValue
            {
                N = initial.N,
                B = initial.B,
                BS = initial.BS,
                L = initial.L,
                S = initial.S,
                BOOL = initial.BOOL,
                M = value,
                NS = initial.NS,
                NULL = initial.NULL,
                SS = initial.SS
            };

        public static AttributeValue withNS(this AttributeValue initial, List<string> value) =>
            new AttributeValue
            {
                N = initial.N,
                B = initial.B,
                BS = initial.BS,
                L = initial.L,
                S = initial.S,
                BOOL = initial.BOOL,
                M = initial.M,
                NS = value,
                NULL = initial.NULL,
                SS = initial.SS
            };

        public static AttributeValue withNULL(this AttributeValue initial, bool value) =>
            new AttributeValue
            {
                N = initial.N,
                B = initial.B,
                BS = initial.BS,
                L = initial.L,
                S = initial.S,
                BOOL = initial.BOOL,
                M = initial.M,
                NS = initial.NS,
                NULL = value,
                SS = initial.SS
            };

        public static AttributeValue withSS(this AttributeValue initial, List<string> value) =>
            new AttributeValue
            {
                N = initial.N,
                B = initial.B,
                BS = initial.BS,
                L = initial.L,
                S = initial.S,
                BOOL = initial.BOOL,
                M = initial.M,
                NS = initial.NS,
                NULL = initial.NULL,
                SS = value
            };
    }
}
