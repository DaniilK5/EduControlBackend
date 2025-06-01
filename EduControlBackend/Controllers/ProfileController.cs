using EduControlBackend;
using EduControlBackend.Models.StudentModels;
using EduControlBackend.Models.UserModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EduControl.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProfileController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("me")]
        public async Task<ActionResult<UserProfileDto>> GetMyProfile()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var user = await _context.Users
                .Include(u => u.Group)
                .FirstOrDefaultAsync(u => u.Id == int.Parse(userId));

            if (user == null)
            {
                return NotFound();
            }

            var profile = new UserProfileDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role,
                StudentGroup = user.StudentGroup,
                Address = user.Address,
                PhoneNumber = user.PhoneNumber,
                SocialStatus = user.SocialStatus,
                StudentId = user.StudentId,
                StudentGroupId = user.StudentGroupId
            };

            return Ok(profile);
        }

        [HttpPut("me")]
        public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateUserProfileDto updateDto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var user = await _context.Users.FindAsync(int.Parse(userId));
            if (user == null)
            {
                return NotFound();
            }

            // Обновляем только разрешенные поля
            user.FullName = updateDto.FullName;
            user.Address = updateDto.Address;
            user.PhoneNumber = updateDto.PhoneNumber;
            user.SocialStatus = updateDto.SocialStatus;

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { message = "Профиль успешно обновлен" });
            }
            catch (DbUpdateConcurrencyException)
            {
                return StatusCode(500, new { message = "Ошибка при обновлении профиля" });
            }
        }

        [HttpGet("me/children")]
        [Authorize(Roles = "Parent")]
        public async Task<ActionResult<List<UserProfileDto>>> GetMyChildren()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var children = await _context.Users
                .Include(u => u.Group)
                .Where(u => u.Parents.Any(p => p.Id == int.Parse(userId)))
                .Select(c => new UserProfileDto
                {
                    Id = c.Id,
                    FullName = c.FullName,
                    Email = c.Email,
                    Role = c.Role,
                    StudentGroup = c.StudentGroup,
                    StudentId = c.StudentId,
                    StudentGroupId = c.StudentGroupId
                })
                .ToListAsync();

            return Ok(children);
        }

        [HttpGet("me/curated-groups")]
        [Authorize(Roles = "Teacher")]
        public async Task<ActionResult<List<StudentGroup>>> GetMyCuratedGroups()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var curatedGroups = await _context.Users
                .Include(u => u.CuratedGroups)
                .Where(u => u.Id == int.Parse(userId))
                .SelectMany(u => u.CuratedGroups)
                .ToListAsync();

            return Ok(curatedGroups);
        }
    }
}