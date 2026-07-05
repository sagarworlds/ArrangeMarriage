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
    public class MatchmakingService : IMatchmakingService
    {
        private readonly ApplicationDbContext _context;

        public MatchmakingService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Profile>> SearchMatchesAsync(Guid userId)
        {
            var userProfile = await _context.Profiles.FirstOrDefaultAsync(p => p.UserId == userId);
            if (userProfile == null) return new List<Profile>();

            var pref = await _context.PartnerPreferences.FirstOrDefaultAsync(p => p.ProfileId == userProfile.ProfileId);
            if (pref == null) return new List<Profile>();

            // Refined matching logic based on comprehensive partner preferences
            var matches = await _context.Profiles
                .Where(p => p.UserId != userId)
                .Where(p => p.Gender != userProfile.Gender)
                .Where(p => (pref.MinAge == null || p.Dob.Year <= DateTime.UtcNow.Year - pref.MinAge)
                         && (pref.MaxAge == null || p.Dob.Year >= DateTime.UtcNow.Year - pref.MaxAge))
                .Where(p => pref.PreferredReligion == null || p.Religion == pref.PreferredReligion)
                .Where(p => (pref.MinHeightCm == null || p.HeightCm >= pref.MinHeightCm)
                         && (pref.MaxHeightCm == null || p.HeightCm <= pref.MaxHeightCm))
                .Where(p => pref.PreferredCaste == null || p.CasteCommunity == pref.PreferredCaste)
                .Where(p => pref.PreferredLocation == null || p.City == pref.PreferredLocation || p.State == pref.PreferredLocation || p.Country == pref.PreferredLocation)
                .Where(p => pref.MinIncome == null || p.AnnualIncome >= pref.MinIncome)
                .Where(p => pref.PreferredEducation == null || (p.Education != null && p.Education.ToLower().Contains(pref.PreferredEducation.ToLower())))
                .ToListAsync();

            return matches;
        }

        public async Task<Proposal> SendProposalAsync(Guid senderId, Guid receiverId, string? notes)
        {
            var proposal = new Proposal
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Notes = notes,
                Status = ProposalStatus.Sent
            };

            _context.Proposals.Add(proposal);
            await _context.SaveChangesAsync();
            return proposal;
        }

        public async Task<bool> UpdateProposalStatusAsync(Guid proposalId, ProposalStatus status)
        {
            var proposal = await _context.Proposals.FindAsync(proposalId);
            if (proposal == null) return false;

            proposal.Status = status;
            proposal.UpdatedAt = DateTime.UtcNow;
            var result = await _context.SaveChangesAsync();
            return result > 0;
        }

        public async Task<IEnumerable<Proposal>> GetProposalsForUserAsync(Guid userId)
        {
            return await _context.Proposals
                .Where(p => p.SenderId == userId || p.ReceiverId == userId)
                .ToListAsync();
        }

        public async Task<Proposal?> GetProposalByIdAsync(Guid proposalId)
        {
            return await _context.Proposals.FindAsync(proposalId);
        }
    }
}
