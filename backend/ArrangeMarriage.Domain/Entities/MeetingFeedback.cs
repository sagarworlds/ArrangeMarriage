using System;

namespace ArrangeMarriage.Domain.Entities
{
    public class MeetingFeedback
    {
        public Guid FeedbackId { get; set; } = Guid.NewGuid();
        public Guid MeetingId { get; set; }
        public virtual Meeting Meeting { get; set; } = null!;
        public Guid UserId { get; set; }
        public virtual User User { get; set; } = null!;
        public int Rating { get; set; } // 1-5
        public string? Comments { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
