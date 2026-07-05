using System;

namespace ArrangeMarriage.Domain.Entities
{
    public class MarriageSuccess
    {
        public Guid SuccessId { get; set; } = Guid.NewGuid();
        public Guid BrideId { get; set; }
        public virtual User Bride { get; set; } = null!;
        public Guid GroomId { get; set; }
        public virtual User Groom { get; set; } = null!;
        public Guid ProposalId { get; set; }
        public virtual Proposal Proposal { get; set; } = null!;
        public DateTime? EngagementDate { get; set; }
        public DateTime? MarriageDate { get; set; }
        public decimal? SuccessFeeAmount { get; set; }
        public PaymentStatus SuccessFeeStatus { get; set; } = PaymentStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
