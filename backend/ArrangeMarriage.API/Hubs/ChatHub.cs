using System;
using System.Linq;
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

        /// <summary>
        /// Sends a message from the authenticated caller to <paramref name="receiverId"/>. The sender is always
        /// taken from the caller's token, never from client input.
        /// </summary>
        /// <param name="receiverId">Id of the receiving user (a GUID string).</param>
        /// <param name="content">Message text, 1 to <see cref="MaxMessageLength"/> characters.</param>
        /// <exception cref="HubException">The caller is unauthenticated, the receiver id is invalid, or the content is empty or too long.</exception>
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

            // Route by authenticated user id instead of hand-managed groups. SignalR registers a connection under
            // its user id as part of connecting, whereas joining a group in OnConnectedAsync runs *after* the
            // client's StartAsync has already completed, so a just-connected user could miss messages.
            // Distinct() so a message to oneself is delivered once, not twice.
            var recipients = new[] { senderGuid.ToString(), receiverGuid.ToString() }.Distinct().ToList();
            await Clients.Users(recipients).SendAsync("ReceiveMessage", payload);
        }
    }
}
