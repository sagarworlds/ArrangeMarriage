using System;
using System.Threading.Tasks;
using ArrangeMarriage.Domain.Entities;

namespace ArrangeMarriage.Application.Interfaces
{
    public interface IProfileService
    {
        Task<Profile> CreateProfileAsync(Profile profile);
        Task<Profile?> GetProfileByUserIdAsync(Guid userId);
        Task<bool> UpdateProfileAsync(Profile profile);
        Task<bool> UpdateFamilyDetailsAsync(FamilyDetail detail);
        Task<bool> UpdatePreferencesAsync(PartnerPreference preference);
    }
}
