using ArrangeMarriage.Infrastructure.Services;

namespace ArrangeMarriage.Tests.Services
{
    public class Pbkdf2PasswordHasherTests
    {
        private readonly Pbkdf2PasswordHasher _hasher = new();

        [Fact]
        public void Verify_WithCorrectPassword_ReturnsTrue()
        {
            var hash = _hasher.Hash("correct horse battery staple");

            Assert.True(_hasher.Verify("correct horse battery staple", hash));
        }

        [Fact]
        public void Verify_WithWrongPassword_ReturnsFalse()
        {
            var hash = _hasher.Hash("correct horse battery staple");

            Assert.False(_hasher.Verify("Correct horse battery staple", hash));
        }

        [Fact]
        public void Hash_IsSalted_SamePasswordGivesDifferentHashes()
        {
            Assert.NotEqual(_hasher.Hash("same-password"), _hasher.Hash("same-password"));
        }

        [Fact]
        public void Hash_NeverContainsThePlaintext()
        {
            Assert.DoesNotContain("my-secret-pw", _hasher.Hash("my-secret-pw"));
        }

        [Theory]
        [InlineData("")]
        [InlineData("plaintext-password")] // legacy rows stored before hashing was introduced
        [InlineData("1.2")]
        [InlineData("abc.def.ghi")]
        [InlineData("210000.not-base64!.not-base64!")]
        public void Verify_WithMalformedStoredHash_ReturnsFalseInsteadOfThrowing(string storedHash)
        {
            Assert.False(_hasher.Verify("anything", storedHash));
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void Hash_WithoutPassword_Throws(string? password)
        {
            Assert.Throws<ArgumentException>(() => _hasher.Hash(password!));
        }
    }
}
