using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ArrangeMarriage.Application.Interfaces;
using ArrangeMarriage.Domain.Entities;
using ArrangeMarriage.Infrastructure.Persistence;

namespace ArrangeMarriage.Infrastructure.Services
{
    public class MeetingService : IMeetingService
    {
        private readonly ApplicationDbContext _context;

        public MeetingService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Meeting> ScheduleMeetingAsync(Meeting meeting)
        {
            _context.Meetings.Add(meeting);
            await _context.SaveChangesAsync();
            return meeting;
        }

        public async Task<bool> UpdateMeetingStatusAsync(Guid meetingId, MeetingStatus status)
        {
            var meeting = await _context.Meetings.FindAsync(meetingId);
            if (meeting == null) return false;

            meeting.Status = status;
            var result = await _context.SaveChangesAsync();
            return result > 0;
        }

        public async Task<bool> AddFeedbackAsync(MeetingFeedback feedback)
        {
            _context.MeetingFeedbacks.Add(feedback);
            var result = await _context.SaveChangesAsync();
            return result > 0;
        }

        public async Task<IEnumerable<Meeting>> GetMeetingsForUserAsync(Guid userId)
        {
            return await _context.Meetings
                .Include(m => m.Proposal)
                .Where(m => m.Proposal.SenderId == userId || m.Proposal.ReceiverId == userId)
                .ToListAsync();
        }

        public async Task<MarriageSuccess> RecordMarriageSuccessAsync(MarriageSuccess success)
        {
            _context.MarriageSuccesses.Add(success);
            await _context.SaveChangesAsync();
            return success;
        }
    }
}
