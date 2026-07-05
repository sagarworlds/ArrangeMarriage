using System;

namespace ArrangeMarriage.Domain.Entities
{
    public class Message
    {
        public Guid MessageId { get; set; } = Guid.NewGuid();
        public Guid SenderId { get; set; }
        public Guid ReceiverId { get; set; }
        public string Content { get; set; } = string.Empty;
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual User Sender { get; set; } = null!;
        public virtual User Receiver { get; set; } = null!;
    }
}
