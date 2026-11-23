using System;
using System.Linq;
using System.Threading.Tasks;
using ArrangeMarriage.API.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ArrangeMarriage.Infrastructure.Persistence;

namespace ArrangeMarriage.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class MessageController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public MessageController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("history/{userA}/{userB}")]
        public async Task<IActionResult> GetChatHistory(Guid userA, Guid userB)
        {
            // Only participants (or staff) may read a conversation.
            var me = User.GetUserId();
            if (!User.IsStaff() && me != userA && me != userB) return Forbid();

            var history = await _context.Messages
                .Where(m => (m.SenderId == userA && m.ReceiverId == userB)
                         || (m.SenderId == userB && m.ReceiverId == userA))
                .OrderBy(m => m.CreatedAt)
                .Select(m => new
                {
                    m.MessageId,
                    m.SenderId,
                    m.ReceiverId,
                    m.Content,
                    m.CreatedAt
                })
                .ToListAsync();

            return Ok(history);
        }
    }
}
