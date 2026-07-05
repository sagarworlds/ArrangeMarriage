using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using ArrangeMarriage.Domain.Entities;
using ArrangeMarriage.Infrastructure.Persistence;

namespace ArrangeMarriage.API.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ApplicationDbContext _context;

        public ChatHub(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task JoinChat(string userId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, userId);
        }

        public async Task SendMessage(string senderId, string receiverId, string content)
        {
            var senderGuid = Guid.Parse(senderId);
            var receiverGuid = Guid.Parse(receiverId);

            var message = new Message
            {
                SenderId = senderGuid,
                ReceiverId = receiverGuid,
                Content = content,
                CreatedAt = DateTime.UtcNow
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            // Broadcast message to sender and receiver groups
            await Clients.Group(senderId).SendAsync("ReceiveMessage", message);
            await Clients.Group(receiverId).SendAsync("ReceiveMessage", message);
        }
    }
}
