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
        public async Task<IActionResult> GetSettings()
        {
            var settings = await _context.Settings.FirstOrDefaultAsync();
            if (settings == null)
            {
                // Если настройки не найдены, создаем настройки по умолчанию
                settings = new AppSettings();
                _context.Settings.Add(settings);
                await _context.SaveChangesAsync();
            }

            return Ok(new
            {
                settings.SiteName,
                settings.DefaultTimeZone,
                settings.MaxFileSize,
                settings.AllowedFileTypes,
                settings.MaxUploadFilesPerMessage,
                settings.DefaultPageSize,
                settings.RequireEmailVerification,
                settings.PasswordMinLength,
                settings.RequireStrongPassword
            });
        }

        [HttpPut("settings")]
        [Authorize(Policy = UserRole.Policies.ManageSettings)]
        public async Task<IActionResult> UpdateSettings([FromBody] UpdateSettingsDto dto)
        {
            var settings = await _context.Settings.FirstOrDefaultAsync();
            if (settings == null)
            {
                settings = new AppSettings();
                _context.Settings.Add(settings);
            }

            // Валидация
            if (dto.MaxFileSize < 0)
                return BadRequest("Максимальный размер файла не может быть отрицательным");

            if (dto.DefaultPageSize <= 0)
                return BadRequest("Размер страницы должен быть положительным числом");

            if (dto.PasswordMinLength < 6)
                return BadRequest("Минимальная длина пароля не может быть меньше 6 символов");

            if (dto.MaxUploadFilesPerMessage <= 0)
                return BadRequest("Максимальное количество файлов должно быть положительным числом");

            // Обновляем настройки
            settings.SiteName = dto.SiteName;
            settings.DefaultTimeZone = dto.DefaultTimeZone;
            settings.MaxFileSize = dto.MaxFileSize;
            settings.AllowedFileTypes = dto.AllowedFileTypes;
            settings.MaxUploadFilesPerMessage = dto.MaxUploadFilesPerMessage;
            settings.DefaultPageSize = dto.DefaultPageSize;
            settings.RequireEmailVerification = dto.RequireEmailVerification;
            settings.PasswordMinLength = dto.PasswordMinLength;
            settings.RequireStrongPassword = dto.RequireStrongPassword;

            await _context.SaveChangesAsync();

            // Возвращаем обновленные настройки
            return Ok(new
            {
                settings.SiteName,
                settings.DefaultTimeZone,
                settings.MaxFileSize,
                settings.AllowedFileTypes,
                settings.MaxUploadFilesPerMessage,
                settings.DefaultPageSize,
                settings.RequireEmailVerification,
                settings.PasswordMinLength,
                settings.RequireStrongPassword
            });
        }
    }
}