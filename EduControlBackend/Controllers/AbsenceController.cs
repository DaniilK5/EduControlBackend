using EduControlBackend.Models;
using EduControlBackend.Models.StudentModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EduControlBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AbsenceController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AbsenceController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Получение статистики пропусков по группе
        [HttpGet("group/{groupId}/statistics")]
        public async Task<IActionResult> GetGroupStatistics(int groupId, 
            [FromQuery] DateTime? startDate = null, 
            [FromQuery] DateTime? endDate = null)
        {
            var group = await _context.StudentGroups
                .Include(g => g.Students)
                .FirstOrDefaultAsync(g => g.Id == groupId);

            if (group == null)
                return NotFound("Группа не найдена");

            var query = _context.Absences
                .Where(a => a.StudentGroupId == groupId);

            if (startDate.HasValue)
                query = query.Where(a => a.Date >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(a => a.Date <= endDate.Value);

            var statistics = await query
                .GroupBy(a => a.StudentId)
                .Select(g => new
                {
                    StudentId = g.Key,
                    Student = g.First().Student.FullName,
                    TotalHours = g.Sum(a => a.Hours),
                    ExcusedHours = g.Where(a => a.IsExcused).Sum(a => a.Hours),
                    UnexcusedHours = g.Where(a => !a.IsExcused).Sum(a => a.Hours),
                    AbsenceDates = g.OrderByDescending(a => a.Date)
                        .Select(a => new
                        {
                            a.Date,
                            a.Hours,
                            a.IsExcused,
                            a.Reason
                        })
                })
                .ToListAsync();

            var totalStatistics = new
            {
                GroupName = group.Name,
                TotalStudents = group.Students.Count,
                TotalAbsenceHours = statistics.Sum(s => s.TotalHours),
                AverageAbsenceHours = statistics.Any() ? 
                    Math.Round(statistics.Average(s => s.TotalHours), 2) : 0,
                ExcusedHours = statistics.Sum(s => s.ExcusedHours),
                UnexcusedHours = statistics.Sum(s => s.UnexcusedHours),
                StudentStatistics = statistics.OrderByDescending(s => s.TotalHours)
            };

            return Ok(totalStatistics);
        }

        // Получение пропусков студента
        [HttpGet("student/{studentId}")]
        public async Task<IActionResult> GetStudentAbsences(int studentId,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            // Проверяем права доступа
            if (userRole == UserRole.Student && currentUserId != studentId)
                return Forbid();

            // Для родителей проверяем, является ли запрашиваемый студент их ребёнком
            if (userRole == UserRole.Parent)
            {
                var hasAccess = await _context.Users
                    .Where(u => u.Id == currentUserId)
                    .SelectMany(u => u.Children)
                    .AnyAsync(c => c.Id == studentId);

                if (!hasAccess)
                    return Forbid("У вас нет прав для просмотра пропусков этого студента");
            }

            var query = _context.Absences
                .Include(a => a.Instructor)
                .Include(a => a.Student)
                .Where(a => a.StudentId == studentId);

            if (startDate.HasValue)
                query = query.Where(a => a.Date >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(a => a.Date <= endDate.Value);

            var absences = await query
                .OrderByDescending(a => a.Date)
                .Select(a => new
                {
                    a.Id,
                    a.Date,
                    a.Hours,
                    a.Reason,
                    a.IsExcused,
                    a.Comment,
                    Student = new 
                    { 
                        a.Student.Id,
                        a.Student.FullName,
                        a.Student.StudentGroup,
                        a.Student.StudentId
                    },
                    Instructor = new 
                    { 
                        a.Instructor.Id,
                        a.Instructor.FullName 
                    },
                    a.CreatedAt
                })
                .ToListAsync();

            var totalHours = absences.Sum(a => a.Hours);
            var excusedHours = absences.Where(a => a.IsExcused).Sum(a => a.Hours);

            return Ok(new
            {
                StudentInfo = absences.FirstOrDefault()?.Student,
                TotalHours = totalHours,
                ExcusedHours = excusedHours,
                UnexcusedHours = totalHours - excusedHours,
                Absences = absences
            });
        }

        // Добавление пропуска
        [HttpPost]
        [Authorize(Policy = UserRole.Policies.ManageStudents)]
        public async Task<IActionResult> CreateAbsence([FromBody] CreateAbsenceDto dto)
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // Проверяем существование студента
            var student = await _context.Users
                .Include(u => u.Group)
                .FirstOrDefaultAsync(u => u.Id == dto.StudentId && u.Role == UserRole.Student);

            if (student == null)
                return NotFound("Студент не найден");

            if (student.Group == null)
                return BadRequest("Студент не привязан к группе");

            var absence = new Absence
            {
                StudentId = dto.StudentId,
                StudentGroupId = student.Group.Id,
                InstructorId = currentUserId,
                Date = dto.Date.Date, // Убираем время
                Hours = dto.Hours,
                Reason = dto.Reason,
                IsExcused = dto.IsExcused,
                Comment = dto.Comment
            };

            _context.Absences.Add(absence);
            await _context.SaveChangesAsync();

            return Ok(new { absenceId = absence.Id });
        }

        // Обновление записи о пропуске
        [HttpPut("{id}")]
        [Authorize(Policy = UserRole.Policies.ManageStudents)]
        public async Task<IActionResult> UpdateAbsence(int id, [FromBody] CreateAbsenceDto dto)
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            var absence = await _context.Absences.FindAsync(id);
            if (absence == null)
                return NotFound("Запись о пропуске не найдена");

            // Только создатель записи или администратор может её изменить
            if (userRole != UserRole.Administrator && absence.InstructorId != currentUserId)
                return Forbid();

            absence.Hours = dto.Hours;
            absence.Reason = dto.Reason;
            absence.IsExcused = dto.IsExcused;
            absence.Comment = dto.Comment;

            await _context.SaveChangesAsync();
            return Ok();
        }

        // Удаление записи о пропуске
        [HttpDelete("{id}")]
        [Authorize(Policy = UserRole.Policies.ManageStudents)]
        public async Task<IActionResult> DeleteAbsence(int id)
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            var absence = await _context.Absences.FindAsync(id);
            if (absence == null)
                return NotFound("Запись о пропуске не найдена");

            // Только создатель записи или администратор может её удалить
            if (userRole != UserRole.Administrator && absence.InstructorId != currentUserId)
                return Forbid();

            _context.Absences.Remove(absence);
            await _context.SaveChangesAsync();
            return Ok();
        }

        // Добавим метод для получения пропусков всех детей родителя
        [HttpGet("parent/children")]
        [Authorize(Roles = UserRole.Parent)]
        public async Task<IActionResult> GetChildrenAbsences(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // Получаем ID всех детей текущего родителя
            var childrenIds = await _context.Users
                .Where(u => u.Id == currentUserId)
                .SelectMany(u => u.Children)
                .Select(c => c.Id)
                .ToListAsync();

            var query = _context.Absences
                .Include(a => a.Student)
                .Include(a => a.Instructor)
                .Where(a => childrenIds.Contains(a.StudentId));

            if (startDate.HasValue)
                query = query.Where(a => a.Date >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(a => a.Date <= endDate.Value);

            var absencesByChild = await query
                .GroupBy(a => a.StudentId)
                .Select(g => new
                {
                    Student = new
                    {
                        g.First().Student.Id,
                        g.First().Student.FullName,
                        g.First().Student.StudentGroup,
                        g.First().Student.StudentId
                    },
                    TotalHours = g.Sum(a => a.Hours),
                    ExcusedHours = g.Where(a => a.IsExcused).Sum(a => a.Hours),
                    UnexcusedHours = g.Where(a => !a.IsExcused).Sum(a => a.Hours),
                    Absences = g.OrderByDescending(a => a.Date)
                        .Select(a => new
                        {
                            a.Id,
                            a.Date,
                            a.Hours,
                            a.Reason,
                            a.IsExcused,
                            a.Comment,
                            Instructor = new { a.Instructor.Id, a.Instructor.FullName },
                            a.CreatedAt
                        })
                })
                .ToListAsync();

            return Ok(absencesByChild);
        }
    }
}