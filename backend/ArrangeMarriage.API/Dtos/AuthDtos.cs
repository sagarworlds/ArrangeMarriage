using System;
using System.ComponentModel.DataAnnotations;
using ArrangeMarriage.Domain.Entities;

namespace ArrangeMarriage.API.Dtos
{
    /// <summary>
    /// Public registration payload. Length limits mirror docs/database_schema.sql. Role/verification are
    /// deliberately not client-settable beyond the allowed roles.
    /// </summary>
    public class RegisterRequest
    {
        [Required, StringLength(50, MinimumLength = 3)]
        public string Username { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required, Phone, StringLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required, StringLength(128, MinimumLength = 8)]
        public string Password { get; set; } = string.Empty;

        public UserRole Role { get; set; } = UserRole.Family;
    }

    public class LoginRequest
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>User data safe to return to clients (no password hash).</summary>
    public class UserResponse
    {
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public bool IsVerified { get; set; }
        public DateTime CreatedAt { get; set; }

        public static UserResponse From(User user) => new()
        {
            UserId = user.UserId,
            Username = user.Username,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Role = user.Role,
            IsVerified = user.IsVerified,
            CreatedAt = user.CreatedAt
        };
    }

    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public UserResponse User { get; set; } = null!;
    }
}
