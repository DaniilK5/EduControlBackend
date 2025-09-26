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
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            // Проверяем существование студента
            var student = await _context.Users
                .Include(u => u.Group)
                .FirstOrDefaultAsync(u => u.Id == dto.StudentId && u.Role == UserRole.Student);

            if (student == null)
                return NotFound("Студент не найден");

            if (student.Group == null)
                return BadRequest("Студент не привязан к группе");

            // Проверяем права доступа для преподавателей
            if (userRole == UserRole.Teacher)
            {
                // Проверяем, является ли преподаватель куратором группы
                bool isCurator = student.Group.CuratorId == currentUserId;

                // Проверяем, преподаёт ли учитель в группе студента
                bool isTeaching = await _context.CourseTeachers
                    .Where(ct => ct.UserId == currentUserId)
                    .Join(_context.CourseStudents,
                        ct => ct.CourseId,
                        cs => cs.CourseId,
                        (ct, cs) => cs.UserId)
                    .AnyAsync(studentId => studentId == student.Id);

                if (!isCurator && !isTeaching)
                    return Forbid("Вы можете отмечать пропуски только для студентов из ваших групп");
            }
            // Для администраторов пропускаем проверку

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


        [HttpGet("available-students")]
        [Authorize(Policy = UserRole.Policies.ManageStudents)]
        public async Task<IActionResult> GetAvailableStudents()
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            var query = _context.Users
                .Include(u => u.Group)
                    .ThenInclude(g => g.Curator)
                .Where(u => u.Role == UserRole.Student && u.StudentGroupId != null)
                .AsQueryable();

            // Для преподавателей фильтруем студентов
            if (userRole == UserRole.Teacher)
            {
                // Получаем студентов из групп, где преподаватель является куратором
                var curatedStudents = query.Where(u => u.Group.CuratorId == currentUserId);

                // Получаем студентов из групп, где преподаватель ведёт занятия
                var taughtStudents = query.Where(student =>
                    _context.CourseTeachers
                        .Where(ct => ct.UserId == currentUserId)
                        .Join(_context.CourseStudents,
                            ct => ct.CourseId,
                            cs => cs.CourseId,
                            (ct, cs) => cs.UserId)
                        .Contains(student.Id));

                // Объединяем результаты
                query = curatedStudents.Union(taughtStudents);
            }

            var monthAgo = DateTime.UtcNow.AddMonths(-1);
            
            // Получаем все пропуски за последний месяц отдельным запросом
            var recentAbsences = await _context.Absences
                .Where(a => a.Date >= monthAgo)
                .Select(a => new
                {
                    a.StudentId,
                    a.Date,
                    a.Hours,
                    a.IsExcused,
                    a.Reason
                })
                .ToListAsync();

            var students = await query
                .OrderBy(s => s.Group.Name)
                .ThenBy(s => s.FullName)
                .Select(s => new
                {
                    s.Id,
                    s.FullName,
                    s.StudentId,
                    Group = new
                    {
                        s.Group.Id,
                        s.Group.Name,
                        Curator = s.Group.Curator != null ? new
                        {
                            s.Group.Curator.Id,
                            s.Group.Curator.FullName
                        } : null
                    }
                })
                .ToListAsync();

            // Группируем студентов по группам и добавляем информацию о пропусках
            var groupedStudents = students
                .GroupBy(s => s.Group.Name)
                .Select(g => new
                {
                    GroupName = g.Key,
                    GroupId = g.First().Group.Id,
                    Curator = g.First().Group.Curator,
                    Students = g.Select(s =>
                    {
                        var studentAbsences = recentAbsences
                            .Where(a => a.StudentId == s.Id)
                            .OrderByDescending(a => a.Date)
                            .ToList();

                        return new
                        {
                            s.Id,
                            s.FullName,
                            s.StudentId,
                            TotalAbsenceHours = studentAbsences.Sum(a => a.Hours),
                            ExcusedHours = studentAbsences.Where(a => a.IsExcused).Sum(a => a.Hours),
                            UnexcusedHours = studentAbsences.Where(a => !a.IsExcused).Sum(a => a.Hours),
                            RecentAbsences = studentAbsences
                        };
                    }).OrderBy(s => s.FullName)
                })
                .OrderBy(g => g.GroupName);

            return Ok(new
            {
                TotalGroups = groupedStudents.Count(),
                TotalStudents = students.Count,
                Groups = groupedStudents
            });
        }


        // Получение статистики пропусков по всем группам
        [HttpGet("groups/statistics")]
        [Authorize(Policy = UserRole.Policies.ManageStudents)] // Только для преподавателей и администраторов
        public async Task<IActionResult> GetAllGroupsStatistics(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            var query = _context.StudentGroups
                .Include(g => g.Students)
                .Include(g => g.Curator)
                .AsQueryable();

            var groupsData = await query.Select(group => new
            {
                GroupInfo = new
                {
                    group.Id,
                    group.Name,
                    group.Description,
                    StudentsCount = group.Students.Count,
                    Curator = group.Curator != null ? new 
                    { 
                        group.Curator.Id,
                        group.Curator.FullName 
                    } : null
                },
                Absences = _context.Absences
                    .Where(a => a.StudentGroupId == group.Id)
                    .Where(a => !startDate.HasValue || a.Date >= startDate.Value)
                    .Where(a => !endDate.HasValue || a.Date <= endDate.Value)
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
                    .OrderByDescending(s => s.TotalHours)
                    .ToList(),
            })
            .ToListAsync();

            // Подсчитываем общую статистику по всем группам
            var totalStatistics = new
            {
                TotalGroups = groupsData.Count,
                TotalStudents = groupsData.Sum(g => g.GroupInfo.StudentsCount),
                TotalAbsenceHours = groupsData.Sum(g => g.Absences.Sum(s => s.TotalHours)),
                ExcusedHours = groupsData.Sum(g => g.Absences.Sum(s => s.ExcusedHours)),
                UnexcusedHours = groupsData.Sum(g => g.Absences.Sum(s => s.UnexcusedHours)),
                AverageAbsenceHoursPerGroup = groupsData.Any() ? 
                    Math.Round(groupsData.Average(g => 
                        g.Absences.Any() ? g.Absences.Sum(s => s.TotalHours) : 0), 2) : 0,
                GroupsWithNoAbsences = groupsData.Count(g => !g.Absences.Any()),
                Period = new
                {
                    StartDate = startDate?.ToString("yyyy-MM-dd"),
                    EndDate = endDate?.ToString("yyyy-MM-dd")
                }
            };

            var result = new
            {
                TotalStatistics = totalStatistics,
                GroupsDetails = groupsData.Select(g => new
                {
                    g.GroupInfo,
                    Statistics = new
                    {
                        TotalAbsenceHours = g.Absences.Sum(s => s.TotalHours),
                        ExcusedHours = g.Absences.Sum(s => s.ExcusedHours),
                        UnexcusedHours = g.Absences.Sum(s => s.UnexcusedHours),
                        AverageAbsenceHoursPerStudent = g.Absences.Any() && g.GroupInfo.StudentsCount > 0 ? 
                            Math.Round((double)g.Absences.Sum(s => s.TotalHours) / g.GroupInfo.StudentsCount, 2) : 0,
                        StudentsWithAbsences = g.Absences.Count,
                        TopAbsentStudents = g.Absences
                            .OrderByDescending(s => s.TotalHours)
                            .Take(5)
                            .Select(s => new
                            {
                                StudentId = s.StudentId,
                                StudentName = s.Student,
                                s.TotalHours,
                                s.ExcusedHours,
                                s.UnexcusedHours
                            })
                    }
                })
                .OrderByDescending(g => g.Statistics.TotalAbsenceHours)
            };

            return Ok(result);
        }
    }
}