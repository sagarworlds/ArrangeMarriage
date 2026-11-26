using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ArrangeMarriage.API.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ArrangeMarriage.Application.Interfaces;
using ArrangeMarriage.Domain.Entities;

namespace ArrangeMarriage.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class MeetingController : ControllerBase
    {
        private readonly IMeetingService _meetingService;

        public MeetingController(IMeetingService meetingService)
        {
            _meetingService = meetingService;
        }

        [Authorize(Roles = "Admin,Staff")]
        [HttpPost("schedule")]
        public async Task<IActionResult> ScheduleMeeting([FromBody] Meeting meeting)
        {
            try
            {
                return Ok(await _meetingService.ScheduleMeetingAsync(meeting));
            }
            catch (KeyNotFoundException)
            {
                return NotFound("Proposal not found.");
            }
        }

        [Authorize(Roles = "Admin,Staff")]
        [HttpPut("status")]
        public async Task<IActionResult> UpdateStatus([FromBody] MeetingStatusUpdate request)
        {
            var result = await _meetingService.UpdateMeetingStatusAsync(request.MeetingId, request.Status);
            if (!result) return NotFound();
            return Ok("Meeting status updated successfully");
        }

        [HttpPost("feedback")]
        public async Task<IActionResult> AddFeedback([FromBody] MeetingFeedback feedback)
        {
            var meeting = await _meetingService.GetMeetingByIdAsync(feedback.MeetingId);
            if (meeting == null) return NotFound("Meeting not found.");

            // Feedback is written in the name of feedback.UserId, so the caller must be that person (or staff recording
            // it for them), and that person must be one of the two who actually met.
            if (!User.CanAccessUser(feedback.UserId)) return Forbid();
            if (feedback.UserId != meeting.Proposal.SenderId && feedback.UserId != meeting.Proposal.ReceiverId) return Forbid();

            if (feedback.Rating is < 1 or > 5) return BadRequest("Rating must be between 1 and 5.");
            if (await _meetingService.HasFeedbackAsync(feedback.MeetingId, feedback.UserId))
            {
                return Conflict("Feedback has already been left for this meeting.");
            }

            var result = await _meetingService.AddFeedbackAsync(feedback);
            if (!result) return BadRequest("Could not add feedback");
            return Ok("Feedback added successfully");
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserMeetings(Guid userId)
        {
            if (!User.CanAccessUser(userId)) return Forbid();

            var meetings = await _meetingService.GetMeetingsForUserAsync(userId);
            return Ok(meetings);
        }

        [Authorize(Roles = "Admin,Staff")]
        [HttpPost("success")]
        public async Task<IActionResult> RecordSuccess([FromBody] MarriageSuccess success)
        {
            var result = await _meetingService.RecordMarriageSuccessAsync(success);
            return Ok(result);
        }
    }

    public class MeetingStatusUpdate
    {
        public Guid MeetingId { get; set; }
        public MeetingStatus Status { get; set; }
    }
}
