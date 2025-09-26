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
    [Authorize(Policy = UserRole.Policies.SendMessages)] // ������� �������� ��� ����� �����������
    public class MessageController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly FileService _fileService;
        private readonly ILogger<MessageController> _logger; // ��������� ���� _logger

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

            // ���������, �������� �� ��� ������ ���������� ��� ���������
            if (dto.GroupChatId.HasValue)
            {
                // �������� ��� ���������� ����
                var groupChat = await _context.GroupChats
                    .Include(gc => gc.Members)
                    .FirstOrDefaultAsync(gc => gc.Id == dto.GroupChatId);

                if (groupChat == null)
                    return NotFound("��������� ��� �� ������");

                if (!groupChat.Members.Any(m => m.UserId == senderId))
                    return Forbid("�� �� ��������� ���������� ����� ����");
            }
            else if (dto.ReceiverId.HasValue)
            {
                // �������� ��� ������� ���������
                var receiver = await _context.Users.FindAsync(dto.ReceiverId);
                if (receiver == null)
                    return BadRequest("���������� �� ������");
            }
            else
            {
                return BadRequest("���������� ������� ���� ����������, ���� ��������� ���");
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

            return Ok("��������� ����������");
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
                return NotFound("��������� �� �������");

            // ������ ����� ��������� ��� ������������� ����� ��� �������
            if (message.SenderId != currentUserId && !User.IsInRole(UserRole.Administrator))
                return Forbid("������ ����� ��������� ��� ������������� ����� ��� �������");

            message.IsDeleted = true;
            await _context.SaveChangesAsync();

            return Ok("��������� �������");
        }

        [HttpGet("chat/{groupChatId}")]
        public async Task<IActionResult> GetGroupChatMessages(int groupChatId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // ���������, �������� �� ������������ ���������� ����
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
                    return NotFound("���� �� ������");

                if (string.IsNullOrEmpty(message.AttachmentPath))
                    return NotFound("���� �� ������");

                // ��������� ������ � �����
                bool hasAccess = false;
                if (message.GroupChatId.HasValue)
                {
                    // ��� ���������� ����
                    hasAccess = message.GroupChat.Members.Any(m => m.UserId == currentUserId);
                }
                else
                {
                    // ��� ������ ���������
                    hasAccess = message.SenderId == currentUserId || message.ReceiverId == currentUserId;
                }

                if (!hasAccess)
                    return Forbid("� ��� ��� ������� � ����� �����");

                var fileBytes = await _fileService.GetFileAsync(message.AttachmentPath);

                // ���������� MIME-��� �����
                string contentType = "application/octet-stream"; // �������� �� ���������
                if (!string.IsNullOrWhiteSpace(message.AttachmentType))
                {
                    contentType = message.AttachmentType;
                }
                else
                {
                    // ������� ���������� ��� ����� �� ����������
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

                // ��������, ��� ��� ����� ���������
                string fileName = message.AttachmentName;
                if (string.IsNullOrEmpty(fileName))
                {
                    fileName = "file" + Path.GetExtension(message.AttachmentPath);
                }

                // ��������� ��������� Content-Disposition
                var cd = new ContentDisposition
                {
                    FileName = fileName,
                    Inline = false // ��� �������� ������� ��������� ���� ������ �����������
                };

                Response.Headers.Add("Content-Disposition", cd.ToString());

                return File(fileBytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "������ ��� ��������� ����� {MessageId}", messageId);
                return Problem("������ ��� ��������� �����", statusCode: 500);
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
                return NotFound("��������� �� �������");

            // ������ ����� ��������� ����� ��� �������������
            if (message.SenderId != currentUserId)
                return Forbid("������ ����� ��������� ����� ��� �������������");

            // ���� ��� ��������� ���, ��������� ��� ������������ �� ��� �������� ��� ����������
            if (message.GroupChatId.HasValue)
            {
                var isMember = message.GroupChat.Members.Any(m => m.UserId == currentUserId);
                if (!isMember)
                    return Forbid("�� ������ �� ��������� ���������� ����� ����");
            }

            message.Content = dto.Content;
            await _context.SaveChangesAsync();

            return Ok("��������� ���������");
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
                    m.Content // ������������ ��������� � �����
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

            // ������� ������
            var query = _context.Users
                .Include(u => u.Group)
                .Where(u => u.Id != currentUserId); // ��������� �������� ������������

            // ��������� ������ �� ����
            if (!string.IsNullOrEmpty(role) && UserRole.AllRoles.Contains(role))
            {
                query = query.Where(u => u.Role == role);
            }

            // ��������� ����� �� ����� ��� email
            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                query = query.Where(u =>
                    u.FullName.ToLower().Contains(search) ||
                    u.Email.ToLower().Contains(search));
            }

            // ��������� ������� � ����������� �� ���� �������� ������������
            switch (currentUserRole)
            {
                case UserRole.Student:
                    // �������� ����� �������� � ��������������� � ������� ���������� ����� ������
                    query = query.Where(u =>
                        u.Role == UserRole.Teacher ||
                        (u.Role == UserRole.Student && u.StudentGroupId ==
                            _context.Users.Where(cu => cu.Id == currentUserId)
                            .Select(cu => cu.StudentGroupId)
                            .FirstOrDefault()));
                    break;

                case UserRole.Teacher:
                    // ������������� ����� �������� �� ����� ���������� � ������� ���������������
                    query = query.Where(u =>
                        u.Role == UserRole.Teacher ||
                        u.Role == UserRole.Student);
                    break;

                case UserRole.Parent:
                    // �������� ����� �������� � ��������������� � �� ������ ������
                    var childrenIds = await _context.Users
                        .Where(u => u.Parents.Any(p => p.Id == currentUserId))
                        .Select(u => u.Id)
                        .ToListAsync();

                    query = query.Where(u =>
                        u.Role == UserRole.Teacher ||
                        childrenIds.Contains(u.Id));
                    break;

                case UserRole.Administrator:
                    // �������������� ����� �������� �� �����
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