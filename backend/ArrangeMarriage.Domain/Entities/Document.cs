using System;

namespace ArrangeMarriage.Domain.Entities
{
    public class Document
    {
        public Guid DocumentId { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public virtual User User { get; set; } = null!;

        public string DocumentType { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public bool IsVerified { get; set; } = false;
        public DateTime? VerifiedAt { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}
