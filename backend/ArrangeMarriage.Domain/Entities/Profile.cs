using System;

namespace ArrangeMarriage.Domain.Entities
{
    public class Profile
    {
        public Guid ProfileId { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public virtual User User { get; set; } = null!;

        public string FullName { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public DateTime Dob { get; set; }
        public int? HeightCm { get; set; }
        public int? WeightKg { get; set; }
        public string? Religion { get; set; }
        public string? CasteCommunity { get; set; }
        public string? MotherTongue { get; set; }
        public MaritalStatus MaritalStatus { get; set; } = MaritalStatus.NeverMarried;
        public string? Education { get; set; }
        public string? Occupation { get; set; }
        public decimal? AnnualIncome { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; }
        public bool Smoking { get; set; } = false;
        public bool Drinking { get; set; } = false;
        public string? Diet { get; set; }
        public string? Hobbies { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public int? MatchScore { get; set; }

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public int? HoroscopeMatchScore { get; set; }

        public virtual FamilyDetail? FamilyDetail { get; set; }
        public virtual PartnerPreference? PartnerPreference { get; set; }
    }
}
