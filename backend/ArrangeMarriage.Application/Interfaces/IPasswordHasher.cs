namespace ArrangeMarriage.Application.Interfaces
{
    /// <summary>Hashes and verifies user passwords so plaintext is never stored.</summary>
    public interface IPasswordHasher
    {
        /// <summary>Returns a salted, self-describing hash of <paramref name="password"/>.</summary>
        string Hash(string password);

        /// <summary>Returns true when <paramref name="password"/> matches <paramref name="hash"/>.</summary>
        bool Verify(string password, string hash);
    }
}
