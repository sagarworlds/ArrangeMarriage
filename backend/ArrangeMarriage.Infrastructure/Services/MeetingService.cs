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
            if (!await _context.Proposals.AnyAsync(p => p.ProposalId == meeting.ProposalId))
            {
                throw new KeyNotFoundException($"Proposal {meeting.ProposalId} not found.");
            }

            // Identity and creation time are the server's to decide, not the client's.
            meeting.MeetingId = Guid.NewGuid();
            meeting.CreatedAt = DateTime.UtcNow;
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
            feedback.FeedbackId = Guid.NewGuid();
            feedback.CreatedAt = DateTime.UtcNow;
            _context.MeetingFeedbacks.Add(feedback);
            var result = await _context.SaveChangesAsync();
            return result > 0;
        }

        public async Task<Meeting?> GetMeetingByIdAsync(Guid meetingId)
        {
            return await _context.Meetings
                .Include(m => m.Proposal)
                .FirstOrDefaultAsync(m => m.MeetingId == meetingId);
        }

        public async Task<bool> HasFeedbackAsync(Guid meetingId, Guid userId)
        {
            return await _context.MeetingFeedbacks.AnyAsync(f => f.MeetingId == meetingId && f.UserId == userId);
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
            success.SuccessId = Guid.NewGuid();
            success.CreatedAt = DateTime.UtcNow;
            _context.MarriageSuccesses.Add(success);
            await _context.SaveChangesAsync();
            return success;
        }
    }
}
