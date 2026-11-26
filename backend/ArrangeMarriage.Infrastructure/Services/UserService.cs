using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ArrangeMarriage.Application.Interfaces;
using ArrangeMarriage.Domain.Entities;
using ArrangeMarriage.Infrastructure.Persistence;

namespace ArrangeMarriage.Infrastructure.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;

        public UserService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<User> CreateUserAsync(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<User?> GetUserByIdAsync(Guid userId)
        {
            return await _context.Users.FindAsync(userId);
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<string?> FindDuplicateIdentifierAsync(string username, string email, string phoneNumber)
        {
            if (await _context.Users.AnyAsync(u => u.Email == email)) return "email";
            if (await _context.Users.AnyAsync(u => u.Username == username)) return "username";
            if (await _context.Users.AnyAsync(u => u.PhoneNumber == phoneNumber)) return "phone number";
            return null;
        }

        public async Task<bool> UpdateUserAsync(User user)
        {
            _context.Users.Update(user);
            var result = await _context.SaveChangesAsync();
            return result > 0;
        }

        public async Task<bool> VerifyUserAsync(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;
            user.IsVerified = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<System.Collections.Generic.IEnumerable<User>> GetUnverifiedUsersAsync()
        {
            return await _context.Users
                .Where(u => !u.IsVerified)
                .ToListAsync();
        }
    }
}
