using EduControlBackend.Models;
using EduControlBackend.Models.CourseModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EduControlBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SubjectController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SubjectController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Получение всех предметов с информацией о курсах
        [HttpGet]
        [Authorize(Policy = UserRole.Policies.ViewCourses)]
        public async Task<IActionResult> GetSubjects([FromQuery] bool includeCourses = false)
        {
            var query = _context.Subjects.AsQueryable();

            if (includeCourses)
            {
                query = query.Include(s => s.Courses)
                            .ThenInclude(c => c.Teachers)
                                .ThenInclude(t => t.User);
            }

            var subjects = await query.Select(s => new
            {
                s.Id,
                s.Name,
                s.Code,
                s.Description,
                CoursesCount = s.Courses.Count(c => c.IsActive),
                Courses = includeCourses ? s.Courses
                    .Where(c => c.IsActive)
                    .Select(c => new
                    {
                        c.Id,
                        c.Name,
                        c.Description,
                        Teachers = c.Teachers.Select(t => new { t.User.Id, t.User.FullName }),
                        StudentsCount = c.Students.Count
                    }) : null
            }).ToListAsync();

            return Ok(subjects);
        }

        // Получение конкретного предмета
        [HttpGet("{id}")]
        [Authorize(Policy = UserRole.Policies.ViewCourses)]
        public async Task<IActionResult> GetSubject(int id)
        {
            var subject = await _context.Subjects
                .Include(s => s.Courses.Where(c => c.IsActive))
                    .ThenInclude(c => c.Teachers)
                        .ThenInclude(t => t.User)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (subject == null)
                return NotFound("Предмет не найден");

            return Ok(new
            {
                subject.Id,
                subject.Name,
                subject.Code,
                subject.Description,
                Courses = subject.Courses.Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Description,
                    c.CreatedAt,
                    Teachers = c.Teachers.Select(t => new { t.User.Id, t.User.FullName }),
                    StudentsCount = c.Students.Count
                })
            });
        }

        [HttpGet("{subjectId}/courses")]
        [Authorize(Policy = UserRole.Policies.ViewCourses)]
        public async Task<IActionResult> GetSubjectCourses(int subjectId)
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            var query = _context.Courses
                .Include(c => c.Teachers)
                    .ThenInclude(t => t.User)
                .Where(c => c.SubjectId == subjectId && c.IsActive);

            // Фильтруем курсы в зависимости от роли на уровне базы данных
            if (userRole == UserRole.Teacher)
            {
                query = query.Where(c => c.Teachers.Any(t => t.UserId == currentUserId));
            }
            else if (userRole == UserRole.Student)
            {
                query = query.Where(c => c.Students.Any(s => s.UserId == currentUserId));
            }

            var subject = await _context.Subjects
                .FirstOrDefaultAsync(s => s.Id == subjectId);

            if (subject == null)
                return NotFound("Предмет не найден");

            var courses = await query
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Description,
                    c.CreatedAt,
                    Teachers = c.Teachers.Select(t => new
                    {
                        t.User.Id,
                        t.User.FullName,
                        t.User.Email
                    }),
                    StudentsCount = c.Students.Count()
                })
                .ToListAsync();

            return Ok(new 
            { 
                subject.Name, 
                subject.Code, 
                Courses = courses 
            });
        }

        [HttpGet("student/{studentId}")]
        [Authorize(Policy = UserRole.Policies.ViewCourses)]
        public async Task<IActionResult> GetStudentSubjects(int studentId)
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            // Проверяем права доступа
            if (userRole == UserRole.Student && currentUserId != studentId)
                return Forbid("Вы можете просматривать только свои предметы");

            var subjects = await _context.Subjects
                .Where(s => s.Courses.Any(c => c.Students.Any(cs => cs.UserId == studentId)))
                .Select(s => new
                {
                    s.Id,
                    s.Name,
                    s.Code,
                    s.Description,
                    Courses = s.Courses
                        .Where(c => c.Students.Any(cs => cs.UserId == studentId))
                        .Select(c => new
                        {
                            c.Id,
                            c.Name,
                            c.Description,
                            TeachersCount = c.Teachers.Count,
                            StudentsCount = c.Students.Count
                        })
                })
                .ToListAsync();

            return Ok(subjects);
        }

        // Получение курсов по предмету для конкретного студента
        [HttpGet("student/{studentId}/courses")]
        [Authorize(Policy = UserRole.Policies.ViewCourses)]
        public async Task<IActionResult> GetStudentSubjectCourses(int studentId)
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            // Проверяем права доступа
            if (userRole == UserRole.Student && currentUserId != studentId)
                return Forbid("Вы можете просматривать только свои курсы");

            var subjects = await _context.Subjects
                .Include(s => s.Courses.Where(c => c.IsActive && c.Students.Any(cs => cs.UserId == studentId)))
                    .ThenInclude(c => c.Teachers)
                        .ThenInclude(t => t.User)
                .Where(s => s.Courses.Any(c => c.Students.Any(cs => cs.UserId == studentId)))
                .Select(s => new
                {
                    s.Id,
                    s.Name,
                    s.Code,
                    s.Description,
                    Courses = s.Courses.Select(c => new
                    {
                        c.Id,
                        c.Name,
                        c.Description,
                        Teachers = c.Teachers.Select(t => new { t.User.Id, t.User.FullName }),
                        StudentsCount = c.Students.Count
                    })
                })
                .ToListAsync();

            return Ok(subjects);
        }

        // Создание нового предмета
        [HttpPost]
        [Authorize(Policy = UserRole.Policies.ManageCourses)]
        public async Task<IActionResult> CreateSubject([FromBody] CreateSubjectDto dto)
        {
            if (await _context.Subjects.AnyAsync(s => s.Code == dto.Code))
                return BadRequest("Предмет с таким кодом уже существует");

            var subject = new Subject
            {
                Name = dto.Name,
                Code = dto.Code.ToUpper(), // Автоматически переводим в верхний регистр
                Description = dto.Description
            };

            _context.Subjects.Add(subject);
            await _context.SaveChangesAsync();

            return Ok(new { subjectId = subject.Id });
        }

        // Обновление предмета
        [HttpPut("{id}")]
        [Authorize(Policy = UserRole.Policies.ManageCourses)]
        public async Task<IActionResult> UpdateSubject(int id, [FromBody] CreateSubjectDto dto)
        {
            var subject = await _context.Subjects.FindAsync(id);
            if (subject == null)
                return NotFound("Предмет не найден");

            if (await _context.Subjects.AnyAsync(s => s.Code == dto.Code && s.Id != id))
                return BadRequest("Предмет с таким кодом уже существует");

            subject.Name = dto.Name;
            subject.Code = dto.Code.ToUpper();
            subject.Description = dto.Description;

            await _context.SaveChangesAsync();
            return Ok();
        }

        // Удаление предмета
        [HttpDelete("{id}")]
        [Authorize(Policy = UserRole.Policies.ManageCourses)]
        public async Task<IActionResult> DeleteSubject(int id)
        {
            var subject = await _context.Subjects
                .Include(s => s.Courses)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (subject == null)
                return NotFound("Предмет не найден");

            if (subject.Courses.Any())
                return BadRequest("Нельзя удалить предмет, к которому привязаны курсы");

            _context.Subjects.Remove(subject);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Предмет успешно удален" });
        }
    }
}