using System;

namespace ArrangeMarriage.Domain.Entities
{
    public class Meeting
    {
        public Guid MeetingId { get; set; } = Guid.NewGuid();
        public Guid ProposalId { get; set; }
        public virtual Proposal Proposal { get; set; } = null!;
        public DateTime MeetingDate { get; set; }
        public string? Venue { get; set; }
        public bool IsOnline { get; set; } = false;
        public MeetingStatus Status { get; set; } = MeetingStatus.Scheduled;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
