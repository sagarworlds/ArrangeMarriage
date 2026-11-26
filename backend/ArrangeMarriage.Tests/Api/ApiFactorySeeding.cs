using ArrangeMarriage.Domain.Entities;

namespace ArrangeMarriage.Tests.Api
{
    /// <summary>Direct-to-database test data, so ownership tests can set up "someone else's" records without going through the API.</summary>
    public static class ApiFactorySeeding
    {
        public static Task<Guid> SeedProfileAsync(this ApiFactory factory, TestUser owner, string fullName = "Seeded Member", string? city = "Pune") =>
            factory.WithDbAsync(async db =>
            {
                var profile = new Profile { UserId = owner.UserId, FullName = fullName, Gender = "Female", Dob = new DateTime(1996, 4, 12), City = city };
                db.Profiles.Add(profile);
                await db.SaveChangesAsync();
                return profile.ProfileId;
            });

        public static Task<Guid> SeedFamilyAsync(this ApiFactory factory, Guid profileId, string fatherName) =>
            factory.WithDbAsync(async db =>
            {
                var detail = new FamilyDetail { ProfileId = profileId, FatherName = fatherName };
                db.FamilyDetails.Add(detail);
                await db.SaveChangesAsync();
                return detail.FamilyDetailId;
            });

        public static Task<Guid> SeedPreferenceAsync(this ApiFactory factory, Guid profileId, string religion) =>
            factory.WithDbAsync(async db =>
            {
                var preference = new PartnerPreference { ProfileId = profileId, PreferredReligion = religion };
                db.PartnerPreferences.Add(preference);
                await db.SaveChangesAsync();
                return preference.PreferenceId;
            });

        public static Task<Guid> SeedProposalAsync(this ApiFactory factory, Guid senderId, Guid receiverId, ProposalStatus status = ProposalStatus.Sent) =>
            factory.WithDbAsync(async db =>
            {
                var proposal = new Proposal { SenderId = senderId, ReceiverId = receiverId, Status = status };
                db.Proposals.Add(proposal);
                await db.SaveChangesAsync();
                return proposal.ProposalId;
            });

        public static Task<Guid> SeedMeetingAsync(this ApiFactory factory, Guid proposalId) =>
            factory.WithDbAsync(async db =>
            {
                var meeting = new Meeting { ProposalId = proposalId, MeetingDate = DateTime.UtcNow.AddDays(3), Venue = "Community hall" };
                db.Meetings.Add(meeting);
                await db.SaveChangesAsync();
                return meeting.MeetingId;
            });
    }
}
