using System;

namespace ArrangeMarriage.Domain.Entities
{
    public class FamilyDetail
    {
        public Guid FamilyDetailId { get; set; } = Guid.NewGuid();
        public Guid ProfileId { get; set; }
        public virtual Profile Profile { get; set; } = null!;

        public string? FatherName { get; set; }
        public string? MotherName { get; set; }
        public int? SiblingsCount { get; set; }
        public string? FamilyType { get; set; }
        public string? FamilyBusiness { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
