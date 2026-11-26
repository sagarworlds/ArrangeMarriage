using System;
using System.Collections.Generic;
using System.Linq;
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
        // The only stages a receiver may move a proposal to...
        private static readonly ProposalStatus[] ReceiverMayChoose =
        {
            ProposalStatus.Viewed, ProposalStatus.Accepted, ProposalStatus.Rejected
        };

        // ...and, once the matchmaker has reached one of these, the receiver can no longer change the proposal.
        private static readonly ProposalStatus[] MatchmakerStages =
        {
            ProposalStatus.MeetingArranged, ProposalStatus.Finalized
        };

        private readonly IMatchmakingService _matchmakingService;
        private readonly IUserService _userService;

        public MatchmakingController(IMatchmakingService matchmakingService, IUserService userService)
        {
            _matchmakingService = matchmakingService;
            _userService = userService;
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
            if (request.SenderId == request.ReceiverId) return BadRequest("You cannot send a proposal to yourself.");
            if (await _userService.GetUserByIdAsync(request.ReceiverId) == null) return NotFound("Receiver not found.");

            var proposal = await _matchmakingService.SendProposalAsync(request.SenderId, request.ReceiverId, request.Notes);
            return Ok(proposal);
        }

        [HttpPut("status")]
        public async Task<IActionResult> UpdateStatus([FromBody] ProposalStatusUpdate request)
        {
            var proposal = await _matchmakingService.GetProposalByIdAsync(request.ProposalId);
            if (proposal == null) return NotFound();

            // Staff run the whole workflow. The receiver may respond to a proposal addressed to them (but cannot jump to
            // the matchmaker's stages, nor undo them once reached). The sender and everyone else cannot change it: a sender
            // must not be able to mark their own proposal "Accepted" for the receiver.
            var isReceiver = User.GetUserId() == proposal.ReceiverId;
            var receiverMayAct = isReceiver
                && ReceiverMayChoose.Contains(request.Status)
                && !MatchmakerStages.Contains(proposal.Status);
            var allowed = User.IsStaff() || receiverMayAct;
            if (!allowed) return Forbid();

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
