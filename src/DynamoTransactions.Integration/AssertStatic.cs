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
        public static void assertTrue(bool value)
        {
            value.Should().BeTrue();
        }

        public static void assertTrue(string message, bool value)
        {
            value.Should().BeTrue(message);
        }

        public static void assertNull(string message, object value)
        {
            value.Should().BeNull(message);
        }

        public static void assertNull(object value)
        {
            value.Should().BeNull();
        }

        public static void assertNotNull(string message, object value)
        {
            value.Should().NotBeNull(message);
        }

        public static void assertNotNull(object value)
        {
            value.Should().NotBeNull();
        }

        public static void assertEquals(string message, object value1, object value2)
        {
            value1.ShouldBeEquivalentTo(value2, message);
        }

        public static void assertEquals(object value1, object value2)
        {
            value1.ShouldBeEquivalentTo(value2);
        }

        public static void assertFalse(string message, bool value)
        {
            value.Should().BeFalse(message);
        }

        public static void assertFalse(bool value)
        {
            value.Should().BeFalse();
        }

        public static void fail(string message = null)
        {
            throw new AssertionFailedException(message);
        }
    }
}
