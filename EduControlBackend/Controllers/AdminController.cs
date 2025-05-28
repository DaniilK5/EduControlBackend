using EduControlBackend.Models;
using EduControlBackend.Models.AdminModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduControlBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Базовая авторизация
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("users")]
        [Authorize(Policy = UserRole.Policies.ManageUsers)]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _context.Users
                .Select(u => new
                {
                    u.Id,
                    u.FullName,
                    u.Email,
                    u.Role,
                    u.StudentGroup
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpPut("users/{userId}/role")]
        [Authorize(Policy = UserRole.Policies.ManageUsers)]
        public async Task<IActionResult> UpdateUserRole(int userId, [FromBody] UpdateUserRoleDto dto)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound();

            if (!UserRole.AllRoles.Contains(dto.Role))
                return BadRequest("Недопустимая роль");

            user.Role = dto.Role;
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpDelete("users/{userId}")]
        [Authorize(Policy = UserRole.Policies.ManageUsers)]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound();

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpGet("settings")]
        [Authorize(Policy = UserRole.Policies.ManageSettings)]
        public IActionResult GetSettings()
        {
            // Реализация получения настроек
            return Ok(new { /* настройки */ });
        }

        [HttpPut("settings")]
        [Authorize(Policy = UserRole.Policies.ManageSettings)]
        public async Task<IActionResult> UpdateSettings([FromBody] UpdateSettingsDto dto)
        {
            // Реализация обновления настроек
            return Ok();
        }
    }
}