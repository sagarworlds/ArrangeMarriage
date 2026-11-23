using ArrangeMarriage.Domain.Entities;

namespace ArrangeMarriage.Application.Interfaces
{
    /// <summary>Issues signed access tokens for authenticated users.</summary>
    public interface ITokenService
    {
        /// <summary>Creates a signed JWT carrying the user's id, email and role.</summary>
        string CreateToken(User user);
    }
}
