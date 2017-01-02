using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
            value1.ShouldBeEquivalentTo(value2, message);
        }

        public static void AssertEquals(object value1, object value2)
        {
            value1.ShouldBeEquivalentTo(value2);
        }

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
