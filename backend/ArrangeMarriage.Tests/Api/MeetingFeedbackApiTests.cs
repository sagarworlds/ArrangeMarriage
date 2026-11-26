using System.Net;
using System.Net.Http.Json;
using ArrangeMarriage.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ArrangeMarriage.Tests.Api
{
    /// <summary>Only the two people who met (or the matchmaker on their behalf) may leave feedback, once each.</summary>
    public class MeetingFeedbackApiTests : IClassFixture<ApiFactory>
    {
        private readonly ApiFactory _factory;

        public MeetingFeedbackApiTests(ApiFactory factory) => _factory = factory;

        private sealed record MeetingScene(TestUser Sender, TestUser Receiver, Guid MeetingId);

        private async Task<MeetingScene> ArrangeMeetingAsync()
        {
            var sender = await _factory.CreateUserAsync();
            var receiver = await _factory.CreateUserAsync();
            var proposalId = await _factory.SeedProposalAsync(sender.UserId, receiver.UserId, ProposalStatus.MeetingArranged);
            return new MeetingScene(sender, receiver, await _factory.SeedMeetingAsync(proposalId));
        }

        private static object Feedback(Guid meetingId, Guid userId, int rating = 4) =>
            new { meetingId, userId, rating, comments = "A pleasant conversation" };

        private Task<List<MeetingFeedback>> FeedbackForAsync(Guid meetingId) =>
            _factory.WithDbAsync(db => db.MeetingFeedbacks.AsNoTracking().Where(f => f.MeetingId == meetingId).ToListAsync());

        [Fact]
        public async Task BothParticipants_CanLeaveFeedbackAboutTheirOwnMeeting()
        {
            var scene = await ArrangeMeetingAsync();
            using var asSender = _factory.ClientFor(scene.Sender);
            using var asReceiver = _factory.ClientFor(scene.Receiver);

            var fromSender = await asSender.PostAsJsonAsync("/api/meeting/feedback", Feedback(scene.MeetingId, scene.Sender.UserId, rating: 5));
            var fromReceiver = await asReceiver.PostAsJsonAsync("/api/meeting/feedback", Feedback(scene.MeetingId, scene.Receiver.UserId, rating: 3));

            Assert.Equal(HttpStatusCode.OK, fromSender.StatusCode);
            Assert.Equal(HttpStatusCode.OK, fromReceiver.StatusCode);
            var stored = await FeedbackForAsync(scene.MeetingId);
            Assert.Equal(2, stored.Count);
            Assert.Equal(5, stored.Single(f => f.UserId == scene.Sender.UserId).Rating);
        }

        [Fact]
        public async Task SomeoneWhoWasNotAtTheMeeting_CannotLeaveFeedback()
        {
            var scene = await ArrangeMeetingAsync();
            var stranger = await _factory.CreateUserAsync();
            using var client = _factory.ClientFor(stranger);

            var response = await client.PostAsJsonAsync("/api/meeting/feedback", Feedback(scene.MeetingId, stranger.UserId));

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Empty(await FeedbackForAsync(scene.MeetingId));
        }

        [Fact]
        public async Task AParticipant_CannotLeaveFeedbackInTheOtherParticipantsName()
        {
            var scene = await ArrangeMeetingAsync();
            using var asSender = _factory.ClientFor(scene.Sender);

            var response = await asSender.PostAsJsonAsync("/api/meeting/feedback", Feedback(scene.MeetingId, scene.Receiver.UserId, rating: 1));

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Empty(await FeedbackForAsync(scene.MeetingId));
        }

        [Fact]
        public async Task AStranger_CannotImpersonateAParticipant()
        {
            var scene = await ArrangeMeetingAsync();
            var stranger = await _factory.CreateUserAsync();
            using var client = _factory.ClientFor(stranger);

            var response = await client.PostAsJsonAsync("/api/meeting/feedback", Feedback(scene.MeetingId, scene.Sender.UserId, rating: 1));

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Empty(await FeedbackForAsync(scene.MeetingId));
        }

        [Fact]
        public async Task Staff_CanRecordFeedbackOnBehalfOfAParticipant_ButNotForAnOutsider()
        {
            var scene = await ArrangeMeetingAsync();
            var outsider = await _factory.CreateUserAsync();
            var staff = await _factory.CreateUserAsync(UserRole.Staff);
            using var client = _factory.ClientFor(staff);

            var forParticipant = await client.PostAsJsonAsync("/api/meeting/feedback", Feedback(scene.MeetingId, scene.Receiver.UserId));
            var forOutsider = await client.PostAsJsonAsync("/api/meeting/feedback", Feedback(scene.MeetingId, outsider.UserId));

            Assert.Equal(HttpStatusCode.OK, forParticipant.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, forOutsider.StatusCode);
            Assert.Equal(new[] { scene.Receiver.UserId }, (await FeedbackForAsync(scene.MeetingId)).Select(f => f.UserId).ToArray());
        }

        [Fact]
        public async Task FeedbackForAnUnknownMeeting_Returns404()
        {
            var member = await _factory.CreateUserAsync();
            using var client = _factory.ClientFor(member);

            var response = await client.PostAsJsonAsync("/api/meeting/feedback", Feedback(Guid.NewGuid(), member.UserId));

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(6)]
        [InlineData(-3)]
        public async Task ARatingOutsideOneToFive_IsRejected(int rating)
        {
            var scene = await ArrangeMeetingAsync();
            using var client = _factory.ClientFor(scene.Sender);

            var response = await client.PostAsJsonAsync("/api/meeting/feedback", Feedback(scene.MeetingId, scene.Sender.UserId, rating));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Empty(await FeedbackForAsync(scene.MeetingId));
        }

        [Fact]
        public async Task EachParticipant_CanLeaveFeedbackOnlyOnce()
        {
            var scene = await ArrangeMeetingAsync();
            using var client = _factory.ClientFor(scene.Sender);
            await client.PostAsJsonAsync("/api/meeting/feedback", Feedback(scene.MeetingId, scene.Sender.UserId, rating: 5));

            var again = await client.PostAsJsonAsync("/api/meeting/feedback", Feedback(scene.MeetingId, scene.Sender.UserId, rating: 1));

            Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
            var stored = await FeedbackForAsync(scene.MeetingId);
            Assert.Equal(5, Assert.Single(stored).Rating); // the original is kept
        }

        [Fact]
        public async Task Feedback_CannotSmuggleInAnAccount_ThroughANestedUser()
        {
            var scene = await ArrangeMeetingAsync();
            var smuggledId = Guid.NewGuid();
            using var client = _factory.ClientFor(scene.Sender);

            await client.PostAsJsonAsync("/api/meeting/feedback", new
            {
                meetingId = scene.MeetingId,
                userId = scene.Sender.UserId,
                rating = 4,
                user = new { userId = smuggledId, username = "smuggled-fb", email = "smuggled-fb@example.com", phoneNumber = "9888888888", passwordHash = "x", role = (int)UserRole.Admin, isVerified = true }
            });

            Assert.False(await _factory.WithDbAsync(db => db.Users.AnyAsync(u => u.UserId == smuggledId)));
        }
    }
}
