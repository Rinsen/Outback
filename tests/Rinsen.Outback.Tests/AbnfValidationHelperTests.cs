using Xunit;

namespace Rinsen.Outback.Tests
{
    /// <summary>
    /// FUNC-005: AbnfValidationHelper.IsValid must fail closed on null input
    /// (return false) instead of throwing a NullReferenceException.
    /// </summary>
    public class AbnfValidationHelperTests
    {
        [Fact]
        public void IsValid_Null_ReturnsFalse()
        {
            Assert.False(Rinsen.Outback.Helpers.AbnfValidationHelper.IsValid((string)null));
        }

        [Fact]
        public void IsValid_Empty_ReturnsFalse()
        {
            Assert.False(Rinsen.Outback.Helpers.AbnfValidationHelper.IsValid(""));
        }

        [Fact]
        public void IsValid_WithLength_Null_ReturnsFalse()
        {
            Assert.False(Rinsen.Outback.Helpers.AbnfValidationHelper.IsValid((string)null, 1, 10));
            Assert.False(Rinsen.Outback.Helpers.AbnfValidationHelper.IsValid((string)null, 0, 10));
        }

        [Fact]
        public void IsValid_WithLength_Empty_ReturnsFalse()
        {
            Assert.False(Rinsen.Outback.Helpers.AbnfValidationHelper.IsValid("", 0, 10));
        }

        [Fact]
        public void IsValid_UnreservedCharacters_ReturnsTrue()
        {
            Assert.True(Rinsen.Outback.Helpers.AbnfValidationHelper.IsValid("aZ09-._~"));
        }

        [Fact]
        public void IsValid_ReservedCharacter_ReturnsFalse()
        {
            Assert.False(Rinsen.Outback.Helpers.AbnfValidationHelper.IsValid("a+b"));
        }

        [Fact]
        public void IsValid_WithLength_OutOfRange_ReturnsFalse()
        {
            Assert.False(Rinsen.Outback.Helpers.AbnfValidationHelper.IsValid("abc", 4, 10));
            Assert.False(Rinsen.Outback.Helpers.AbnfValidationHelper.IsValid("abcdefghij", 1, 9));
        }
    }
}
