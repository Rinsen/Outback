using System;
using System.Security.Cryptography;
using Xunit;

namespace Rinsen.Outback.Tests
{
    /// <summary>
    /// SEC-012: SecretComparer must compare secrets in constant time (via
    /// CryptographicOperations.FixedTimeEquals), be length-checked, and fail closed
    /// on null inputs. Correct and incorrect secrets both go through the same
    /// full-length comparison path.
    /// </summary>
    public class SecretComparerTests
    {
        [Theory]
        [InlineData("client-secret-123", "client-secret-123")]
        [InlineData("", "")]
        [InlineData("a", "a")]
        public void String_Equals_ReturnsTrue(string left, string right)
        {
            Assert.True(Rinsen.Outback.Helpers.SecretComparer.FixedTimeEquals(left, right));
        }

        [Theory]
        [InlineData("client-secret-123", "client-secret-124")]
        [InlineData("client-secret-123", "client-secret-12x")]
        [InlineData("password", "Password")]
        public void String_NotEqual_ReturnsFalse(string left, string right)
        {
            Assert.False(Rinsen.Outback.Helpers.SecretComparer.FixedTimeEquals(left, right));
        }

        [Theory]
        [InlineData("client-secret-123", "client-secret-12")]
        [InlineData("abc", "abcd")]
        public void String_DifferentLength_ReturnsFalse(string left, string right)
        {
            Assert.False(Rinsen.Outback.Helpers.SecretComparer.FixedTimeEquals(left, right));
        }

        [Fact]
        public void String_Null_ReturnsFalse()
        {
            Assert.False(Rinsen.Outback.Helpers.SecretComparer.FixedTimeEquals((string)null, "secret"));
            Assert.False(Rinsen.Outback.Helpers.SecretComparer.FixedTimeEquals("secret", (string)null));
            Assert.False(Rinsen.Outback.Helpers.SecretComparer.FixedTimeEquals((string)null, (string)null));
        }

        [Fact]
        public void ByteArray_Equals_ReturnsTrue()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            var copy = (byte[])bytes.Clone();

            Assert.True(Rinsen.Outback.Helpers.SecretComparer.FixedTimeEquals(bytes, copy));
        }

        [Fact]
        public void ByteArray_NotEqual_ReturnsFalse()
        {
            var left = RandomNumberGenerator.GetBytes(32);
            var right = (byte[])left.Clone();
            right[0] ^= 0x01; // Flip a single bit.

            Assert.False(Rinsen.Outback.Helpers.SecretComparer.FixedTimeEquals(left, right));
        }

        [Fact]
        public void ByteArray_DifferentLength_ReturnsFalse()
        {
            var left = RandomNumberGenerator.GetBytes(32);
            var right = RandomNumberGenerator.GetBytes(16);

            Assert.False(Rinsen.Outback.Helpers.SecretComparer.FixedTimeEquals(left, right));
        }

        [Fact]
        public void ByteArray_Null_ReturnsFalse()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);

            Assert.False(Rinsen.Outback.Helpers.SecretComparer.FixedTimeEquals((byte[])null, bytes));
            Assert.False(Rinsen.Outback.Helpers.SecretComparer.FixedTimeEquals(bytes, (byte[])null));
            Assert.False(Rinsen.Outback.Helpers.SecretComparer.FixedTimeEquals((byte[])null, (byte[])null));
        }
    }
}
