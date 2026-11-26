using System.Net;
using System.Net.Http.Json;
using ArrangeMarriage.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ArrangeMarriage.Tests.Api
{
    /// <summary>
    /// Who may create or change a profile and its family/preference sections. Every rule is judged against the
    /// stored record's owner, never against ids or nested objects supplied in the request body.
    /// </summary>
    public class ProfileOwnershipApiTests : IClassFixture<ApiFactory>
    {
        private readonly ApiFactory _factory;

        public ProfileOwnershipApiTests(ApiFactory factory) => _factory = factory;

        private static Dictionary<string, object?> ProfileBody(Guid userId, string fullName = "Asha Rao", string? city = "Pune") => new()
        {
            ["userId"] = userId,
            ["fullName"] = fullName,
            ["gender"] = "Female",
            ["dob"] = "1996-04-12T00:00:00Z",
            ["city"] = city
        };

        private Task<Profile> ProfileOfUserAsync(Guid userId) =>
            _factory.WithDbAsync(db => db.Profiles.AsNoTracking().SingleAsync(p => p.UserId == userId));

        private Task<Profile> ProfileByIdAsync(Guid profileId) =>
            _factory.WithDbAsync(db => db.Profiles.AsNoTracking().SingleAsync(p => p.ProfileId == profileId));

        // ---- creating a profile ------------------------------------------------------------------------------

        [Fact]
        public async Task CreateProfile_AssignsItsOwnId_IgnoringOneChosenByTheClient()
        {
            var victim = await _factory.CreateUserAsync();
            var attacker = await _factory.CreateUserAsync();
            var victimProfileId = await _factory.SeedProfileAsync(victim, "Victim Name");
            using var client = _factory.ClientFor(attacker);
            var body = ProfileBody(attacker.UserId);
            body["profileId"] = victimProfileId;

            var response = await client.PostAsJsonAsync("/api/profile", body);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotEqual(victimProfileId, (await ProfileOfUserAsync(attacker.UserId)).ProfileId);
            var victimProfile = await ProfileByIdAsync(victimProfileId);
            Assert.Equal(victim.UserId, victimProfile.UserId);
            Assert.Equal("Victim Name", victimProfile.FullName);
        }

        [Fact]
        public async Task CreateProfile_WhenTheUserAlreadyHasOne_Returns409_AndKeepsTheExistingProfile()
        {
            var member = await _factory.CreateUserAsync();
            await _factory.SeedProfileAsync(member, "Original Name");
            using var client = _factory.ClientFor(member);

            var response = await client.PostAsJsonAsync("/api/profile", ProfileBody(member.UserId, "Second Attempt"));

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Assert.Equal("Original Name", (await ProfileOfUserAsync(member.UserId)).FullName);
        }

        [Fact]
        public async Task CreateProfile_CannotSmuggleInAnAdminAccount_ThroughANestedUser()
        {
            var member = await _factory.CreateUserAsync();
            var smuggledId = Guid.NewGuid();
            var body = ProfileBody(member.UserId);
            body["user"] = new
            {
                userId = smuggledId,
                username = "smuggled",
                email = "smuggled@example.com",
                phoneNumber = "9999999999",
                passwordHash = "x",
                role = (int)UserRole.Admin,
                isVerified = true
            };
            using var client = _factory.ClientFor(member);

            await client.PostAsJsonAsync("/api/profile", body);

            Assert.False(await _factory.WithDbAsync(db => db.Users.AnyAsync(u => u.UserId == smuggledId || u.Email == "smuggled@example.com")));
        }

        // ---- updating a profile ------------------------------------------------------------------------------

        [Fact]
        public async Task UpdateProfile_ByTheOwner_ChangesEditableFields_ButNeverIdentityOrOwnership()
        {
            var owner = await _factory.CreateUserAsync();
            var profileId = await _factory.SeedProfileAsync(owner, "Asha Rao", city: "Pune");
            var createdAt = (await ProfileByIdAsync(profileId)).CreatedAt;
            var body = ProfileBody(owner.UserId, "Asha R. Rao", "Mumbai");
            body["profileId"] = Guid.NewGuid(); // a stale or wrong id from the client must not matter
            using var client = _factory.ClientFor(owner);

            var response = await client.PutAsJsonAsync("/api/profile", body);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var stored = await ProfileOfUserAsync(owner.UserId); // SingleAsync: also proves no duplicate row appeared
            Assert.Equal(profileId, stored.ProfileId);
            Assert.Equal(owner.UserId, stored.UserId);
            Assert.Equal(createdAt, stored.CreatedAt);
            Assert.Equal("Asha R. Rao", stored.FullName);
            Assert.Equal("Mumbai", stored.City);
        }

        [Fact]
        public async Task UpdateProfile_ForAnotherUser_IsForbidden_AndChangesNothing()
        {
            var victim = await _factory.CreateUserAsync();
            var attacker = await _factory.CreateUserAsync();
            await _factory.SeedProfileAsync(victim, "Victim Name", city: "Delhi");
            using var client = _factory.ClientFor(attacker);

            var response = await client.PutAsJsonAsync("/api/profile", ProfileBody(victim.UserId, "PWNED", "Nowhere"));

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            var stored = await ProfileOfUserAsync(victim.UserId);
            Assert.Equal("Victim Name", stored.FullName);
            Assert.Equal("Delhi", stored.City);
        }

        [Fact]
        public async Task UpdateProfile_ClaimingSomeoneElsesProfileId_CannotOverwriteOrTakeOverThatProfile()
        {
            var victim = await _factory.CreateUserAsync();
            var attacker = await _factory.CreateUserAsync();
            var victimProfileId = await _factory.SeedProfileAsync(victim, "Victim Name", city: "Delhi");
            await _factory.SeedProfileAsync(attacker, "Attacker Name", city: "Goa");
            // The attacker passes the "is this userId mine?" check with their own id, but names the victim's profile row.
            var body = ProfileBody(attacker.UserId, "PWNED", "Nowhere");
            body["profileId"] = victimProfileId;
            using var client = _factory.ClientFor(attacker);

            await client.PutAsJsonAsync("/api/profile", body);

            var victimProfile = await ProfileByIdAsync(victimProfileId);
            Assert.Equal(victim.UserId, victimProfile.UserId);
            Assert.Equal("Victim Name", victimProfile.FullName);
            Assert.Equal("Delhi", victimProfile.City);
        }

        [Fact]
        public async Task UpdateProfile_IgnoresNestedObjects_SoOtherPeoplesFamilyDetailsCannotBeReassigned()
        {
            var victim = await _factory.CreateUserAsync();
            var attacker = await _factory.CreateUserAsync();
            var victimProfileId = await _factory.SeedProfileAsync(victim);
            var victimFamilyId = await _factory.SeedFamilyAsync(victimProfileId, "Victim Father");
            var attackerProfileId = await _factory.SeedProfileAsync(attacker);
            var body = ProfileBody(attacker.UserId);
            body["profileId"] = attackerProfileId; // a normal client sends its own, valid profile id
            body["familyDetail"] = new { familyDetailId = victimFamilyId, profileId = attackerProfileId, fatherName = "hijacked" };
            using var client = _factory.ClientFor(attacker);

            await client.PutAsJsonAsync("/api/profile", body);

            var family = await _factory.WithDbAsync(db => db.FamilyDetails.AsNoTracking().SingleAsync(f => f.FamilyDetailId == victimFamilyId));
            Assert.Equal(victimProfileId, family.ProfileId);
            Assert.Equal("Victim Father", family.FatherName);
        }

        [Fact]
        public async Task UpdateProfile_WhenTheUserHasNoProfile_Returns404()
        {
            var member = await _factory.CreateUserAsync();
            using var client = _factory.ClientFor(member);

            var response = await client.PutAsJsonAsync("/api/profile", ProfileBody(member.UserId));

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task UpdateProfile_ByStaff_CanEditAMembersProfile()
        {
            var member = await _factory.CreateUserAsync();
            var staff = await _factory.CreateUserAsync(UserRole.Staff);
            await _factory.SeedProfileAsync(member, "Typo Nmae");
            using var client = _factory.ClientFor(staff);

            var response = await client.PutAsJsonAsync("/api/profile", ProfileBody(member.UserId, "Corrected Name"));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Corrected Name", (await ProfileOfUserAsync(member.UserId)).FullName);
        }

        // ---- family details and partner preferences ------------------------------------------------------------

        private static Dictionary<string, object?> SectionBody(string section, Guid profileId, string marker) =>
            section == "family"
                ? new() { ["profileId"] = profileId, ["fatherName"] = marker }
                : new() { ["profileId"] = profileId, ["preferredReligion"] = marker };

        private Task<List<string?>> ReadMarkersAsync(string section, Guid profileId) =>
            _factory.WithDbAsync(db => section == "family"
                ? db.FamilyDetails.AsNoTracking().Where(f => f.ProfileId == profileId).Select(f => f.FatherName).ToListAsync()
                : db.PartnerPreferences.AsNoTracking().Where(p => p.ProfileId == profileId).Select(p => p.PreferredReligion).ToListAsync());

        private Task SeedSectionAsync(string section, Guid profileId, string marker) =>
            section == "family" ? _factory.SeedFamilyAsync(profileId, marker) : _factory.SeedPreferenceAsync(profileId, marker);

        [Theory]
        [InlineData("family")]
        [InlineData("preferences")]
        public async Task Section_OnYourOwnProfile_IsCreatedThenUpdatedInPlace(string section)
        {
            var owner = await _factory.CreateUserAsync();
            var profileId = await _factory.SeedProfileAsync(owner);
            using var client = _factory.ClientFor(owner);

            var first = await client.PutAsJsonAsync($"/api/profile/{section}", SectionBody(section, profileId, "first"));
            var second = await client.PutAsJsonAsync($"/api/profile/{section}", SectionBody(section, profileId, "second"));

            Assert.Equal(HttpStatusCode.OK, first.StatusCode);
            Assert.Equal(HttpStatusCode.OK, second.StatusCode);
            Assert.Equal(new[] { "second" }, (await ReadMarkersAsync(section, profileId)).ToArray()); // one row, updated in place
        }

        [Theory]
        [InlineData("family")]
        [InlineData("preferences")]
        public async Task Section_OnSomeoneElsesProfile_IsForbidden_AndChangesNothing(string section)
        {
            var victim = await _factory.CreateUserAsync();
            var attacker = await _factory.CreateUserAsync();
            var victimProfileId = await _factory.SeedProfileAsync(victim);
            await SeedSectionAsync(section, victimProfileId, "original");
            using var client = _factory.ClientFor(attacker);

            var response = await client.PutAsJsonAsync($"/api/profile/{section}", SectionBody(section, victimProfileId, "pwned"));

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Equal(new[] { "original" }, (await ReadMarkersAsync(section, victimProfileId)).ToArray());
        }

        [Theory]
        [InlineData("family")]
        [InlineData("preferences")]
        public async Task Section_ForAnUnknownProfile_Returns404_AndStoresNothing(string section)
        {
            var member = await _factory.CreateUserAsync();
            var unknownProfileId = Guid.NewGuid();
            using var client = _factory.ClientFor(member);

            var response = await client.PutAsJsonAsync($"/api/profile/{section}", SectionBody(section, unknownProfileId, "orphan"));

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Empty(await ReadMarkersAsync(section, unknownProfileId));
        }

        [Theory]
        [InlineData("family")]
        [InlineData("preferences")]
        public async Task Section_ByStaff_CanEditAnyMembersProfile(string section)
        {
            var member = await _factory.CreateUserAsync();
            var staff = await _factory.CreateUserAsync(UserRole.Admin);
            var profileId = await _factory.SeedProfileAsync(member);
            using var client = _factory.ClientFor(staff);

            var response = await client.PutAsJsonAsync($"/api/profile/{section}", SectionBody(section, profileId, "by-staff"));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(new[] { "by-staff" }, (await ReadMarkersAsync(section, profileId)).ToArray());
        }
    }
}
