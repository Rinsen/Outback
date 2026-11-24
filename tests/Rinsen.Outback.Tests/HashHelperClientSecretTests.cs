using Rinsen.Outback.Helpers;
using Xunit;

namespace Rinsen.Outback.Tests
{
    /// <summary>
    /// SEC-013: client secrets must be stored with a salted KDF (PBKDF2-HMAC-SHA256
    /// at the OWASP minimum iteration count), and verification must succeed against
    /// both the new salted format and the legacy unsalted SHA-256 format.
    /// </summary>
    public class HashHelperClientSecretTests
    {
        private const string Secret = "correct-horse-battery-staple";
        private const string WrongSecret = "wrong-secret-value-entirely";

        [Fact]
        public void HashClientSecret_ProducesSaltedPbkdf2Format()
        {
            var stored = HashHelper.HashClientSecret(Secret);

            var parts = stored.Split('$');
            Assert.Equal(4, parts.Length);
            Assert.Equal("PBKDF2-SHA256", parts[0]);
            Assert.Equal("600000", parts[1]);
            Assert.False(string.IsNullOrEmpty(parts[2]), "salt must be present");
            Assert.False(string.IsNullOrEmpty(parts[3]), "hash must be present");
        }

        [Fact]
        public void HashClientSecret_SameSecretTwice_ProducesDifferentStoredValues()
        {
            Assert.NotEqual(HashHelper.HashClientSecret(Secret), HashHelper.HashClientSecret(Secret));
        }

        [Fact]
        public void VerifyClientSecret_SaltedFormat_CorrectSecret_ReturnsTrue()
        {
            var stored = HashHelper.HashClientSecret(Secret);

            Assert.True(HashHelper.VerifyClientSecret(Secret, stored));
        }

        [Fact]
        public void VerifyClientSecret_SaltedFormat_WrongSecret_ReturnsFalse()
        {
            var stored = HashHelper.HashClientSecret(Secret);

            Assert.False(HashHelper.VerifyClientSecret(WrongSecret, stored));
        }

        [Fact]
        public void VerifyClientSecret_LegacySha256_CorrectSecret_ReturnsTrue()
        {
            var stored = HashHelper.GetSha256Hash(Secret);

            Assert.True(HashHelper.VerifyClientSecret(Secret, stored));
        }

        [Fact]
        public void VerifyClientSecret_LegacySha256_WrongSecret_ReturnsFalse()
        {
            var stored = HashHelper.GetSha256Hash(Secret);

            Assert.False(HashHelper.VerifyClientSecret(WrongSecret, stored));
        }

        [Fact]
        public void IsLegacyClientSecret_LegacyValue_ReturnsTrue()
        {
            Assert.True(HashHelper.IsLegacyClientSecret(HashHelper.GetSha256Hash(Secret)));
        }

        [Fact]
        public void IsLegacyClientSecret_SaltedValue_ReturnsFalse()
        {
            Assert.False(HashHelper.IsLegacyClientSecret(HashHelper.HashClientSecret(Secret)));
        }

        [Theory]
        [InlineData("PBKDF2-SHA256$600000$")]
        [InlineData("PBKDF2-SHA256$600000$abc$")]
        [InlineData("PBKDF2-SHA256$600000$abc$def$extra")]
        [InlineData("PBKDF2-SHA256$notanumber$abc$def")]
        [InlineData("PBKDF2-SHA256$0$abc$def")]
        [InlineData("PBKDF2-SHA256$600000$!!not-base64!!$def")]
        public void VerifyClientSecret_MalformedSaltedValue_FailsClosed(string stored)
        {
            Assert.False(HashHelper.VerifyClientSecret(Secret, stored));
        }

        [Fact]
        public void VerifyClientSecret_NullInputs_ReturnsFalse()
        {
            Assert.False(HashHelper.VerifyClientSecret(null, HashHelper.HashClientSecret(Secret)));
            Assert.False(HashHelper.VerifyClientSecret(Secret, null));
            Assert.False(HashHelper.VerifyClientSecret(null, null));
        }
    }
}
