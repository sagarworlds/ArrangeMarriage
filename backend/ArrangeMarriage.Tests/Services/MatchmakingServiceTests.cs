using ArrangeMarriage.Domain.Entities;
using ArrangeMarriage.Infrastructure.Persistence;
using ArrangeMarriage.Infrastructure.Services;
using ArrangeMarriage.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace ArrangeMarriage.Tests.Services
{
    public class MatchmakingServiceTests
    {
        private static readonly DateTime Today = DateTime.UtcNow.Date;

        private static MatchmakingService NewService(ApplicationDbContext context) =>
            new(context, new AstroService());

        /// <summary>Adds a user with a profile born so that they are exactly <paramref name="ageYears"/> today, plus days offset.</summary>
        private static Profile AddProfile(ApplicationDbContext context, string name, string gender, int ageYears,
            int extraDaysOld = 0, string? religion = "Hindu", string? caste = "Brahmin")
        {
            var user = new User { Username = name, Email = $"{name}@example.com", PhoneNumber = name };
            var profile = new Profile
            {
                UserId = user.UserId,
                FullName = name,
                Gender = gender,
                Dob = Today.AddYears(-ageYears).AddDays(-extraDaysOld),
                Religion = religion,
                CasteCommunity = caste
            };
            context.Users.Add(user);
            context.Profiles.Add(profile);
            return profile;
        }

        private static void AddPreference(ApplicationDbContext context, Profile owner, int? minAge, int? maxAge,
            string? religion = null, string? caste = null)
        {
            context.PartnerPreferences.Add(new PartnerPreference
            {
                ProfileId = owner.ProfileId,
                MinAge = minAge,
                MaxAge = maxAge,
                PreferredReligion = religion,
                PreferredCaste = caste
            });
        }

        [Fact]
        public async Task SearchMatchesAsync_AppliesInclusiveAgeRangeAroundBirthdays()
        {
            await using var context = TestDb.Create();
            var seeker = AddProfile(context, "seeker", "Female", 28);
            AddPreference(context, seeker, minAge: 25, maxAge: 30);

            AddProfile(context, "age24", "Male", 24, extraDaysOld: 364);      // turns 25 tomorrow => still 24
            AddProfile(context, "age25-today", "Male", 25);                   // 25th birthday today => in range
            AddProfile(context, "age30", "Male", 30);
            AddProfile(context, "age30-31tomorrow", "Male", 31, extraDaysOld: -1); // turns 31 tomorrow => still 30
            AddProfile(context, "age31-today", "Male", 31);                   // 31st birthday today => out of range
            await context.SaveChangesAsync();

            var matches = (await NewService(context).SearchMatchesAsync(seeker.UserId)).Select(m => m.FullName).ToList();

            Assert.Equal(
                new[] { "age25-today", "age30", "age30-31tomorrow" },
                matches.OrderBy(n => n).ToArray());
        }

        [Fact]
        public async Task SearchMatchesAsync_ExcludesSelfAndSameGender()
        {
            await using var context = TestDb.Create();
            var seeker = AddProfile(context, "seeker", "Female", 28);
            AddPreference(context, seeker, minAge: null, maxAge: null);
            AddProfile(context, "sister", "Female", 28);
            AddProfile(context, "groom", "Male", 30);
            await context.SaveChangesAsync();

            var matches = (await NewService(context).SearchMatchesAsync(seeker.UserId)).ToList();

            Assert.Equal(new[] { "groom" }, matches.Select(m => m.FullName).ToArray());
        }

        [Fact]
        public async Task SearchMatchesAsync_ScoresAndOrdersByPreferenceMatch_WithoutTrackingResults()
        {
            await using var context = TestDb.Create();
            var seeker = AddProfile(context, "seeker", "Female", 28);
            AddPreference(context, seeker, minAge: null, maxAge: null, religion: "Hindu", caste: "Brahmin");
            AddProfile(context, "mismatch", "Male", 30, religion: "Other", caste: "Other");
            AddProfile(context, "perfect", "Male", 30, religion: "Hindu", caste: "Brahmin");
            await context.SaveChangesAsync();

            var matches = (await NewService(context).SearchMatchesAsync(seeker.UserId)).ToList();

            Assert.Equal(new[] { "perfect", "mismatch" }, matches.Select(m => m.FullName).ToArray());
            Assert.True(matches[0].MatchScore > matches[1].MatchScore);
            Assert.All(matches, m => Assert.InRange(m.HoroscopeMatchScore!.Value, 14, 36));
            // Scores are transient view data: the result entities must not be tracked (and so never saved).
            Assert.All(matches, m => Assert.Equal(EntityState.Detached, context.Entry(m).State));
        }

        [Fact]
        public async Task SearchMatchesAsync_WithoutProfileOrPreferences_ReturnsEmpty()
        {
            await using var context = TestDb.Create();
            var seekerWithoutPrefs = AddProfile(context, "seeker", "Female", 28);
            AddProfile(context, "groom", "Male", 30);
            await context.SaveChangesAsync();
            var service = NewService(context);

            Assert.Empty(await service.SearchMatchesAsync(Guid.NewGuid()));
            Assert.Empty(await service.SearchMatchesAsync(seekerWithoutPrefs.UserId));
        }
    }
}
