using FluentAssertions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.Model;
using com.amazonaws.services.dynamodbv2.transactions;
using FluentAssertions.Equivalency;
using FluentAssertions.Execution;

namespace DynamoTransactions.Integration
{
    public static class AssertStatic
    {
        public static void AssertTrue(bool value)
        {
            value.Should().BeTrue();
        }

        public static void AssertTrue(string message, bool value)
        {
            value.Should().BeTrue(message);
        }

        public static void AssertNull(string message, object value)
        {
            value.Should().BeNull(message);
        }

        public static void AssertNull(object value)
        {
            value.Should().BeNull();
        }

        public static void AssertNotNull(string message, object value)
        {
            value.Should().NotBeNull(message);
        }

        public static void AssertNotNull(object value)
        {
            value.Should().NotBeNull();
        }

        public static void AssertEquals(string message, object value1, object value2)
        {
            value1.Should().Be(value2, message);
        }

        public static void AssertEquals(object value1, object value2)
        {
            value1.ShouldBeEquivalentTo(value2);
        }

        public static void AssertEquals(PutItemRequest value1, PutItemRequest value2)
        {
            value1.ShouldBeEquivalentTo(value1, config =>
                config.Excluding((ISubjectInfo si) => si.SelectedMemberInfo.Name == "Item"));

            var zippedItemValues = value1.Item
                .OrderBy(x => x.Key)
                .Zip(value2.Item
                    .OrderBy(x => x.Key),
                    (v1, v2) => new { v1, v2 });
            foreach (var values in zippedItemValues)
            {
                AssertEquals(values.v1.Key, values.v2.Key);
                AssertEquals(values.v1.Value, values.v2.Value);
            }
        }

        public static void AssertEquals(GetItemRequest value1, GetItemRequest value2)
        {
            value1.ShouldBeEquivalentTo(value1, config =>
                config.Excluding((ISubjectInfo si) => si.SelectedMemberInfo.Name == "Item"));

            var zippedItemValues = value1.Key
                .OrderBy(x => x.Key)
                .Zip(value2.Key
                    .OrderBy(x => x.Key),
                    (v1, v2) => new { v1, v2 });
            foreach (var values in zippedItemValues)
            {
                AssertEquals(values.v1.Key, values.v2.Key);
                AssertEquals(values.v1.Value, values.v2.Value);
            }
        }

        public static void AssertEquals(AttributeValue value1, AttributeValue value2)
        {
            value1.ShouldBeEquivalentTo(value2, config =>
                config.Excluding(
                    (ISubjectInfo si) =>
                si.SelectedMemberInfo.Name == "B" || si.SelectedMemberInfo.Name == "BS"
                ));
            AssertEquals(value1.B, value2.B);
            foreach (var values in value1.BS.Zip(value2.BS, (v1, v2) => new { v1, v2 }))
            {
                AssertEquals(values.v1, values.v2);
            };
        }

        public static void AssertEquals(MemoryStream value1, MemoryStream value2) =>
            value1?.ToArray().ShouldBeEquivalentTo(value2?.ToArray());

        public static void AssertFalse(string message, bool value)
        {
            value.Should().BeFalse(message);
        }

        public static void AssertFalse(bool value)
        {
            value.Should().BeFalse();
        }

        public static void Fail(string message = null)
        {
            throw new AssertionFailedException(message);
        }

        public static void AssertArrayEquals<T>(T value1, T value2)
        {
            value1.ShouldBeEquivalentTo(value2);
        }
    }
}
