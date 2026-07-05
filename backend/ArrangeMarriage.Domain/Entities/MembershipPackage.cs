using System;

namespace ArrangeMarriage.Domain.Entities
{
    public class MembershipPackage
    {
        public Guid PackageId { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int DurationDays { get; set; }
        public string? FeaturesJson { get; set; } // Simplified for now
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
