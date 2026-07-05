using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ArrangeMarriage.Application.Interfaces;
using ArrangeMarriage.Domain.Entities;

namespace ArrangeMarriage.API.Controllers
{
    [ApiController]
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
            var createdProfile = await _profileService.CreateProfileAsync(profile);
            return Ok(createdProfile);
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetProfile(Guid userId)
        {
            var profile = await _profileService.GetProfileByUserIdAsync(userId);
            if (profile == null) return NotFound();
            return Ok(profile);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateProfile([FromBody] Profile profile)
        {
            var result = await _profileService.UpdateProfileAsync(profile);
            if (!result) return BadRequest("Could not update profile");
            return Ok("Profile updated successfully");
        }

        [HttpPut("family")]
        public async Task<IActionResult> UpdateFamily([FromBody] FamilyDetail detail)
        {
            var result = await _profileService.UpdateFamilyDetailsAsync(detail);
            if (!result) return BadRequest("Could not update family details");
            return Ok("Family details updated successfully");
        }

        [HttpPut("preferences")]
        public async Task<IActionResult> UpdatePreferences([FromBody] PartnerPreference preference)
        {
            var result = await _profileService.UpdatePreferencesAsync(preference);
            if (!result) return BadRequest("Could not update preferences");
            return Ok("Preferences updated successfully");
        }
    }
}
