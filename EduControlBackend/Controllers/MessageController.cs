using EduControlBackend.Models;
using EduControlBackend.Models.MessagesModels;
using EduControlBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Net.Mime;
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
        private readonly ILogger<MessageController> _logger; // Добавляем поле _logger

        public MessageController(ApplicationDbContext context, FileService fileService, ILogger<MessageController> logger)
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
            try
            {
                var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

                var message = await _context.Messages
                    .Include(m => m.GroupChat)
                    .ThenInclude(gc => gc.Members)
                    .FirstOrDefaultAsync(m => m.Id == messageId);

                if (message == null || message.IsDeleted)
                    return NotFound("Файл не найден");

                if (string.IsNullOrEmpty(message.AttachmentPath))
                    return NotFound("Файл не найден");

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
                    return Forbid("У вас нет доступа к этому файлу");

                var fileBytes = await _fileService.GetFileAsync(message.AttachmentPath);

                // Определяем MIME-тип файла
                string contentType = "application/octet-stream"; // значение по умолчанию
                if (!string.IsNullOrWhiteSpace(message.AttachmentType))
                {
                    contentType = message.AttachmentType;
                }
                else
                {
                    // Попытка определить тип файла по расширению
                    var extension = Path.GetExtension(message.AttachmentName)?.ToLowerInvariant();
                    if (!string.IsNullOrEmpty(extension))
                    {
                        contentType = extension switch
                        {
                            ".jpg" or ".jpeg" => "image/jpeg",
                            ".png" => "image/png",
                            ".gif" => "image/gif",
                            ".pdf" => "application/pdf",
                            ".doc" => "application/msword",
                            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                            ".xls" => "application/vnd.ms-excel",
                            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                            ".txt" => "text/plain",
                            _ => "application/octet-stream"
                        };
                    }
                }

                // Убедимся, что имя файла корректно
                string fileName = message.AttachmentName;
                if (string.IsNullOrEmpty(fileName))
                {
                    fileName = "file" + Path.GetExtension(message.AttachmentPath);
                }

                // Добавляем заголовок Content-Disposition
                var cd = new ContentDisposition
                {
                    FileName = fileName,
                    Inline = false // Это заставит браузер скачивать файл вместо отображения
                };

                Response.Headers.Add("Content-Disposition", cd.ToString());

                return File(fileBytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении файла {MessageId}", messageId);
                return Problem("Ошибка при получении файла", statusCode: 500);
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

        [HttpGet("users")]
        [Authorize(Policy = UserRole.Policies.SendMessages)]
        public async Task<ActionResult<IEnumerable<ChatUserDto>>> GetAvailableChatUsers([FromQuery] string? search, [FromQuery] string? role)
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var currentUserRole = User.FindFirstValue(ClaimTypes.Role);

            // Базовый запрос
            var query = _context.Users
                .Include(u => u.Group)
                .Where(u => u.Id != currentUserId); // Исключаем текущего пользователя

            // Применяем фильтр по роли
            if (!string.IsNullOrEmpty(role) && UserRole.AllRoles.Contains(role))
            {
                query = query.Where(u => u.Role == role);
            }

            // Применяем поиск по имени или email
            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                query = query.Where(u =>
                    u.FullName.ToLower().Contains(search) ||
                    u.Email.ToLower().Contains(search));
            }

            // Применяем фильтры в зависимости от роли текущего пользователя
            switch (currentUserRole)
            {
                case UserRole.Student:
                    // Студенты могут общаться с преподавателями и другими студентами своей группы
                    query = query.Where(u =>
                        u.Role == UserRole.Teacher ||
                        (u.Role == UserRole.Student && u.StudentGroupId ==
                            _context.Users.Where(cu => cu.Id == currentUserId)
                            .Select(cu => cu.StudentGroupId)
                            .FirstOrDefault()));
                    break;

                case UserRole.Teacher:
                    // Преподаватели могут общаться со всеми студентами и другими преподавателями
                    query = query.Where(u =>
                        u.Role == UserRole.Teacher ||
                        u.Role == UserRole.Student);
                    break;

                case UserRole.Parent:
                    // Родители могут общаться с преподавателями и со своими детьми
                    var childrenIds = await _context.Users
                        .Where(u => u.Parents.Any(p => p.Id == currentUserId))
                        .Select(u => u.Id)
                        .ToListAsync();

                    query = query.Where(u =>
                        u.Role == UserRole.Teacher ||
                        childrenIds.Contains(u.Id));
                    break;

                case UserRole.Administrator:
                    // Администраторы могут общаться со всеми
                    break;

                default:
                    return Forbid();
            }

            var users = await query
                .OrderBy(u => u.Role)
                .ThenBy(u => u.FullName)
                .Select(u => new ChatUserDto
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email,
                    Role = u.Role,
                    StudentGroup = u.StudentGroup
                })
                .ToListAsync();

            return Ok(users);
        }
    }
}