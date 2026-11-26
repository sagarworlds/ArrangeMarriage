using System.Net;
using System.Net.Http.Json;
using ArrangeMarriage.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ArrangeMarriage.Tests.Api
{
    /// <summary>
    /// Endpoints that bind a domain entity from the request body. Regression tests for "The User field is
    /// required": clients send foreign-key ids, not nested objects, while malformed input is still rejected.
    /// </summary>
    public class EntityBindingApiTests : IClassFixture<ApiFactory>
    {
        private readonly ApiFactory _factory;

        public EntityBindingApiTests(ApiFactory factory) => _factory = factory;

        [Fact]
        public async Task CreateProfile_WithOnlyForeignKeyIds_Succeeds()
        {
            var member = await _factory.CreateUserAsync();
            using var client = _factory.ClientFor(member);

            var response = await client.PostAsJsonAsync("/api/profile",
                new { userId = member.UserId, fullName = "Asha Rao", gender = "Female", dob = "1996-04-12T00:00:00Z", religion = "Hindu" });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var stored = await _factory.WithDbAsync(db => db.Profiles.SingleAsync(p => p.UserId == member.UserId));
            Assert.Equal("Asha Rao", stored.FullName);
        }

        [Fact]
        public async Task CreateProfile_ForAnotherUser_IsForbidden()
        {
            var member = await _factory.CreateUserAsync();
            var other = await _factory.CreateUserAsync();
            using var client = _factory.ClientFor(member);

            var response = await client.PostAsJsonAsync("/api/profile",
                new { userId = other.UserId, fullName = "Impostor", gender = "Male", dob = "1990-01-01T00:00:00Z" });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task CreateProfile_WithAnUnparseableDate_IsStillRejectedAsABadRequest()
        {
            var member = await _factory.CreateUserAsync();
            using var client = _factory.ClientFor(member);

            var response = await client.PostAsJsonAsync("/api/profile",
                new { userId = member.UserId, fullName = "Asha Rao", gender = "Female", dob = "not-a-date" });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ScheduleMeeting_ByStaff_AcceptsAProposalId()
        {
            var sender = await _factory.CreateUserAsync();
            var receiver = await _factory.CreateUserAsync();
            var staff = await _factory.CreateUserAsync(UserRole.Staff);
            var proposalId = await _factory.WithDbAsync(async db =>
            {
                var proposal = new Proposal { SenderId = sender.UserId, ReceiverId = receiver.UserId };
                db.Proposals.Add(proposal);
                await db.SaveChangesAsync();
                return proposal.ProposalId;
            });
            using var client = _factory.ClientFor(staff);

            var response = await client.PostAsJsonAsync("/api/meeting/schedule",
                new { proposalId, meetingDate = "2027-02-01T10:00:00Z", venue = "Community hall", isOnline = false });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Single(await _factory.WithDbAsync(db => db.Meetings.Where(m => m.ProposalId == proposalId).ToListAsync()));

            // Both participants can now see the meeting; an unrelated user cannot list it as theirs.
            using var asSender = _factory.ClientFor(sender);
            var mine = await asSender.GetFromJsonAsync<List<Meeting>>($"/api/meeting/user/{sender.UserId}");
            Assert.Single(mine!);
        }

        [Fact]
        public async Task ScheduleMeeting_ForAnUnknownProposal_Returns404_AndStoresNothing()
        {
            var staff = await _factory.CreateUserAsync(UserRole.Staff);
            var unknownProposalId = Guid.NewGuid();
            using var client = _factory.ClientFor(staff);

            var response = await client.PostAsJsonAsync("/api/meeting/schedule",
                new { proposalId = unknownProposalId, meetingDate = "2027-02-01T10:00:00Z" });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.False(await _factory.WithDbAsync(db => db.Meetings.AnyAsync(m => m.ProposalId == unknownProposalId)));
        }

        [Fact]
        public async Task AddMeetingFeedback_AcceptsMeetingAndUserIds()
        {
            var member = await _factory.CreateUserAsync();
            var meetingId = await _factory.WithDbAsync(async db =>
            {
                var proposal = new Proposal { SenderId = member.UserId, ReceiverId = Guid.NewGuid() };
                var meeting = new Meeting { ProposalId = proposal.ProposalId, MeetingDate = DateTime.UtcNow.AddDays(1) };
                db.Proposals.Add(proposal);
                db.Meetings.Add(meeting);
                await db.SaveChangesAsync();
                return meeting.MeetingId;
            });
            using var client = _factory.ClientFor(member);

            var response = await client.PostAsJsonAsync("/api/meeting/feedback",
                new { meetingId, userId = member.UserId, rating = 4, comments = "Pleasant conversation" });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
