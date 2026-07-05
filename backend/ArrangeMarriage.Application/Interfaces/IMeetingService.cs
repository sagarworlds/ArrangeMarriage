using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ArrangeMarriage.Domain.Entities;

namespace ArrangeMarriage.Application.Interfaces
{
    public interface IMeetingService
    {
        Task<Meeting> ScheduleMeetingAsync(Meeting meeting);
        Task<bool> UpdateMeetingStatusAsync(Guid meetingId, MeetingStatus status);
        Task<bool> AddFeedbackAsync(MeetingFeedback feedback);
        Task<IEnumerable<Meeting>> GetMeetingsForUserAsync(Guid userId);
        Task<MarriageSuccess> RecordMarriageSuccessAsync(MarriageSuccess success);
    }
}
