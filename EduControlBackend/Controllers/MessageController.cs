using EduControlBackend.Models;
using EduControlBackend.Services;
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
        private readonly FileService _fileService;

        public MessageController(ApplicationDbContext context, FileService fileService)
        {
            _context = context;
            _fileService = fileService;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromForm] SendMessageDto dto)
        {
            try
            {
                // Получаем id текущего пользователя из токена
                var senderId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                if (senderId != dto.SenderId)
                    return Forbid();

                var receiver = await _context.Users.FindAsync(dto.ReceiverId);
                if (receiver == null)
                    return BadRequest("Получатель не найден");

                // Проверка размера файла (например, 100 МБ)
                if (dto.Attachment != null && dto.Attachment.Length > 104857600)
                {
                    return BadRequest("Файл слишком большой. Максимальный размер: 100 МБ");
                }

                var message = new Message
                {
                    SenderId = senderId,
                    ReceiverId = dto.ReceiverId,
                    Content = dto.Content,
                    Timestamp = DateTime.UtcNow
                };

                if (dto.Attachment != null)
                {
                    // Проверка типа файла (опционально)
                    var allowedTypes = new[] { ".jpg", ".jpeg", ".png", ".pdf", ".doc", ".docx", ".txt" };
                    var extension = Path.GetExtension(dto.Attachment.FileName).ToLowerInvariant();
                    if (!allowedTypes.Contains(extension))
                    {
                        return BadRequest("Неподдерживаемый тип файла");
                    }

                    var (path, fileName) = await _fileService.SaveFileAsync(dto.Attachment);
                    message.AttachmentPath = path;
                    message.AttachmentName = fileName;
                    message.AttachmentType = dto.Attachment.ContentType;
                }

                _context.Messages.Add(message);
                await _context.SaveChangesAsync();

                return Ok("Сообщение отправлено");
            }
            catch (Exception ex)
            {
                // Логирование ошибки
                Console.WriteLine($"Error in SendMessage: {ex}");
                return StatusCode(500, "Произошла ошибка при отправке сообщения");
            }
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

        [HttpGet("file/{messageId}")]
        public async Task<IActionResult> GetFile(int messageId)
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        
            var message = await _context.Messages
                .FirstOrDefaultAsync(m => m.Id == messageId && 
                    (m.SenderId == currentUserId || m.ReceiverId == currentUserId));

            if (message == null || string.IsNullOrEmpty(message.AttachmentPath))
                return NotFound();

            try
            {
                var fileBytes = await _fileService.GetFileAsync(message.AttachmentPath);
                return File(fileBytes, message.AttachmentType ?? "application/octet-stream", 
                    message.AttachmentName);
            }
            catch (FileNotFoundException)
            {
                return NotFound();
            }
        }
    }

    public class SendMessageDto
    {
        public int SenderId { get; set; }
        public int ReceiverId { get; set; }
        public string Content { get; set; }
        public IFormFile Attachment { get; set; }
    }
}