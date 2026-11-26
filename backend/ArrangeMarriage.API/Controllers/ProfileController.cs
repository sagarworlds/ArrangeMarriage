using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ArrangeMarriage.API.Security;
using ArrangeMarriage.Application.Interfaces;
using ArrangeMarriage.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArrangeMarriage.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class ProfileController : ControllerBase
    {
        private readonly IProfileService _profileService;

        public ProfileController(IProfileService profileService)
        {
            _profileService = profileService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateProfile([FromBody] Profile profile)
        {
            if (!User.CanAccessUser(profile.UserId)) return Forbid();

            if (await _profileService.GetProfileByUserIdAsync(profile.UserId) != null)
            {
                return Conflict("This user already has a profile. Use PUT to change it.");
            }

            try
            {
                return Ok(await _profileService.CreateProfileAsync(profile));
            }
            catch (KeyNotFoundException)
            {
                return NotFound("User not found.");
            }
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetProfile(Guid userId)
        {
            if (!User.CanAccessUser(userId)) return Forbid();

            var profile = await _profileService.GetProfileByUserIdAsync(userId);
            if (profile == null) return NotFound();
            return Ok(profile);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateProfile([FromBody] Profile profile)
        {
            // The profile that changes is the one owned by profile.UserId, and that id is checked against the token.
            // The ProfileId in the body is deliberately ignored: honouring it would let a caller pass this check with
            // their own UserId while naming someone else's profile row.
            if (!User.CanAccessUser(profile.UserId)) return Forbid();

            var updated = await _profileService.UpdateProfileAsync(profile);
            if (!updated) return NotFound("This user has no profile yet.");
            return Ok("Profile updated successfully");
        }

        [HttpPut("family")]
        public async Task<IActionResult> UpdateFamily([FromBody] FamilyDetail detail)
        {
            var denied = await DenyUnlessProfileAccessibleAsync(detail.ProfileId);
            if (denied != null) return denied;

            var saved = await _profileService.UpdateFamilyDetailsAsync(detail);
            if (!saved) return NotFound("Profile not found.");
            return Ok("Family details updated successfully");
        }

        [HttpPut("preferences")]
        public async Task<IActionResult> UpdatePreferences([FromBody] PartnerPreference preference)
        {
            var denied = await DenyUnlessProfileAccessibleAsync(preference.ProfileId);
            if (denied != null) return denied;

            var saved = await _profileService.UpdatePreferencesAsync(preference);
            if (!saved) return NotFound("Profile not found.");
            return Ok("Preferences updated successfully");
        }

        /// <summary>
        /// Returns null when the caller may change the profile (its owner, or staff); otherwise the 404/403 to send back.
        /// Ownership comes from the stored profile, never from anything in the request body.
        /// </summary>
        private async Task<IActionResult?> DenyUnlessProfileAccessibleAsync(Guid profileId)
        {
            var profile = await _profileService.GetProfileByIdAsync(profileId);
            if (profile == null) return NotFound("Profile not found.");
            return User.CanAccessUser(profile.UserId) ? null : Forbid();
        }
    }
}
