using System;
using System.Collections.Generic;

namespace ArrangeMarriage.Domain.Entities
{
    public class User
    {
        public Guid UserId { get; set; } = Guid.NewGuid();
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public bool IsVerified { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public virtual Profile? Profile { get; set; }
        public virtual ICollection<Document> Documents { get; set; } = new List<Document>();
    }
}
