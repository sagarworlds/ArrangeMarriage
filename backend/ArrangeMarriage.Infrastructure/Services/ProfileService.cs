using System;
using System.Collections.Generic;
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

        /// <inheritdoc />
        public async Task<Profile> CreateProfileAsync(Profile profile)
        {
            if (!await _context.Users.AnyAsync(u => u.UserId == profile.UserId))
            {
                throw new KeyNotFoundException($"User {profile.UserId} not found.");
            }

            // Start from a fresh row so the id, timestamps and related sections can never come from the client.
            var created = new Profile { UserId = profile.UserId };
            _context.Profiles.Add(created);
            ApplyClientValues(created, profile,
                nameof(Profile.ProfileId), nameof(Profile.UserId), nameof(Profile.CreatedAt), nameof(Profile.UpdatedAt));

            await _context.SaveChangesAsync();
            return created;
        }

        /// <inheritdoc />
        public async Task<Profile?> GetProfileByUserIdAsync(Guid userId)
        {
            return await _context.Profiles.FirstOrDefaultAsync(p => p.UserId == userId);
        }

        /// <inheritdoc />
        public async Task<Profile?> GetProfileByIdAsync(Guid profileId)
        {
            return await _context.Profiles.AsNoTracking().FirstOrDefaultAsync(p => p.ProfileId == profileId);
        }

        /// <inheritdoc />
        public async Task<bool> UpdateProfileAsync(Profile profile)
        {
            // Look the row up by owner, never by the ProfileId in the body: the caller has proven who they are
            // (UserId), but not that the ProfileId they sent is theirs.
            var stored = await _context.Profiles.FirstOrDefaultAsync(p => p.UserId == profile.UserId);
            if (stored == null) return false;

            ApplyClientValues(stored, profile, nameof(Profile.ProfileId), nameof(Profile.UserId), nameof(Profile.CreatedAt));
            stored.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        /// <inheritdoc />
        public async Task<bool> UpdateFamilyDetailsAsync(FamilyDetail detail)
        {
            if (!await ProfileExistsAsync(detail.ProfileId)) return false;

            var stored = await _context.FamilyDetails.FirstOrDefaultAsync(f => f.ProfileId == detail.ProfileId);
            if (stored == null)
            {
                stored = new FamilyDetail { ProfileId = detail.ProfileId };
                _context.FamilyDetails.Add(stored);
            }

            ApplyClientValues(stored, detail, nameof(FamilyDetail.FamilyDetailId), nameof(FamilyDetail.ProfileId));
            stored.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        /// <inheritdoc />
        public async Task<bool> UpdatePreferencesAsync(PartnerPreference preference)
        {
            if (!await ProfileExistsAsync(preference.ProfileId)) return false;

            var stored = await _context.PartnerPreferences.FirstOrDefaultAsync(p => p.ProfileId == preference.ProfileId);
            if (stored == null)
            {
                stored = new PartnerPreference { ProfileId = preference.ProfileId };
                _context.PartnerPreferences.Add(stored);
            }

            ApplyClientValues(stored, preference, nameof(PartnerPreference.PreferenceId), nameof(PartnerPreference.ProfileId));
            stored.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        private Task<bool> ProfileExistsAsync(Guid profileId) =>
            _context.Profiles.AnyAsync(p => p.ProfileId == profileId);

        /// <summary>
        /// Copies the client's scalar values onto <paramref name="stored"/>, but keeps the named properties (keys,
        /// ownership, creation time) exactly as they are on the stored row. Navigation properties are never copied.
        /// </summary>
        /// <remarks>
        /// Doing this explicitly is what keeps ownership checks meaningful: <c>DbContext.Update(clientEntity)</c> would
        /// write whichever row the client's key names, and move it to whichever owner the client's foreign key names.
        /// </remarks>
        private void ApplyClientValues<TEntity>(TEntity stored, TEntity fromClient, params string[] keepStored)
            where TEntity : class
        {
            var entry = _context.Entry(stored);
            var merged = entry.CurrentValues.Clone();
            merged.SetValues(fromClient);
            foreach (var property in keepStored)
            {
                merged[property] = entry.CurrentValues[property];
            }

            entry.CurrentValues.SetValues(merged);
        }
    }
}
