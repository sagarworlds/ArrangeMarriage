using System;

namespace ArrangeMarriage.Domain.Entities
{
    public class Subscription
    {
        public Guid SubscriptionId { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public virtual User User { get; set; } = null!;
        public Guid PackageId { get; set; }
        public virtual MembershipPackage Package { get; set; } = null!;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string PaymentStatus { get; set; } = "Pending";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
