using EduControlBackend.Models;
using EduControlBackend.Models.Chat;
using EduControlBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EduControlBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Базовая авторизация
    public class GroupChatController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly FileService _fileService;

        public GroupChatController(ApplicationDbContext context, FileService fileService)
        {
            _context = context;
            _fileService = fileService;
        }

        [HttpPost("create")]
        [Authorize(Policy = UserRole.Policies.ManageGroupChats)]
        public async Task<IActionResult> CreateGroupChat([FromBody] CreateGroupChatDto dto)
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var groupChat = new GroupChat
            {
                Name = dto.Name,
                CreatedAt = DateTime.UtcNow,
                Members = new List<GroupChatMember>
                {
                    new GroupChatMember
                    {
                        UserId = currentUserId,
                        IsAdmin = true,
                        JoinedAt = DateTime.UtcNow
                    }
                }
            };

            _context.GroupChats.Add(groupChat);
            await _context.SaveChangesAsync();

            return Ok(new { groupChatId = groupChat.Id });
        }

        [HttpPost("{chatId}/AddMember")]
        [Authorize(Policy = UserRole.Policies.ManageGroupChats)]
        public async Task<IActionResult> AddMember(int chatId, [FromBody] AddMemberDto dto)
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            
            var chat = await _context.GroupChats
                .Include(c => c.Members)
                .FirstOrDefaultAsync(c => c.Id == chatId);

            if (chat == null)
                return NotFound("Чат не найден");

            var currentMember = chat.Members.FirstOrDefault(m => m.UserId == currentUserId);
            if (currentMember == null || !currentMember.IsAdmin)
                return Forbid();

            if (chat.Members.Any(m => m.UserId == dto.UserId))
                return BadRequest("Пользователь уже в чате");

            var newMember = new GroupChatMember
            {
                UserId = dto.UserId,
                GroupChatId = chatId,
                IsAdmin = false,
                JoinedAt = DateTime.UtcNow
            };

            chat.Members.Add(newMember);
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpPost("{chatId}/PromoteToAdmin")]
        [Authorize(Policy = UserRole.Policies.ManageGroupChats)]
        public async Task<IActionResult> PromoteToAdmin(int chatId, [FromBody] PromoteToAdminDto dto)
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            
            var chat = await _context.GroupChats
                .Include(c => c.Members)
                .FirstOrDefaultAsync(c => c.Id == chatId);

            if (chat == null)
                return NotFound("Чат не найден");

            var currentMember = chat.Members.FirstOrDefault(m => m.UserId == currentUserId);
            if (currentMember == null || !currentMember.IsAdmin)
                return Forbid();

            var memberToPromote = chat.Members.FirstOrDefault(m => m.UserId == dto.UserId);
            if (memberToPromote == null)
                return NotFound("Пользователь не найден в чате");

            if (memberToPromote.IsAdmin)
                return BadRequest("Пользователь уже является администратором");

            memberToPromote.IsAdmin = true;
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpDelete("{chatId}/members/{userId}")]
        [Authorize(Policy = UserRole.Policies.ManageGroupChats)]
        public async Task<IActionResult> RemoveMember(int chatId, int userId)
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            
            var chat = await _context.GroupChats
                .Include(c => c.Members)
                .FirstOrDefaultAsync(c => c.Id == chatId);

            if (chat == null)
                return NotFound("Чат не найден");

            var currentMember = chat.Members.FirstOrDefault(m => m.UserId == currentUserId);
            if (currentMember == null || !currentMember.IsAdmin)
                return Forbid();

            var memberToRemove = chat.Members.FirstOrDefault(m => m.UserId == userId);
            if (memberToRemove == null)
                return NotFound("Пользователь не найден в чате");

            // Нельзя удалить последнего администратора
            if (memberToRemove.IsAdmin && chat.Members.Count(m => m.IsAdmin) == 1)
                return BadRequest("Нельзя удалить последнего администратора");

            chat.Members.Remove(memberToRemove);
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpDelete("{chatId}/messages/{messageId}")]
        [Authorize(Policy = UserRole.Policies.DeleteMessages)]
        public async Task<IActionResult> DeleteMessage(int chatId, int messageId)
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            
            var chat = await _context.GroupChats
                .Include(c => c.Members)
                .FirstOrDefaultAsync(c => c.Id == chatId);

            if (chat == null)
                return NotFound("Чат не найден");

            var currentMember = chat.Members.FirstOrDefault(m => m.UserId == currentUserId);
            if (currentMember == null || !currentMember.IsAdmin)
                return Forbid();

            var message = await _context.Messages
                .FirstOrDefaultAsync(m => m.Id == messageId && m.GroupChatId == chatId);

            if (message == null)
                return NotFound("Сообщение не найдено");

            message.IsDeleted = true;
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpGet("{chatId}/messages")]
        public async Task<IActionResult> GetMessages(int chatId)
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            
            var chat = await _context.GroupChats
                .Include(c => c.Members)
                .FirstOrDefaultAsync(c => c.Id == chatId);

            if (chat == null)
                return NotFound("Чат не найден");

            if (!chat.Members.Any(m => m.UserId == currentUserId))
                return Forbid();

            var messages = await _context.Messages
                .Include(m => m.Sender)
                .Where(m => m.GroupChatId == chatId && !m.IsDeleted)
                .OrderByDescending(m => m.Timestamp)
                .Select(m => new
                {
                    m.Id,
                    m.Content,
                    m.Timestamp,
                    Sender = new 
                    { 
                        m.Sender.Id,
                        m.Sender.FullName,
                        m.Sender.Email
                    },
                    HasAttachment = m.AttachmentPath != null,
                    m.AttachmentName,
                    m.AttachmentType
                })
                .ToListAsync();

            var chatInfo = new
            {
                chat.Id,
                chat.Name,
                chat.CreatedAt,
                Members = chat.Members.Select(m => new
                {
                    UserId = m.UserId,
                    m.IsAdmin,
                    m.JoinedAt
                }),
                Messages = messages
            };

            return Ok(chatInfo);
        }

        [HttpGet("{chatId}/members")]
        public async Task<IActionResult> GetChatMembers(int chatId)
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            
            var chat = await _context.GroupChats
                .Include(c => c.Members)
                .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(c => c.Id == chatId);

            if (chat == null)
                return NotFound("Чат не найден");

            // Проверяем, является ли текущий пользователь участником чата
            if (!chat.Members.Any(m => m.UserId == currentUserId))
                return Forbid();

            var members = chat.Members.Select(m => new
            {
                UserId = m.UserId,
                m.User.FullName,
                m.User.Email,
                m.IsAdmin,
                m.JoinedAt
            }).OrderByDescending(m => m.IsAdmin)
            .ThenBy(m => m.FullName)
            .ToList();

            return Ok(members);
        }

        [HttpGet("{chatId}/attachments")]
        public async Task<IActionResult> GetChatAttachments(int chatId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            
            var chat = await _context.GroupChats
                .Include(c => c.Members)
                .FirstOrDefaultAsync(c => c.Id == chatId);

            if (chat == null)
                return NotFound("Чат не найден");

            if (!chat.Members.Any(m => m.UserId == currentUserId))
                return Forbid();

            var attachments = await _context.Messages
                .Include(m => m.Sender)
                .Where(m => m.GroupChatId == chatId && 
                        !m.IsDeleted && 
                        m.AttachmentPath != null)
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
                .CountAsync(m => m.GroupChatId == chatId && 
                            !m.IsDeleted && 
                            m.AttachmentPath != null);

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

        [HttpGet("my")]
        public async Task<IActionResult> GetMyGroupChats()
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var chats = await _context.GroupChats
                .Include(c => c.Members)
                    .ThenInclude(m => m.User)
                .Where(c => c.Members.Any(m => m.UserId == currentUserId))
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.CreatedAt,
                    MembersCount = c.Members.Count,
                    IsAdmin = c.Members.Any(m => m.UserId == currentUserId && m.IsAdmin),
                    LastMessage = c.Messages
                        .Where(m => !m.IsDeleted)
                        .OrderByDescending(m => m.Timestamp)
                        .Select(m => new
                        {
                            m.Id,
                            m.Content,
                            m.Timestamp,
                            Sender = new
                            {
                                m.Sender.Id,
                                m.Sender.FullName
                            },
                            HasAttachment = m.AttachmentPath != null,
                            m.AttachmentName
                        })
                        .FirstOrDefault(),
                    Members = c.Members
                        .Take(3) // Получаем только первых трех участников для превью
                        .Select(m => new
                        {
                            m.User.Id,
                            m.User.FullName,
                            m.IsAdmin
                        })
                        .ToList(),
                    UnreadCount = c.Messages.Count(m => 
                        !m.IsDeleted && 
                        m.Timestamp > c.Members
                            .First(member => member.UserId == currentUserId)
                            .JoinedAt
                    )
                })
                .OrderByDescending(c => c.LastMessage.Timestamp)
                .ToListAsync();

            return Ok(new
            {
                TotalCount = chats.Count,
                Chats = chats
            });
        }

        [HttpGet("{chatId}")]
        public async Task<IActionResult> GetGroupChat(int chatId)
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var chat = await _context.GroupChats
                .Include(c => c.Members)
                    .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(c => c.Id == chatId);

            if (chat == null)
                return NotFound("Чат не найден");

            // Проверяем, является ли пользователь участником чата
            var currentMember = chat.Members.FirstOrDefault(m => m.UserId == currentUserId);
            if (currentMember == null)
                return Forbid("Вы не являетесь участником этого чата");

            var result = new
            {
                chat.Id,
                chat.Name,
                chat.CreatedAt,
                CurrentUserInfo = new
                {
                    IsAdmin = currentMember.IsAdmin,
                    JoinedAt = currentMember.JoinedAt
                },
                Members = chat.Members.Select(m => new
                {
                    UserId = m.UserId,
                    m.User.FullName,
                    m.User.Email,
                    m.IsAdmin,
                    m.JoinedAt
                }).OrderByDescending(m => m.IsAdmin)
                  .ThenBy(m => m.FullName)
                  .ToList(),
                Statistics = new
                {
                    TotalMembers = chat.Members.Count,
                    AdminsCount = chat.Members.Count(m => m.IsAdmin),
                    TotalMessages = _context.Messages.Count(m => m.GroupChatId == chatId && !m.IsDeleted),
                    TotalAttachments = _context.Messages.Count(m => 
                        m.GroupChatId == chatId && 
                        !m.IsDeleted && 
                        m.AttachmentPath != null),
                    UnreadMessages = _context.Messages.Count(m => 
                        m.GroupChatId == chatId &&
                        !m.IsDeleted && 
                        m.Timestamp > currentMember.JoinedAt &&
                        m.SenderId != currentUserId),
                    YourMessages = _context.Messages.Count(m => 
                        m.GroupChatId == chatId && 
                        !m.IsDeleted && 
                        m.SenderId == currentUserId)
                }
            };

            return Ok(result);
        }
    }
}