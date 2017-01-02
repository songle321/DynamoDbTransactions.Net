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
        public static AttributeValue WithN(this AttributeValue initial, string value) =>
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

        public static AttributeValue WithB(this AttributeValue initial, MemoryStream value) =>
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

        public static AttributeValue WithBs(this AttributeValue initial, List<MemoryStream> value) =>
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

        public static AttributeValue WithL(this AttributeValue initial, List<AttributeValue> value) =>
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

        public static AttributeValue WithS(this AttributeValue initial, string value) =>
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

        public static AttributeValue WithBool(this AttributeValue initial, bool value) =>
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

        public static AttributeValue WithM(this AttributeValue initial, Dictionary<string, AttributeValue> value) =>
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

        public static AttributeValue WithNs(this AttributeValue initial, List<string> value) =>
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

        public static AttributeValue WithNull(this AttributeValue initial, bool value) =>
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

        public static AttributeValue WithSs(this AttributeValue initial, List<string> value) =>
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
