using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ArrangeMarriage.Domain.Entities;

namespace ArrangeMarriage.Application.Interfaces
{
    /// <summary>Meetings arranged from accepted proposals, the feedback people leave about them, and successful marriages.</summary>
    public interface IMeetingService
    {
        /// <summary>Schedules a meeting for an existing proposal. The meeting id and creation time are generated here.</summary>
        /// <exception cref="KeyNotFoundException">The proposal <c>meeting.ProposalId</c> does not exist.</exception>
        Task<Meeting> ScheduleMeetingAsync(Meeting meeting);

        Task<bool> UpdateMeetingStatusAsync(Guid meetingId, MeetingStatus status);

        /// <summary>Stores feedback under a newly generated id. The caller must already have verified who may leave it.</summary>
        Task<bool> AddFeedbackAsync(MeetingFeedback feedback);

        /// <summary>Returns the meeting (with its proposal loaded, so its two participants are known), or null if it does not exist.</summary>
        Task<Meeting?> GetMeetingByIdAsync(Guid meetingId);

        /// <summary>True when <paramref name="userId"/> has already left feedback for the meeting.</summary>
        Task<bool> HasFeedbackAsync(Guid meetingId, Guid userId);

        Task<IEnumerable<Meeting>> GetMeetingsForUserAsync(Guid userId);

        /// <summary>Records a successful match under a newly generated id.</summary>
        Task<MarriageSuccess> RecordMarriageSuccessAsync(MarriageSuccess success);
    }
}
