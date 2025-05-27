using EduControlBackend.Models;
using EduControlBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EduControlBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
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

        [HttpPost("{chatId}/members")]
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

        [HttpPost("{chatId}/admins")]
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

            memberToPromote.IsAdmin = true;
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpDelete("{chatId}/members/{userId}")]
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
                .Where(m => m.GroupChatId == chatId && !m.IsDeleted)
                .OrderByDescending(m => m.Timestamp)
                .ToListAsync();

            return Ok(messages);
        }
    }

    public class CreateGroupChatDto
    {
        public string Name { get; set; }
    }

    public class AddMemberDto
    {
        public int UserId { get; set; }
    }

    public class PromoteToAdminDto
    {
        public int UserId { get; set; }
    }
}