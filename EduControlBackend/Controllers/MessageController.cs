using EduControlBackend.Models;
using EduControlBackend.Models.MessagesModels;
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
    [Authorize(Policy = UserRole.Policies.SendMessages)] // Базовая политика для всего контроллера
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
        [Authorize(Policy = UserRole.Policies.SendMessages)]
        public async Task<IActionResult> SendMessage([FromForm] SendMessageDto dto)
        {
            var senderId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // Проверяем, является ли это личным сообщением или групповым
            if (dto.GroupChatId.HasValue)
            {
                // Проверка для группового чата
                var groupChat = await _context.GroupChats
                    .Include(gc => gc.Members)
                    .FirstOrDefaultAsync(gc => gc.Id == dto.GroupChatId);

                if (groupChat == null)
                    return NotFound("Групповой чат не найден");

                if (!groupChat.Members.Any(m => m.UserId == senderId))
                    return Forbid("Вы не являетесь участником этого чата");
            }
            else if (dto.ReceiverId.HasValue)
            {
                // Проверка для личного сообщения
                var receiver = await _context.Users.FindAsync(dto.ReceiverId);
                if (receiver == null)
                    return BadRequest("Получатель не найден");
            }
            else
            {
                return BadRequest("Необходимо указать либо получателя, либо групповой чат");
            }

            var message = new Message
            {
                SenderId = senderId,
                ReceiverId = dto.ReceiverId,
                GroupChatId = dto.GroupChatId,
                Content = dto.Content,
                Timestamp = DateTime.UtcNow
            };

            if (dto.Attachment != null)
            {
                var (path, fileName) = await _fileService.SaveFileAsync(dto.Attachment);
                message.AttachmentPath = path;
                message.AttachmentName = fileName;
                message.AttachmentType = dto.Attachment.ContentType;
            }

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            return Ok("Сообщение отправлено");
        }

        [HttpDelete("messages/{messageId}")]
        [Authorize(Policy = UserRole.Policies.DeleteMessages)]
        public async Task<IActionResult> DeleteMessage(int messageId)
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var message = await _context.Messages
                .Include(m => m.GroupChat)
                .ThenInclude(gc => gc.Members)
                .FirstOrDefaultAsync(m => m.Id == messageId);

            if (message == null || message.IsDeleted)
                return NotFound("Сообщение не найдено");

            // Только автор сообщения или администратор может его удалить
            if (message.SenderId != currentUserId && !User.IsInRole(UserRole.Administrator))
                return Forbid("Только автор сообщения или администратор может его удалить");

            message.IsDeleted = true;
            await _context.SaveChangesAsync();

            return Ok("Сообщение удалено");
        }

        [HttpGet("chat/{groupChatId}")]
        public async Task<IActionResult> GetGroupChatMessages(int groupChatId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // Проверяем, является ли пользователь участником чата
            var isMember = await _context.GroupChatMembers
                .AnyAsync(m => m.GroupChatId == groupChatId && m.UserId == currentUserId);

            if (!isMember)
                return Forbid();

            var messages = await _context.Messages
                .Include(m => m.Sender)
                .Where(m => m.GroupChatId == groupChatId && !m.IsDeleted)
                .OrderByDescending(m => m.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new
                {
                    m.Id,
                    m.Content,
                    m.Timestamp,
                    Sender = new { m.Sender.Id, m.Sender.FullName },
                    HasAttachment = m.AttachmentPath != null,
                    m.AttachmentName,
                    m.AttachmentType
                })
                .ToListAsync();

            return Ok(messages);
        }

        [HttpGet("direct/{userId}")]
        public async Task<IActionResult> GetDirectMessages(int userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var messages = await _context.Messages
                .Include(m => m.Sender)
                .Where(m => !m.IsDeleted &&
                           !m.GroupChatId.HasValue &&
                           ((m.SenderId == currentUserId && m.ReceiverId == userId) ||
                            (m.SenderId == userId && m.ReceiverId == currentUserId)))
                .OrderByDescending(m => m.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new
                {
                    m.Id,
                    m.Content,
                    m.Timestamp,
                    Sender = new { m.Sender.Id, m.Sender.FullName },
                    HasAttachment = m.AttachmentPath != null,
                    m.AttachmentName,
                    m.AttachmentType
                })
                .ToListAsync();

            return Ok(messages);
        }

        [HttpGet("file/{messageId}")]
        public async Task<IActionResult> GetFile(int messageId)
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        
            var message = await _context.Messages
                .Include(m => m.GroupChat)
                .ThenInclude(gc => gc.Members)
                .FirstOrDefaultAsync(m => m.Id == messageId);

            if (message == null || message.IsDeleted)
                return NotFound();

            // Проверяем доступ к файлу
            bool hasAccess = false;
            if (message.GroupChatId.HasValue)
            {
                // Для группового чата
                hasAccess = message.GroupChat.Members.Any(m => m.UserId == currentUserId);
            }
            else
            {
                // Для личных сообщений
                hasAccess = message.SenderId == currentUserId || message.ReceiverId == currentUserId;
            }

            if (!hasAccess)
                return Forbid();

            if (string.IsNullOrEmpty(message.AttachmentPath))
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

        [HttpPut("{messageId}")]
        public async Task<IActionResult> EditMessage(int messageId, [FromBody] EditMessageDto dto)
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var message = await _context.Messages
                .Include(m => m.GroupChat)
                .ThenInclude(gc => gc.Members)
                .FirstOrDefaultAsync(m => m.Id == messageId);

            if (message == null || message.IsDeleted)
                return NotFound("Сообщение не найдено");

            // Только автор сообщения может его редактировать
            if (message.SenderId != currentUserId)
                return Forbid("Только автор сообщения может его редактировать");

            // Если это групповой чат, проверяем что пользователь всё ещё является его участником
            if (message.GroupChatId.HasValue)
            {
                var isMember = message.GroupChat.Members.Any(m => m.UserId == currentUserId);
                if (!isMember)
                    return Forbid("Вы больше не являетесь участником этого чата");
            }

            message.Content = dto.Content;
            await _context.SaveChangesAsync();

            return Ok("Сообщение обновлено");
        }

        [HttpGet("direct/{userId}/attachments")]
        public async Task<IActionResult> GetDirectChatAttachments(int userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var attachments = await _context.Messages
                .Include(m => m.Sender)
                .Where(m => !m.IsDeleted &&
                           !m.GroupChatId.HasValue &&
                           m.AttachmentPath != null &&
                           ((m.SenderId == currentUserId && m.ReceiverId == userId) ||
                            (m.SenderId == userId && m.ReceiverId == currentUserId)))
                .OrderByDescending(m => m.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new
                {
                    m.Id,
                    m.Timestamp,
                    Sender = new
                    {
                        m.Sender.Id,
                        m.Sender.FullName,
                        m.Sender.Email
                    },
                    m.AttachmentName,
                    m.AttachmentType,
                    m.Content // Опциональное сообщение к файлу
                })
                .ToListAsync();

            var totalAttachments = await _context.Messages
                .CountAsync(m => !m.IsDeleted &&
                                !m.GroupChatId.HasValue &&
                                m.AttachmentPath != null &&
                                ((m.SenderId == currentUserId && m.ReceiverId == userId) ||
                                 (m.SenderId == userId && m.ReceiverId == currentUserId)));

            var result = new
            {
                Items = attachments,
                TotalCount = totalAttachments,
                CurrentPage = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalAttachments / (double)pageSize)
            };

            return Ok(result);
        }
    }
}