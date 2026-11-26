using System;
using System.Threading.Tasks;
using ArrangeMarriage.Domain.Entities;

namespace ArrangeMarriage.Application.Interfaces
{
    /// <summary>
    /// Member profiles and their family and partner-preference sections. Callers decide who may act (see the
    /// controllers); these methods never trust identifiers or ownership supplied inside the entities they receive.
    /// </summary>
    public interface IProfileService
    {
        /// <summary>
        /// Creates a profile for <c>profile.UserId</c>. The new row gets a server-generated id; any id, creation time
        /// or related sections on <paramref name="profile"/> are ignored.
        /// </summary>
        /// <returns>The stored profile.</returns>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">No user with <c>profile.UserId</c> exists.</exception>
        Task<Profile> CreateProfileAsync(Profile profile);

        /// <summary>Returns the profile that belongs to <paramref name="userId"/>, or null if the user has none.</summary>
        Task<Profile?> GetProfileByUserIdAsync(Guid userId);

        /// <summary>Returns the profile with the given id, or null if it does not exist.</summary>
        Task<Profile?> GetProfileByIdAsync(Guid profileId);

        /// <summary>
        /// Replaces the editable fields of the profile that belongs to <c>profile.UserId</c>. The profile id, its owner
        /// and its creation time are kept from the stored row, whatever <paramref name="profile"/> contains.
        /// </summary>
        /// <returns>True when updated; false when that user has no profile.</returns>
        Task<bool> UpdateProfileAsync(Profile profile);

        /// <summary>
        /// Creates or updates the family section of profile <c>detail.ProfileId</c>. Row identity is kept from the
        /// stored row (or generated when creating).
        /// </summary>
        /// <returns>True when saved; false when the profile does not exist.</returns>
        Task<bool> UpdateFamilyDetailsAsync(FamilyDetail detail);

        /// <summary>
        /// Creates or updates the partner preferences of profile <c>preference.ProfileId</c>. Row identity is kept
        /// from the stored row (or generated when creating).
        /// </summary>
        /// <returns>True when saved; false when the profile does not exist.</returns>
        Task<bool> UpdatePreferencesAsync(PartnerPreference preference);
    }
}
