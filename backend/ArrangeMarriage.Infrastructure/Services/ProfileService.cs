using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ArrangeMarriage.Application.Interfaces;
using ArrangeMarriage.Domain.Entities;
using ArrangeMarriage.Infrastructure.Persistence;

namespace ArrangeMarriage.Infrastructure.Services
{
    public class ProfileService : IProfileService
    {
        private readonly ApplicationDbContext _context;

        public ProfileService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Profile> CreateProfileAsync(Profile profile)
        {
            _context.Profiles.Add(profile);
            await _context.SaveChangesAsync();
            return profile;
        }

        public async Task<Profile?> GetProfileByUserIdAsync(Guid userId)
        {
            return await _context.Profiles.FirstOrDefaultAsync(p => p.UserId == userId);
        }

        public async Task<bool> UpdateProfileAsync(Profile profile)
        {
            _context.Profiles.Update(profile);
            var result = await _context.SaveChangesAsync();
            return result > 0;
        }

        public async Task<bool> UpdateFamilyDetailsAsync(FamilyDetail detail)
        {
            var existing = await _context.FamilyDetails.FirstOrDefaultAsync(f => f.ProfileId == detail.ProfileId);
            if (existing == null)
            {
                _context.FamilyDetails.Add(detail);
            }
            else
            {
                _context.Entry(existing).CurrentValues.SetValues(detail);
            }
            var result = await _context.SaveChangesAsync();
            return result > 0;
        }

        public async Task<bool> UpdatePreferencesAsync(PartnerPreference preference)
        {
            var existing = await _context.PartnerPreferences.FirstOrDefaultAsync(p => p.ProfileId == preference.ProfileId);
            if (existing == null)
            {
                _context.PartnerPreferences.Add(preference);
            }
            else
            {
                _context.Entry(existing).CurrentValues.SetValues(preference);
            }
            var result = await _context.SaveChangesAsync();
            return result > 0;
        }
    }
}
