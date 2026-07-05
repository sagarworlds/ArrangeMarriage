using System;

namespace ArrangeMarriage.Domain.Entities
{
    public class PartnerPreference
    {
        public Guid PreferenceId { get; set; } = Guid.NewGuid();
        public Guid ProfileId { get; set; }
        public virtual Profile Profile { get; set; } = null!;

        public int? MinAge { get; set; }
        public int? MaxAge { get; set; }
        public int? MinHeightCm { get; set; }
        public int? MaxHeightCm { get; set; }
        public string? PreferredEducation { get; set; }
        public string? PreferredReligion { get; set; }
        public string? PreferredCaste { get; set; }
        public string? PreferredLocation { get; set; }
        public decimal? MinIncome { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
