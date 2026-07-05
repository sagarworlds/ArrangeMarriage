using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ArrangeMarriage.Application.Interfaces;
using ArrangeMarriage.Domain.Entities;

namespace ArrangeMarriage.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MeetingController : ControllerBase
    {
        private readonly IMeetingService _meetingService;

        public MeetingController(IMeetingService meetingService)
        {
            _meetingService = meetingService;
        }

        [HttpPost("schedule")]
        public async Task<IActionResult> ScheduleMeeting([FromBody] Meeting meeting)
        {
            var result = await _meetingService.ScheduleMeetingAsync(meeting);
            return Ok(result);
        }

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
            var result = await _meetingService.AddFeedbackAsync(feedback);
            if (!result) return BadRequest("Could not add feedback");
            return Ok("Feedback added successfully");
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserMeetings(Guid userId)
        {
            var meetings = await _meetingService.GetMeetingsForUserAsync(userId);
            return Ok(meetings);
        }

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
