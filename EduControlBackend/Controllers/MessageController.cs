using EduControlBackend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace EduControlBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Защита всех методов контроллера
    public class MessageController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public MessageController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageDto dto)
        {
            // Получаем id текущего пользователя из токена
            var senderId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            if (senderId != dto.SenderId)
                return Forbid();

            var receiver = await _context.Users.FindAsync(dto.ReceiverId);
            if (receiver == null)
                return BadRequest("Получатель не найден");

            var message = new Message
            {
                SenderId = senderId,
                ReceiverId = dto.ReceiverId,
                Content = dto.Content,
                Timestamp = DateTime.UtcNow
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            return Ok("Сообщение отправлено");
        }

        // Получить все сообщения между двумя пользователями
        [HttpGet("between/{userId}")]
        public async Task<IActionResult> GetMessagesBetween(int userId)
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var messages = await _context.Messages
                .Where(m =>
                    (m.SenderId == currentUserId && m.ReceiverId == userId) ||
                    (m.SenderId == userId && m.ReceiverId == currentUserId))
                .OrderBy(m => m.Timestamp)
                .ToListAsync();

            return Ok(messages);
        }

        // Получить все сообщения, где пользователь отправитель или получатель
        [HttpGet("my")]
        public async Task<IActionResult> GetMyMessages()
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var messages = await _context.Messages
                .Where(m => m.SenderId == currentUserId || m.ReceiverId == currentUserId)
                .OrderByDescending(m => m.Timestamp)
                .ToListAsync();

            return Ok(messages);
        }
    }

    public class SendMessageDto
    {
        public int SenderId { get; set; }
        public int ReceiverId { get; set; }
        public string Content { get; set; }
    }
}