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
        Task<bool> VerifyUserAsync(Guid userId);
    }
}
