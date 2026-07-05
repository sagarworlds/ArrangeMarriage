namespace ArrangeMarriage.Domain.Entities
{
    public enum UserRole
    {
        Admin,
        Bride,
        Groom,
        Staff,
        Family
    }

    public enum MaritalStatus
    {
        NeverMarried,
        Divorced,
        Widowed,
        AwaitingDivorce
    }

    public enum ProposalStatus
    {
        Sent,
        Viewed,
        Accepted,
        Rejected,
        MeetingArranged,
        Finalized
    }

    public enum PaymentType
    {
        UPI,
        Card,
        NetBanking,
        Cash
    }

    public enum PaymentStatus
    {
        Paid,
        Pending,
        Overdue,
        Refunded
    }

    public enum MeetingStatus
    {
        Scheduled,
        Completed,
        Cancelled
    }
}
