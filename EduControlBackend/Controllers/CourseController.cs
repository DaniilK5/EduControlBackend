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

            // ��������� ������������� ��������
            var subject = await _context.Subjects.FirstOrDefaultAsync(s => s.Id == dto.SubjectId);
            if (subject == null)
                return BadRequest("��������� ������� �� ������");

            // ��������� ������������� ��������������
            var teachers = await _context.Users
                .Where(u => dto.TeacherIds.Contains(u.Id))
                .Where(u => u.Role == UserRole.Teacher || u.Role == UserRole.Administrator)
                .ToListAsync();

            if (teachers.Count != dto.TeacherIds.Count)
                return BadRequest("��������� ������������� �� ������� ��� �� ����� ��������������� ����");

            // ��������� ������������� ���������
            var students = await _context.Users
                .Where(u => dto.StudentIds.Contains(u.Id))
                .Where(u => u.Role == UserRole.Student)
                .ToListAsync();

            if (students.Count != dto.StudentIds.Count)
                return BadRequest("��������� �������� �� ������� ��� �� ����� ���� ��������");

            var course = new Course
            {
                Name = dto.Name,
                Description = dto.Description,
                SubjectId = dto.SubjectId, // ��������� ����� � ���������
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

            // ��������� ����� � ����������� �� ����
            if (userRole == UserRole.Teacher)
            {
                query = query.Where(c => c.Teachers.Any(t => t.UserId == currentUserId));
            }
            else if (userRole == UserRole.Student)
            {
                query = query.Where(c => c.Students.Any(s => s.UserId == currentUserId));
            }
            // ������������� ����� ��� �����

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
        /*
        [HttpPost("{courseId}/assignments")]
        [Authorize(Policy = UserRole.Policies.ManageAssignments)]
        public async Task<IActionResult> CreateAssignment(int courseId, [FromBody] Models.CourseModels.CreateAssignmentDto dto)
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var course = await _context.Courses
                .Include(c => c.Teachers)
                .FirstOrDefaultAsync(c => c.Id == courseId);

            if (course == null)
                return NotFound("���� �� ������");

            // ���������, �������� �� ������� ������������ �������������� �����
            if (!course.Teachers.Any(t => t.UserId == currentUserId) && !User.IsInRole(UserRole.Administrator))
                return Forbid("������ ������������� ����� ����� ��������� �������");

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
        */
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
                return NotFound("���� �� ������");

            // ��������� ������ � �����
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
            var course = await _context.Courses
                .Include(c => c.Teachers)
                .Include(c => c.Students)
                .Include(c => c.Subject)
                .FirstOrDefaultAsync(c => c.Id == courseId);

            if (course == null)
                return NotFound("���� �� ������");

            // ��������� ������������� ��������
            var subject = await _context.Subjects.FirstOrDefaultAsync(s => s.Id == dto.SubjectId);
            if (subject == null)
                return BadRequest("��������� ������� �� ������");

            // ��������� ������������� ��������������
            var teachers = await _context.Users
                .Where(u => dto.TeacherIds.Contains(u.Id))
                .Where(u => u.Role == UserRole.Teacher || u.Role == UserRole.Administrator)
                .ToListAsync();

            if (teachers.Count != dto.TeacherIds.Count)
                return BadRequest("��������� ������������� �� ������� ��� �� ����� ��������������� ����");

            if (teachers.Count == 0)
                return BadRequest("���� ������ ����� ���� �� ������ �������������");

            // ��������� ������������� ���������
            var students = await _context.Users
                .Where(u => dto.StudentIds.Contains(u.Id))
                .Where(u => u.Role == UserRole.Student)
                .ToListAsync();

            if (students.Count != dto.StudentIds.Count)
                return BadRequest("��������� �������� �� ������� ��� �� ����� ���� ��������");

            // ��������� �������� ����������
            course.Name = dto.Name;
            course.Description = dto.Description;
            course.IsActive = dto.IsActive;
            course.SubjectId = dto.SubjectId;

            // ��������� ������ ��������������
            var currentTeacherIds = course.Teachers.Select(t => t.UserId).ToList();
            var teachersToRemove = course.Teachers.Where(t => !dto.TeacherIds.Contains(t.UserId)).ToList();
            var teacherIdsToAdd = dto.TeacherIds.Except(currentTeacherIds).ToList();

            foreach (var teacher in teachersToRemove)
            {
                course.Teachers.Remove(teacher);
            }

            foreach (var teacherId in teacherIdsToAdd)
            {
                course.Teachers.Add(new CourseTeacher
                {
                    UserId = teacherId,
                    JoinedAt = DateTime.UtcNow
                });
            }

            // ��������� ������ ���������
            var currentStudentIds = course.Students.Select(s => s.UserId).ToList();
            var studentsToRemove = course.Students.Where(s => !dto.StudentIds.Contains(s.UserId)).ToList();
            var studentIdsToAdd = dto.StudentIds.Except(currentStudentIds).ToList();

            foreach (var student in studentsToRemove)
            {
                course.Students.Remove(student);
            }

            foreach (var studentId in studentIdsToAdd)
            {
                course.Students.Add(new CourseStudent
                {
                    UserId = studentId,
                    EnrolledAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();

            // ���������� ����������� ���������� � �����
            var updatedCourse = new
            {
                course.Id,
                course.Name,
                course.Description,
                course.IsActive,
                Subject = new
                {
                    course.Subject.Id,
                    course.Subject.Name,
                    course.Subject.Code
                },
                Teachers = course.Teachers.Select(t => new
                {
                    UserId = t.UserId,
                    FullName = t.User.FullName,
                    Email = t.User.Email,
                    t.JoinedAt
                }),
                Students = course.Students.Select(s => new
                {
                    UserId = s.UserId,
                    FullName = s.User.FullName,
                    Email = s.User.Email,
                    s.EnrolledAt
                }),
                StudentsCount = course.Students.Count,
                TeachersCount = course.Teachers.Count,
                Changes = new
                {
                    TeachersAdded = teacherIdsToAdd.Count,
                    TeachersRemoved = teachersToRemove.Count,
                    StudentsAdded = studentIdsToAdd.Count,
                    StudentsRemoved = studentsToRemove.Count
                }
            };

            return Ok(updatedCourse);
        }

        [HttpPost("{courseId}/teachers/{teacherId}")]
        [Authorize(Policy = UserRole.Policies.ManageCourses)]
        public async Task<IActionResult> AddTeacher(int courseId, int teacherId)
        {
            var course = await _context.Courses
                .Include(c => c.Teachers)
                .FirstOrDefaultAsync(c => c.Id == courseId);

            if (course == null)
                return NotFound("���� �� ������");

            var teacher = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == teacherId &&
                    (u.Role == UserRole.Teacher || u.Role == UserRole.Administrator));

            if (teacher == null)
                return BadRequest("������������ �� ������ ��� �� �������� ��������������");

            if (course.Teachers.Any(t => t.UserId == teacherId))
                return BadRequest("������������� ��� �������� � ����");

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
                return NotFound("���� �� ������");

            var teacher = course.Teachers.FirstOrDefault(t => t.UserId == teacherId);
            if (teacher == null)
                return NotFound("������������� �� ������ � �����");

            // ���������, ��� ��� �� ��������� �������������
            if (course.Teachers.Count == 1)
                return BadRequest("������ ������� ���������� ������������� �� �����");

            course.Teachers.Remove(teacher);
            await _context.SaveChangesAsync();

            return Ok();
        }
    }
}