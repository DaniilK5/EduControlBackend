using EduControlBackend.Models;
using EduControlBackend.Models.AssignmentModels;
using EduControlBackend.Models.GradeModels;
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
    public class AssignmentController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly FileService _fileService;

        public AssignmentController(ApplicationDbContext context, FileService fileService)
        {
            _context = context;
            _fileService = fileService;
        }

        [HttpPost("{assignmentId}/submit")]
        [Authorize(Policy = UserRole.Policies.SubmitAssignments)]
        public async Task<IActionResult> SubmitAssignment(int assignmentId, [FromForm] SubmitAssignmentDto dto)
        {
            var studentId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var assignment = await _context.Assignments
                .Include(a => a.Course)
                    .ThenInclude(c => c.Students)
                .FirstOrDefaultAsync(a => a.Id == assignmentId);

            if (assignment == null)
                return NotFound("Задание не найдено");

            // Проверяем, записан ли студент на курс
            if (!assignment.Course.Students.Any(s => s.UserId == studentId))
                return Forbid("Вы не записаны на этот курс");

            // Проверяем, не истек ли срок сдачи
            if (assignment.Deadline < DateTime.UtcNow)
                return BadRequest("Срок сдачи задания истек");

            var submission = new AssignmentSubmission
            {
                Content = dto.Content,
                AssignmentId = assignmentId,
                StudentId = studentId
            };

            if (dto.Attachment != null)
            {
                var (path, fileName) = await _fileService.SaveFileAsync(dto.Attachment);
                submission.AttachmentPath = path;
                submission.AttachmentName = fileName;
                submission.AttachmentType = dto.Attachment.ContentType;
            }

            _context.AssignmentSubmissions.Add(submission);
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpPost("submissions/{submissionId}/grade")]
        [Authorize(Policy = UserRole.Policies.ManageGrades)]
        public async Task<IActionResult> GradeSubmission(int submissionId, [FromBody] GradeSubmissionDto dto)
        {
            var instructorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var submission = await _context.AssignmentSubmissions
                .Include(s => s.Assignment)
                    .ThenInclude(a => a.Course)
                        .ThenInclude(c => c.Teachers)
                .FirstOrDefaultAsync(s => s.Id == submissionId);

            if (submission == null)
                return NotFound("Работа не найдена");

            // Проверяем, является ли преподаватель учителем этого курса
            if (!submission.Assignment.Course.Teachers.Any(t => t.UserId == instructorId))
                return Forbid("Вы не являетесь преподавателем этого курса");

            var grade = new Grade
            {
                Value = dto.Grade,
                Comment = dto.Comment,
                AssignmentId = submission.AssignmentId,
                StudentId = submission.StudentId,
                InstructorId = instructorId,
                GradedAt = DateTime.UtcNow
            };

            _context.Grades.Add(grade);
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpGet("submissions/{submissionId}/file")]
        [Authorize(Policy = UserRole.Policies.ViewAssignments)]
        public async Task<IActionResult> GetSubmissionFile(int submissionId)
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            var submission = await _context.AssignmentSubmissions
                .Include(s => s.Assignment)
                    .ThenInclude(a => a.Course)
                .FirstOrDefaultAsync(s => s.Id == submissionId);

            if (submission == null || string.IsNullOrEmpty(submission.AttachmentPath))
                return NotFound();

            // Проверяем права доступа
            bool hasAccess = false;
            if (userRole == UserRole.Administrator)
            {
                hasAccess = true;
            }
            else if (userRole == UserRole.Teacher)
            {
                hasAccess = await _context.Courses
                    .AnyAsync(c => c.Id == submission.Assignment.CourseId && 
                                 c.Teachers.Any(t => t.UserId == currentUserId));
            }
            else if (userRole == UserRole.Student)
            {
                hasAccess = submission.StudentId == currentUserId;
            }

            if (!hasAccess)
                return Forbid();

            try
            {
                var fileBytes = await _fileService.GetFileAsync(submission.AttachmentPath);
                return File(fileBytes, submission.AttachmentType ?? "application/octet-stream", 
                    submission.AttachmentName);
            }
            catch (FileNotFoundException)
            {
                return NotFound();
            }
        }

        [HttpGet("{assignmentId}/submissions")]
        [Authorize(Policy = UserRole.Policies.ViewAssignments)]
        public async Task<IActionResult> GetAssignmentSubmissions(int assignmentId)
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            var assignment = await _context.Assignments
                .Include(a => a.Course)
                .FirstOrDefaultAsync(a => a.Id == assignmentId);

            if (assignment == null)
                return NotFound("Задание не найдено");

            var query = _context.AssignmentSubmissions
                .Include(s => s.Student)
                .Include(s => s.Grade)
                .Where(s => s.AssignmentId == assignmentId);

            // Если студент, показываем только его работы
            if (userRole == UserRole.Student)
            {
                query = query.Where(s => s.StudentId == currentUserId);
            }

            var submissions = await query
                .Select(s => new
                {
                    s.Id,
                    s.Content,
                    s.SubmittedAt,
                    Student = new { s.Student.Id, s.Student.FullName },
                    HasAttachment = s.AttachmentPath != null,
                    s.AttachmentName,
                    Grade = s.Grade != null ? new
                    {
                        s.Grade.Value,
                        s.Grade.Comment,
                        s.Grade.GradedAt
                    } : null
                })
                .ToListAsync();

            return Ok(submissions);
        }
    }
}