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
        private readonly IAstroService _astroService;

        public MatchmakingService(ApplicationDbContext context, IAstroService astroService)
        {
            _context = context;
            _astroService = astroService;
        }

        public async Task<IEnumerable<Profile>> SearchMatchesAsync(Guid userId)
        {
            var userProfile = await _context.Profiles.FirstOrDefaultAsync(p => p.UserId == userId);
            if (userProfile == null) return new List<Profile>();

            var pref = await _context.PartnerPreferences.FirstOrDefaultAsync(p => p.ProfileId == userProfile.ProfileId);
            if (pref == null) return new List<Profile>();

            // Refined matching logic: Filter basic constraints, score preferences in memory
            // Convert the age range into a birth-date window (older => earlier date). Comparing full
            // dates rather than years avoids off-by-one errors around birthdays.
            var today = DateTime.UtcNow.Date;
            DateTime? latestDob = pref.MinAge.HasValue ? today.AddYears(-pref.MinAge.Value) : null;
            DateTime? earliestDob = pref.MaxAge.HasValue ? today.AddYears(-(pref.MaxAge.Value + 1)).AddDays(1) : null;

            var matches = await _context.Profiles
                .AsNoTracking() // scores are transient view data and must never be persisted
                .Where(p => p.UserId != userId)
                .Where(p => p.Gender != userProfile.Gender)
                .Where(p => latestDob == null || p.Dob <= latestDob)
                .Where(p => earliestDob == null || p.Dob >= earliestDob)
                .ToListAsync();

            foreach (var match in matches)
            {
                int score = 0;

                // Religion Match (20 points)
                if (pref.PreferredReligion == null || match.Religion == pref.PreferredReligion) score += 20;

                // Caste Match (20 points)
                if (pref.PreferredCaste == null || match.CasteCommunity == pref.PreferredCaste) score += 20;

                // Height Match (15 points)
                if ((pref.MinHeightCm == null || match.HeightCm >= pref.MinHeightCm) 
                 && (pref.MaxHeightCm == null || match.HeightCm <= pref.MaxHeightCm))
                {
                    score += 15;
                }

                // Location Match (15 points)
                if (pref.PreferredLocation == null 
                 || match.City == pref.PreferredLocation 
                 || match.State == pref.PreferredLocation 
                 || match.Country == pref.PreferredLocation)
                {
                    score += 15;
                }

                // Income Match (15 points)
                if (pref.MinIncome == null || match.AnnualIncome >= pref.MinIncome) score += 15;

                // Education Match (15 points)
                if (pref.PreferredEducation == null 
                 || (match.Education != null && match.Education.ToLower().Contains(pref.PreferredEducation.ToLower())))
                {
                    score += 15;
                }

                match.MatchScore = score;
                match.HoroscopeMatchScore = _astroService.CalculateGunaMatching(userProfile, match);
            }

            return matches.OrderByDescending(m => m.MatchScore);
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
