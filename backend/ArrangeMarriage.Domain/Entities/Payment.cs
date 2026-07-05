using System;

namespace ArrangeMarriage.Domain.Entities
{
    public class Payment
    {
        public Guid PaymentId { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public virtual User User { get; set; } = null!;
        public decimal Amount { get; set; }
        public PaymentType Type { get; set; }
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
        public string? TransactionId { get; set; }
        public string? FeeType { get; set; } // Registration, Matchmaking, Success
        public DateTime? PaymentDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
