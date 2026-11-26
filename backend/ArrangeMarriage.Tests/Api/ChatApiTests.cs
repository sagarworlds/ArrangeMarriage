using System.Net;
using System.Net.Http.Json;
using ArrangeMarriage.Domain.Entities;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;

namespace ArrangeMarriage.Tests.Api
{
    /// <summary>Chat history over REST and the authenticated SignalR hub.</summary>
    public class ChatApiTests : IClassFixture<ApiFactory>
    {
        private sealed record ReceivedMessage(Guid MessageId, Guid SenderId, Guid ReceiverId, string Content, DateTime CreatedAt);

        private readonly ApiFactory _factory;

        public ChatApiTests(ApiFactory factory) => _factory = factory;

        private HubConnection HubFor(TestUser? user)
        {
            var server = _factory.Server; // forces the in-memory host to start
            return new HubConnectionBuilder()
                .WithUrl(new Uri(server.BaseAddress, "/chatHub"), options =>
                {
                    options.HttpMessageHandlerFactory = _ => server.CreateHandler();
                    options.Transports = HttpTransportType.LongPolling; // WebSockets need a real socket; long polling works in-memory
                    if (user != null) options.AccessTokenProvider = () => Task.FromResult<string?>(user.Token);
                })
                .Build();
        }

        private const string ReadyPing = "__ready__";

        /// <summary>
        /// Connects and returns once the connection is fully set up. SignalR completes StartAsync when the
        /// handshake succeeds, which can be before the server has finished registering the connection, so a
        /// message sent immediately afterwards could miss it. A message to oneself is only processed after
        /// that setup, so seeing its echo proves the connection is ready; the echo is then hidden from the test.
        /// </summary>
        private async Task<HubConnection> ConnectAsync(TestUser user, Action<ReceivedMessage> onMessage)
        {
            var connection = HubFor(user);
            var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            connection.On<ReceivedMessage>("ReceiveMessage", m =>
            {
                if (m.Content == ReadyPing) ready.TrySetResult();
                else onMessage(m);
            });

            await connection.StartAsync();
            await connection.InvokeAsync("SendMessage", user.UserId.ToString(), ReadyPing);
            await ready.Task.WaitAsync(TimeSpan.FromSeconds(10));
            return connection;
        }

        private async Task SeedMessageAsync(Guid from, Guid to, string content, int minutesAgo)
        {
            await _factory.WithDbAsync(async db =>
            {
                db.Messages.Add(new Message { SenderId = from, ReceiverId = to, Content = content, CreatedAt = DateTime.UtcNow.AddMinutes(-minutesAgo) });
                await db.SaveChangesAsync();
                return 0;
            });
        }

        // ---- REST history -------------------------------------------------------------------------------------

        [Fact]
        public async Task History_IsVisibleToParticipantsAndStaff_ButNotToOthers_AndIsOrderedOldestFirst()
        {
            var alice = await _factory.CreateUserAsync();
            var bob = await _factory.CreateUserAsync();
            var eve = await _factory.CreateUserAsync();
            var staff = await _factory.CreateUserAsync(UserRole.Admin);
            await SeedMessageAsync(bob.UserId, alice.UserId, "second", minutesAgo: 1);
            await SeedMessageAsync(alice.UserId, bob.UserId, "first", minutesAgo: 5);

            using var asAlice = _factory.ClientFor(alice);
            using var asBob = _factory.ClientFor(bob);
            using var asEve = _factory.ClientFor(eve);
            using var asStaff = _factory.ClientFor(staff);
            var url = $"/api/message/history/{alice.UserId}/{bob.UserId}";

            var history = await asAlice.GetFromJsonAsync<List<ReceivedMessage>>(url);
            Assert.Equal(new[] { "first", "second" }, history!.Select(m => m.Content).ToArray());
            Assert.Equal(HttpStatusCode.OK, (await asBob.GetAsync(url)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await asStaff.GetAsync(url)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await asEve.GetAsync(url)).StatusCode);
        }

        // ---- SignalR hub --------------------------------------------------------------------------------------

        [Fact]
        public async Task Hub_RejectsConnectionsWithoutAToken()
        {
            await using var connection = HubFor(null);

            await Assert.ThrowsAsync<HttpRequestException>(() => connection.StartAsync());
        }

        [Fact]
        public async Task Hub_DeliversMessagesToTheReceiver_WithTheSenderTakenFromTheToken_AndPersistsThem()
        {
            var alice = await _factory.CreateUserAsync();
            var bob = await _factory.CreateUserAsync();
            var delivered = new TaskCompletionSource<ReceivedMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
            await using var bobHub = await ConnectAsync(bob, m => delivered.TrySetResult(m));
            await using var aliceHub = await ConnectAsync(alice, _ => { });

            await aliceHub.InvokeAsync("SendMessage", bob.UserId.ToString(), "hello bob");

            var received = await delivered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.Equal(alice.UserId, received.SenderId); // not client-supplied: the hub has no sender parameter
            Assert.Equal(bob.UserId, received.ReceiverId);
            Assert.Equal("hello bob", received.Content);

            var stored = await _factory.WithDbAsync(db => db.Messages.SingleAsync(m => m.MessageId == received.MessageId));
            Assert.Equal(alice.UserId, stored.SenderId);
        }

        [Fact]
        public async Task Hub_DeliversToAReceiverWhoseServerSideConnectIsStillRunning()
        {
            // Regression: the hub used to add users to a group in OnConnectedAsync. SignalR finishes the client's
            // StartAsync at handshake, before that runs, so a message sent right after a user connected could be lost.
            var alice = await _factory.CreateUserAsync();
            var bob = await _factory.CreateUserAsync();
            _factory.SlowConnectUsers[bob.UserId.ToString()] = TimeSpan.FromSeconds(2);

            var delivered = new TaskCompletionSource<ReceivedMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
            await using var bobHub = HubFor(bob);
            bobHub.On<ReceivedMessage>("ReceiveMessage", m => delivered.TrySetResult(m));
            await bobHub.StartAsync(); // returns while Bob's server-side connect work is still pending
            await using var aliceHub = await ConnectAsync(alice, _ => { });

            await aliceHub.InvokeAsync("SendMessage", bob.UserId.ToString(), "right after connecting");

            var received = await delivered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.Equal("right after connecting", received.Content);
        }

        [Fact]
        public async Task Hub_SenderAlsoReceivesTheirOwnMessage_ButAThirdUserDoesNot()
        {
            var alice = await _factory.CreateUserAsync();
            var bob = await _factory.CreateUserAsync();
            var eve = await _factory.CreateUserAsync();
            var aliceEcho = new TaskCompletionSource<ReceivedMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
            var bobReceived = new TaskCompletionSource<ReceivedMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
            var eveReceived = new List<ReceivedMessage>();
            await using var aliceHub = await ConnectAsync(alice, m => aliceEcho.TrySetResult(m));
            await using var bobHub = await ConnectAsync(bob, m => bobReceived.TrySetResult(m));
            // Eve is fully connected too, so an empty list really means the message wasn't routed to her.
            await using var eveHub = await ConnectAsync(eve, m => { lock (eveReceived) eveReceived.Add(m); });

            await aliceHub.InvokeAsync("SendMessage", bob.UserId.ToString(), "private");
            await Task.WhenAll(aliceEcho.Task, bobReceived.Task).WaitAsync(TimeSpan.FromSeconds(10));
            await Task.Delay(300); // give a mis-routed copy a chance to arrive before asserting its absence

            lock (eveReceived) Assert.Empty(eveReceived);
        }

        [Theory]
        [InlineData("not-a-guid", "hi")]
        [InlineData("00000000-0000-0000-0000-000000000001", "")]
        [InlineData("00000000-0000-0000-0000-000000000001", "   ")]
        public async Task Hub_InvalidInput_IsRejectedWithAClearError_AndNothingIsStored(string receiverId, string content)
        {
            var alice = await _factory.CreateUserAsync();
            await using var aliceHub = HubFor(alice);
            await aliceHub.StartAsync();
            var before = await _factory.WithDbAsync(db => db.Messages.CountAsync(m => m.SenderId == alice.UserId));

            await Assert.ThrowsAsync<HubException>(() => aliceHub.InvokeAsync("SendMessage", receiverId, content));

            Assert.Equal(before, await _factory.WithDbAsync(db => db.Messages.CountAsync(m => m.SenderId == alice.UserId)));
        }
    }
}
