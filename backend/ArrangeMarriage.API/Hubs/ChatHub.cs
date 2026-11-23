using System;
using System.Threading.Tasks;
using ArrangeMarriage.API.Security;
using ArrangeMarriage.Domain.Entities;
using ArrangeMarriage.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ArrangeMarriage.API.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private const int MaxMessageLength = 2000;
        private readonly ApplicationDbContext _context;

        public ChatHub(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>Joins the caller to their own group, derived from the token so nobody can listen on another user's group.</summary>
        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.GetUserId() ?? throw new HubException("Unauthenticated.");
            await Groups.AddToGroupAsync(Context.ConnectionId, userId.ToString());
            await base.OnConnectedAsync();
        }

        /// <summary>Sends a message from the authenticated caller (sender is never taken from the client).</summary>
        public async Task SendMessage(string receiverId, string content)
        {
            var senderGuid = Context.User?.GetUserId() ?? throw new HubException("Unauthenticated.");

            if (!Guid.TryParse(receiverId, out var receiverGuid))
                throw new HubException("Invalid receiver id.");
            if (string.IsNullOrWhiteSpace(content) || content.Length > MaxMessageLength)
                throw new HubException($"Message must be 1-{MaxMessageLength} characters.");

            var message = new Message
            {
                SenderId = senderGuid,
                ReceiverId = receiverGuid,
                Content = content,
                CreatedAt = DateTime.UtcNow
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            // Project to a plain shape so the broadcast doesn't serialize navigation properties.
            var payload = new { message.MessageId, message.SenderId, message.ReceiverId, message.Content, message.CreatedAt };
            await Clients.Group(senderGuid.ToString()).SendAsync("ReceiveMessage", payload);
            await Clients.Group(receiverGuid.ToString()).SendAsync("ReceiveMessage", payload);
        }
    }
}
