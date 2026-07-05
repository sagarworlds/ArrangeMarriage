using System;

namespace ArrangeMarriage.Domain.Entities
{
    public class Proposal
    {
        public Guid ProposalId { get; set; } = Guid.NewGuid();
        public Guid SenderId { get; set; }
        public virtual User Sender { get; set; } = null!;
        public Guid ReceiverId { get; set; }
        public virtual User Receiver { get; set; } = null!;
        public ProposalStatus Status { get; set; } = ProposalStatus.Sent;
        public int? MatchScore { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
