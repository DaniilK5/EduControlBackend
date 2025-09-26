using EduControlBackend.Models;
using EduControlBackend.Models.LoginAndReg;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace EduControlBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;

        public AuthController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            // Проверяем валидность роли
            if (!UserRole.AllRoles.Contains(registerDto.Role))
            {
                return BadRequest("Недопустимая роль");
            }

            // Проверяем, что группа указана только для студентов
            if (registerDto.Role != UserRole.Student && !string.IsNullOrEmpty(registerDto.StudentGroup))
            {
                return BadRequest("Группа может быть указана только для студентов");
            }

            // Если это студент, проверяем наличие группы
            if (registerDto.Role == UserRole.Student && string.IsNullOrEmpty(registerDto.StudentGroup))
            {
                return BadRequest("Для студента необходимо указать группу");
            }

            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == registerDto.Email);
            if (existingUser != null)
            {
                return BadRequest("Email уже зарегистрирован");
            }

            var user = new User
            {
                FullName = registerDto.FullName,
                Email = registerDto.Email,
                Password = BCrypt.Net.BCrypt.HashPassword(registerDto.Password),
                Role = registerDto.Role,
                StudentGroup = registerDto.StudentGroup
            };

            // Для родителей проверяем и связываем детей
            if (registerDto.Role == UserRole.Parent && registerDto.ChildrenIds != null)
            {
                var children = await _context.Users
                    .Where(u => registerDto.ChildrenIds.Contains(u.Id) && u.Role == UserRole.Student)
                    .ToListAsync();

                if (children.Count != registerDto.ChildrenIds.Count)
                    return BadRequest("Некоторые указанные студенты не найдены");

                user.Children = children;
            }

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok("Пользователь зарегистрирован");
        }


        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == loginDto.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.Password))
            {
                return Unauthorized("Неверный логин или пароль");
            }

            var token = GenerateJwtToken(user);
            return Ok(new { token });
        }

        private string GenerateJwtToken(User user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: "EduControl",
                audience: "EduControl",
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
