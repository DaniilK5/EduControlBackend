using EduControlBackend.Models;
using EduControlBackend.Models.AssignmentModels;
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
    public class CourseController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CourseController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        [Authorize(Policy = UserRole.Policies.ManageCourses)]
        public async Task<IActionResult> CreateCourse([FromBody] CreateCourseDto dto)
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // Проверяем существование предмета
            var subject = await _context.Subjects.FirstOrDefaultAsync(s => s.Id == dto.SubjectId);
            if (subject == null)
                return BadRequest("Указанный предмет не найден");

            // Проверяем существование преподавателей
            var teachers = await _context.Users
                .Where(u => dto.TeacherIds.Contains(u.Id))
                .Where(u => u.Role == UserRole.Teacher || u.Role == UserRole.Administrator)
                .ToListAsync();

            if (teachers.Count != dto.TeacherIds.Count)
                return BadRequest("Некоторые преподаватели не найдены или не имеют соответствующей роли");

            // Проверяем существование студентов
            var students = await _context.Users
                .Where(u => dto.StudentIds.Contains(u.Id))
                .Where(u => u.Role == UserRole.Student)
                .ToListAsync();

            if (students.Count != dto.StudentIds.Count)
                return BadRequest("Некоторые студенты не найдены или не имеют роли студента");

            var course = new Course
            {
                Name = dto.Name,
                Description = dto.Description,
                SubjectId = dto.SubjectId, // Добавляем связь с предметом
                CreatedAt = DateTime.UtcNow,
                Teachers = teachers.Select(t => new CourseTeacher 
                { 
                    UserId = t.Id,
                    JoinedAt = DateTime.UtcNow
                }).ToList(),
                Students = students.Select(s => new CourseStudent 
                { 
                    UserId = s.Id,
                    EnrolledAt = DateTime.UtcNow
                }).ToList()
            };

            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            return Ok(new { courseId = course.Id });
        }

        [HttpGet]
        [Authorize(Policy = UserRole.Policies.ViewCourses)]
        public async Task<IActionResult> GetCourses()
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            var query = _context.Courses
                .Include(c => c.Teachers)
                    .ThenInclude(ct => ct.User)
                .Include(c => c.Students)
                    .ThenInclude(cs => cs.User)
                .AsQueryable();

            // Фильтруем курсы в зависимости от роли
            if (userRole == UserRole.Teacher)
            {
                query = query.Where(c => c.Teachers.Any(t => t.UserId == currentUserId));
            }
            else if (userRole == UserRole.Student)
            {
                query = query.Where(c => c.Students.Any(s => s.UserId == currentUserId));
            }
            // Администратор видит все курсы

            var courses = await query
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Description,
                    c.CreatedAt,
                    c.IsActive,
                    Teachers = c.Teachers.Select(t => new
                    {
                        t.User.Id,
                        t.User.FullName,
                        t.User.Email,
                        t.JoinedAt
                    }),
                    StudentsCount = c.Students.Count
                })
                .ToListAsync();

            return Ok(courses);
        }

        [HttpPost("{courseId}/assignments")]
        [Authorize(Policy = UserRole.Policies.ManageAssignments)]
        public async Task<IActionResult> CreateAssignment(int courseId, [FromBody] CreateAssignmentDto dto)
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var course = await _context.Courses
                .Include(c => c.Teachers)
                .FirstOrDefaultAsync(c => c.Id == courseId);

            if (course == null)
                return NotFound("Курс не найден");

            // Проверяем, является ли текущий пользователь преподавателем курса
            if (!course.Teachers.Any(t => t.UserId == currentUserId) && !User.IsInRole(UserRole.Administrator))
                return Forbid("Только преподаватели курса могут добавлять задания");

            var assignment = new Assignment
            {
                Title = dto.Title,
                Description = dto.Description,
                Deadline = dto.Deadline,
                CourseId = courseId,
                InstructorId = currentUserId
            };

            _context.Assignments.Add(assignment);
            await _context.SaveChangesAsync();

            return Ok(new { assignmentId = assignment.Id });
        }

        [HttpGet("{courseId}/assignments")]
        [Authorize(Policy = UserRole.Policies.ViewAssignments)]
        public async Task<IActionResult> GetCourseAssignments(int courseId)
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            var course = await _context.Courses
                .Include(c => c.Teachers)
                .Include(c => c.Students)
                .FirstOrDefaultAsync(c => c.Id == courseId);

            if (course == null)
                return NotFound("Курс не найден");

            // Проверяем доступ к курсу
            if (userRole == UserRole.Teacher && !course.Teachers.Any(t => t.UserId == currentUserId))
                return Forbid();
            if (userRole == UserRole.Student && !course.Students.Any(s => s.UserId == currentUserId))
                return Forbid();

            var assignments = await _context.Assignments
                .Include(a => a.Instructor)
                .Where(a => a.CourseId == courseId)
                .Select(a => new
                {
                    a.Id,
                    a.Title,
                    a.Description,
                    a.Deadline,
                    Instructor = new { a.Instructor.Id, a.Instructor.FullName }
                })
                .ToListAsync();

            return Ok(assignments);
        }

        [HttpPut("{courseId}")]
        [Authorize(Policy = UserRole.Policies.ManageCourses)]
        public async Task<IActionResult> UpdateCourse(int courseId, [FromBody] UpdateCourseDto dto)
        {
            var course = await _context.Courses.FindAsync(courseId);
            if (course == null)
                return NotFound("Курс не найден");

            course.Name = dto.Name;
            course.Description = dto.Description;
            course.IsActive = dto.IsActive;

            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPost("{courseId}/teachers/{teacherId}")]
        [Authorize(Policy = UserRole.Policies.ManageCourses)]
        public async Task<IActionResult> AddTeacher(int courseId, int teacherId)
        {
            var course = await _context.Courses
                .Include(c => c.Teachers)
                .FirstOrDefaultAsync(c => c.Id == courseId);

            if (course == null)
                return NotFound("Курс не найден");

            var teacher = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == teacherId &&
                    (u.Role == UserRole.Teacher || u.Role == UserRole.Administrator));

            if (teacher == null)
                return BadRequest("Пользователь не найден или не является преподавателем");

            if (course.Teachers.Any(t => t.UserId == teacherId))
                return BadRequest("Преподаватель уже добавлен в курс");

            var courseTeacher = new CourseTeacher
            {
                CourseId = courseId,
                UserId = teacherId,
                JoinedAt = DateTime.UtcNow
            };

            course.Teachers.Add(courseTeacher);
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpDelete("{courseId}/teachers/{teacherId}")]
        [Authorize(Policy = UserRole.Policies.ManageCourses)]
        public async Task<IActionResult> RemoveTeacher(int courseId, int teacherId)
        {
            var course = await _context.Courses
                .Include(c => c.Teachers)
                .FirstOrDefaultAsync(c => c.Id == courseId);

            if (course == null)
                return NotFound("Курс не найден");

            var teacher = course.Teachers.FirstOrDefault(t => t.UserId == teacherId);
            if (teacher == null)
                return NotFound("Преподаватель не найден в курсе");

            // Проверяем, что это не последний преподаватель
            if (course.Teachers.Count == 1)
                return BadRequest("Нельзя удалить последнего преподавателя из курса");

            course.Teachers.Remove(teacher);
            await _context.SaveChangesAsync();

            return Ok();
        }
    }
}