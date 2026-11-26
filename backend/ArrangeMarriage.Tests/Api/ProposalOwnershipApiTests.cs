using System.Net;
using System.Net.Http.Json;
using ArrangeMarriage.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ArrangeMarriage.Tests.Api
{
    /// <summary>Who may send a proposal, to whom, and who may respond to it.</summary>
    public class ProposalOwnershipApiTests : IClassFixture<ApiFactory>
    {
        private readonly ApiFactory _factory;

        public ProposalOwnershipApiTests(ApiFactory factory) => _factory = factory;

        private Task<ProposalStatus> StatusOfAsync(Guid proposalId) =>
            _factory.WithDbAsync(db => db.Proposals.AsNoTracking().Where(p => p.ProposalId == proposalId).Select(p => p.Status).SingleAsync());

        private static object Status(Guid proposalId, ProposalStatus status) => new { proposalId, status };

        // ---- sending ----------------------------------------------------------------------------------------------

        [Fact]
        public async Task Propose_ToARealUser_RecordsASentProposalFromTheCaller()
        {
            var sender = await _factory.CreateUserAsync();
            var receiver = await _factory.CreateUserAsync();
            using var client = _factory.ClientFor(sender);

            var response = await client.PostAsJsonAsync("/api/matchmaking/propose",
                new { senderId = sender.UserId, receiverId = receiver.UserId, notes = "Our families know each other" });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var stored = await _factory.WithDbAsync(db => db.Proposals.AsNoTracking().SingleAsync(p => p.SenderId == sender.UserId));
            Assert.Equal(receiver.UserId, stored.ReceiverId);
            Assert.Equal(ProposalStatus.Sent, stored.Status);
        }

        [Fact]
        public async Task Propose_OnBehalfOfAnotherUser_IsForbidden()
        {
            var victim = await _factory.CreateUserAsync();
            var attacker = await _factory.CreateUserAsync();
            var receiver = await _factory.CreateUserAsync();
            using var client = _factory.ClientFor(attacker);

            var response = await client.PostAsJsonAsync("/api/matchmaking/propose",
                new { senderId = victim.UserId, receiverId = receiver.UserId });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.False(await _factory.WithDbAsync(db => db.Proposals.AnyAsync(p => p.SenderId == victim.UserId)));
        }

        [Fact]
        public async Task Propose_ToAnUnknownUser_Returns404_AndStoresNothing()
        {
            var sender = await _factory.CreateUserAsync();
            using var client = _factory.ClientFor(sender);

            var response = await client.PostAsJsonAsync("/api/matchmaking/propose", new { senderId = sender.UserId, receiverId = Guid.NewGuid() });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.False(await _factory.WithDbAsync(db => db.Proposals.AnyAsync(p => p.SenderId == sender.UserId)));
        }

        [Fact]
        public async Task Propose_ToYourself_Returns400()
        {
            var sender = await _factory.CreateUserAsync();
            using var client = _factory.ClientFor(sender);

            var response = await client.PostAsJsonAsync("/api/matchmaking/propose", new { senderId = sender.UserId, receiverId = sender.UserId });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        // ---- responding -------------------------------------------------------------------------------------------

        [Theory]
        [InlineData(ProposalStatus.Viewed)]
        [InlineData(ProposalStatus.Accepted)]
        [InlineData(ProposalStatus.Rejected)]
        public async Task TheReceiver_CanRespondToAProposalAddressedToThem(ProposalStatus response)
        {
            var sender = await _factory.CreateUserAsync();
            var receiver = await _factory.CreateUserAsync();
            var proposalId = await _factory.SeedProposalAsync(sender.UserId, receiver.UserId);
            using var client = _factory.ClientFor(receiver);

            var result = await client.PutAsJsonAsync("/api/matchmaking/status", Status(proposalId, response));

            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            Assert.Equal(response, await StatusOfAsync(proposalId));
        }

        [Theory]
        [InlineData(ProposalStatus.MeetingArranged)]
        [InlineData(ProposalStatus.Finalized)]
        public async Task TheReceiver_CannotSetTheStatusesOnlyTheMatchmakerControls(ProposalStatus staffOnly)
        {
            var sender = await _factory.CreateUserAsync();
            var receiver = await _factory.CreateUserAsync();
            var proposalId = await _factory.SeedProposalAsync(sender.UserId, receiver.UserId);
            using var client = _factory.ClientFor(receiver);

            var result = await client.PutAsJsonAsync("/api/matchmaking/status", Status(proposalId, staffOnly));

            Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
            Assert.Equal(ProposalStatus.Sent, await StatusOfAsync(proposalId));
        }

        [Theory]
        [InlineData(ProposalStatus.MeetingArranged)]
        [InlineData(ProposalStatus.Finalized)]
        public async Task TheReceiver_CannotUndoAStageTheMatchmakerHasAlreadyReached(ProposalStatus current)
        {
            var sender = await _factory.CreateUserAsync();
            var receiver = await _factory.CreateUserAsync();
            var proposalId = await _factory.SeedProposalAsync(sender.UserId, receiver.UserId, current);
            using var client = _factory.ClientFor(receiver);

            var result = await client.PutAsJsonAsync("/api/matchmaking/status", Status(proposalId, ProposalStatus.Rejected));

            Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
            Assert.Equal(current, await StatusOfAsync(proposalId));
        }

        [Fact]
        public async Task TheSender_CannotChangeTheStatusOfTheirOwnProposal()
        {
            var sender = await _factory.CreateUserAsync();
            var receiver = await _factory.CreateUserAsync();
            var proposalId = await _factory.SeedProposalAsync(sender.UserId, receiver.UserId);
            using var client = _factory.ClientFor(sender);

            // Otherwise a sender could mark their own proposal "Accepted" on the receiver's behalf.
            var result = await client.PutAsJsonAsync("/api/matchmaking/status", Status(proposalId, ProposalStatus.Accepted));

            Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
            Assert.Equal(ProposalStatus.Sent, await StatusOfAsync(proposalId));
        }

        [Fact]
        public async Task AnUnrelatedUser_CannotChangeAProposal()
        {
            var sender = await _factory.CreateUserAsync();
            var receiver = await _factory.CreateUserAsync();
            var stranger = await _factory.CreateUserAsync();
            var proposalId = await _factory.SeedProposalAsync(sender.UserId, receiver.UserId);
            using var client = _factory.ClientFor(stranger);

            var result = await client.PutAsJsonAsync("/api/matchmaking/status", Status(proposalId, ProposalStatus.Rejected));

            Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
            Assert.Equal(ProposalStatus.Sent, await StatusOfAsync(proposalId));
        }

        [Fact]
        public async Task Staff_CanSetAnyStatusOnAnyProposal()
        {
            var sender = await _factory.CreateUserAsync();
            var receiver = await _factory.CreateUserAsync();
            var staff = await _factory.CreateUserAsync(UserRole.Staff);
            var proposalId = await _factory.SeedProposalAsync(sender.UserId, receiver.UserId, ProposalStatus.Accepted);
            using var client = _factory.ClientFor(staff);

            var result = await client.PutAsJsonAsync("/api/matchmaking/status", Status(proposalId, ProposalStatus.MeetingArranged));

            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            Assert.Equal(ProposalStatus.MeetingArranged, await StatusOfAsync(proposalId));
        }

        [Fact]
        public async Task UpdatingAnUnknownProposal_Returns404()
        {
            var member = await _factory.CreateUserAsync();
            using var client = _factory.ClientFor(member);

            var result = await client.PutAsJsonAsync("/api/matchmaking/status", Status(Guid.NewGuid(), ProposalStatus.Accepted));

            Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
        }
    }
}
