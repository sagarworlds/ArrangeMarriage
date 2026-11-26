using System;
using System.Threading.Tasks;
using ArrangeMarriage.Domain.Entities;

namespace ArrangeMarriage.Application.Interfaces
{
    public interface IUserService
    {
        Task<User> CreateUserAsync(User user);
        Task<User?> GetUserByIdAsync(Guid userId);
        Task<User?> GetUserByEmailAsync(string email);
        Task<bool> UpdateUserAsync(User user);

        /// <summary>
        /// Checks the unique identifiers of a new account against existing users.
        /// Returns the name of the first one already taken ("email", "username" or "phone number"), or null if all are free.
        /// </summary>
        Task<string?> FindDuplicateIdentifierAsync(string username, string email, string phoneNumber);
        Task<bool> VerifyUserAsync(Guid userId);
        Task<System.Collections.Generic.IEnumerable<User>> GetUnverifiedUsersAsync();
    }
}
