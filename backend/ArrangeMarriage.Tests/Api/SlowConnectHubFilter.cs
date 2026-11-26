using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;

namespace ArrangeMarriage.Tests.Api
{
    /// <summary>
    /// Test-only hub filter that delays the server-side connect work (the hub's OnConnectedAsync) for chosen users.
    /// Simulates a busy server: the client's StartAsync completes at handshake, before this work finishes.
    /// </summary>
    public sealed class SlowConnectHubFilter : IHubFilter
    {
        private readonly ConcurrentDictionary<string, TimeSpan> _delays;

        public SlowConnectHubFilter(ConcurrentDictionary<string, TimeSpan> delays) => _delays = delays;

        public async Task OnConnectedAsync(HubLifetimeContext context, Func<HubLifetimeContext, Task> next)
        {
            var userId = context.Context.UserIdentifier;
            if (userId != null && _delays.TryGetValue(userId, out var delay))
            {
                await Task.Delay(delay);
            }

            await next(context);
        }
    }
}
