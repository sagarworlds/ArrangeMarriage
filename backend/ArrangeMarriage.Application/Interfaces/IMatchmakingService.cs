using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ArrangeMarriage.Domain.Entities;

namespace ArrangeMarriage.Application.Interfaces
{
    public interface IMatchmakingService
    {
        Task<IEnumerable<Profile>> SearchMatchesAsync(Guid userId);
        Task<Proposal> SendProposalAsync(Guid senderId, Guid receiverId, string? notes);
        Task<bool> UpdateProposalStatusAsync(Guid proposalId, ProposalStatus status);
        Task<IEnumerable<Proposal>> GetProposalsForUserAsync(Guid userId);
        Task<Proposal?> GetProposalByIdAsync(Guid proposalId);
    }
}
