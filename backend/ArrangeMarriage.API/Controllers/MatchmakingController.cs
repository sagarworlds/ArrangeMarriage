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
    public class MatchmakingController : ControllerBase
    {
        private readonly IMatchmakingService _matchmakingService;

        public MatchmakingController(IMatchmakingService matchmakingService)
        {
            _matchmakingService = matchmakingService;
        }

        [HttpGet("search/{userId}")]
        public async Task<IActionResult> SearchMatches(Guid userId)
        {
            if (!User.CanAccessUser(userId)) return Forbid();

            var matches = await _matchmakingService.SearchMatchesAsync(userId);
            return Ok(matches);
        }

        [HttpPost("propose")]
        public async Task<IActionResult> SendProposal([FromBody] ProposalRequest request)
        {
            if (!User.CanAccessUser(request.SenderId)) return Forbid();

            var proposal = await _matchmakingService.SendProposalAsync(request.SenderId, request.ReceiverId, request.Notes);
            return Ok(proposal);
        }

        [Authorize(Roles = "Admin,Staff")]
        [HttpPut("status")]
        public async Task<IActionResult> UpdateStatus([FromBody] ProposalStatusUpdate request)
        {
            var result = await _matchmakingService.UpdateProposalStatusAsync(request.ProposalId, request.Status);
            if (!result) return NotFound();
            return Ok("Proposal status updated successfully");
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserProposals(Guid userId)
        {
            if (!User.CanAccessUser(userId)) return Forbid();

            var proposals = await _matchmakingService.GetProposalsForUserAsync(userId);
            return Ok(proposals);
        }
    }

    public class ProposalRequest
    {
        public Guid SenderId { get; set; }
        public Guid ReceiverId { get; set; }
        public string? Notes { get; set; }
    }

    public class ProposalStatusUpdate
    {
        public Guid ProposalId { get; set; }
        public ProposalStatus Status { get; set; }
    }
}
