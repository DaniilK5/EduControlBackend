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
    public class ParentStudentController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ParentStudentController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Получение списка детей для родителя
        [HttpGet("parent/{parentId}/children")]
        [Authorize(Policy = UserRole.Policies.ManageStudents)]
        public async Task<IActionResult> GetParentChildren(int parentId)
        {
            var parent = await _context.Users
                .Include(u => u.Children)
                .ThenInclude(c => c.Group)
                .FirstOrDefaultAsync(u => u.Id == parentId && u.Role == UserRole.Parent);

            if (parent == null)
                return NotFound("Родитель не найден");

            var children = parent.Children.Select(c => new
            {
                c.Id,
                c.FullName,
                c.Email,
                c.PhoneNumber,
                c.StudentId,
                Group = c.Group != null ? new { c.Group.Id, c.Group.Name } : null
            });

            return Ok(children);
        }

        // Получение списка родителей для студента
        [HttpGet("student/{studentId}/parents")]
        [Authorize(Policy = UserRole.Policies.ManageStudents)]
        public async Task<IActionResult> GetStudentParents(int studentId)
        {
            var student = await _context.Users
                .Include(u => u.Parents)
                .FirstOrDefaultAsync(u => u.Id == studentId && u.Role == UserRole.Student);

            if (student == null)
                return NotFound("Студент не найден");

            var parents = student.Parents.Select(p => new
            {
                p.Id,
                p.FullName,
                p.Email,
                p.PhoneNumber
            });

            return Ok(parents);
        }

        // Добавление связи родитель-студент
        [HttpPost("add-parent")]
        [Authorize(Policy = UserRole.Policies.ManageStudents)]
        public async Task<IActionResult> AddParentToStudent([FromBody] AddParentDto dto)
        {
            var student = await _context.Users
                .Include(u => u.Parents)
                .FirstOrDefaultAsync(u => u.Id == dto.StudentId && u.Role == UserRole.Student);

            if (student == null)
                return NotFound("Студент не найден");

            var parent = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == dto.ParentId && u.Role == UserRole.Parent);

            if (parent == null)
                return NotFound("Родитель не найден");

            if (student.Parents.Any(p => p.Id == dto.ParentId))
                return BadRequest("Этот родитель уже привязан к студенту");

            student.Parents.Add(parent);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                StudentId = student.Id,
                StudentName = student.FullName,
                ParentId = parent.Id,
                ParentName = parent.FullName
            });
        }

        // Удаление связи родитель-студент
        [HttpDelete("remove-parent")]
        [Authorize(Policy = UserRole.Policies.ManageStudents)]
        public async Task<IActionResult> RemoveParentFromStudent([FromBody] AddParentDto dto)
        {
            var student = await _context.Users
                .Include(u => u.Parents)
                .FirstOrDefaultAsync(u => u.Id == dto.StudentId && u.Role == UserRole.Student);

            if (student == null)
                return NotFound("Студент не найден");

            var parent = student.Parents.FirstOrDefault(p => p.Id == dto.ParentId);
            if (parent == null)
                return NotFound("Родитель не найден или не привязан к студенту");

            student.Parents.Remove(parent);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Связь успешно удалена" });
        }

        // Обновление списка детей для родителя
        [HttpPut("update-parent-children")]
        [Authorize(Policy = UserRole.Policies.ManageStudents)]
        public async Task<IActionResult> UpdateParentChildren([FromBody] UpdateParentStudentsDto dto)
        {
            var parent = await _context.Users
                .Include(u => u.Children)
                .FirstOrDefaultAsync(u => u.Id == dto.ParentId && u.Role == UserRole.Parent);

            if (parent == null)
                return NotFound("Родитель не найден");

            // Получаем всех студентов
            var students = await _context.Users
                .Where(u => dto.StudentIds.Contains(u.Id) && u.Role == UserRole.Student)
                .ToListAsync();

            if (students.Count != dto.StudentIds.Count)
                return BadRequest("Некоторые студенты не найдены");

            // Очищаем текущие связи
            parent.Children.Clear();

            // Добавляем новые связи
            foreach (var student in students)
            {
                parent.Children.Add(student);
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                ParentId = parent.Id,
                ParentName = parent.FullName,
                Children = students.Select(s => new
                {
                    s.Id,
                    s.FullName,
                    s.StudentId
                })
            });
        }

        // Обновление списка родителей для студента
        [HttpPut("update-student-parents")]
        [Authorize(Policy = UserRole.Policies.ManageStudents)]
        public async Task<IActionResult> UpdateStudentParents([FromBody] UpdateStudentParentsDto dto)
        {
            var student = await _context.Users
                .Include(u => u.Parents)
                .FirstOrDefaultAsync(u => u.Id == dto.StudentId && u.Role == UserRole.Student);

            if (student == null)
                return NotFound("Студент не найден");

            // Получаем всех родителей
            var parents = await _context.Users
                .Where(u => dto.ParentIds.Contains(u.Id) && u.Role == UserRole.Parent)
                .ToListAsync();

            if (parents.Count != dto.ParentIds.Count)
                return BadRequest("Некоторые родители не найдены");

            // Очищаем текущие связи
            student.Parents.Clear();

            // Добавляем новые связи
            foreach (var parent in parents)
            {
                student.Parents.Add(parent);
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                StudentId = student.Id,
                StudentName = student.FullName,
                Parents = parents.Select(p => new
                {
                    p.Id,
                    p.FullName,
                    p.Email,
                    p.PhoneNumber
                })
            });
        }


        // Добавление нескольких студентов к родителю
        [HttpPost("parent/{parentId}/add-children")]
        [Authorize(Policy = UserRole.Policies.ManageStudents)]
        public async Task<IActionResult> AddChildrenToParent(int parentId, [FromBody] List<int> studentIds)
        {
            var parent = await _context.Users
                .Include(u => u.Children)
                .FirstOrDefaultAsync(u => u.Id == parentId && u.Role == UserRole.Parent);

            if (parent == null)
                return NotFound("Родитель не найден");

            // Получаем студентов для добавления
            var studentsToAdd = await _context.Users
                .Where(u => studentIds.Contains(u.Id) && u.Role == UserRole.Student)
                .ToListAsync();

            if (studentsToAdd.Count != studentIds.Count)
                return BadRequest("Некоторые студенты не найдены или не являются студентами");

            // Проверяем, не добавлены ли уже некоторые студенты
            var existingStudents = studentsToAdd.Where(s => parent.Children.Any(c => c.Id == s.Id)).ToList();
            if (existingStudents.Any())
            {
                return BadRequest($"Следующие студенты уже привязаны к родителю: {string.Join(", ", existingStudents.Select(s => s.FullName))}");
            }

            // Добавляем новых студентов
            foreach (var student in studentsToAdd)
            {
                parent.Children.Add(student);
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                ParentInfo = new
                {
                    parent.Id,
                    parent.FullName,
                    parent.Email
                },
                AddedChildren = studentsToAdd.Select(s => new
                {
                    s.Id,
                    s.FullName,
                    s.Email,
                    s.StudentId,
                    Group = s.Group != null ? new { s.Group.Id, s.Group.Name } : null
                }),
                TotalChildren = parent.Children.Count
            });
        }


        // Получение списка всех доступных студентов для привязки к родителю
        [HttpGet("available-students")]
        [Authorize(Policy = UserRole.Policies.ManageStudents)]
        public async Task<IActionResult> GetAvailableStudents(int? excludeParentId = null)
        {
            var query = _context.Users
                .Include(u => u.Group)
                .Where(u => u.Role == UserRole.Student);

            // Если указан ID родителя, исключаем студентов, которые уже привязаны к этому родителю
            if (excludeParentId.HasValue)
            {
                var parent = await _context.Users
                    .Include(u => u.Children)
                    .FirstOrDefaultAsync(u => u.Id == excludeParentId && u.Role == UserRole.Parent);

                if (parent != null)
                {
                    var childrenIds = parent.Children.Select(c => c.Id).ToList();
                    query = query.Where(u => !childrenIds.Contains(u.Id));
                }
            }

            var availableStudents = await query
                .Select(s => new
                {
                    s.Id,
                    s.FullName,
                    s.Email,
                    s.StudentId,
                    Group = s.Group != null ? new { s.Group.Id, s.Group.Name } : null,
                    ParentsCount = s.Parents.Count
                })
                .OrderBy(s => s.Group.Name)
                .ThenBy(s => s.FullName)
                .ToListAsync();

            return Ok(availableStudents);
        }

        // Получение статистики по связям родитель-студент
        [HttpGet("statistics")]
        [Authorize(Policy = UserRole.Policies.ManageStudents)]
        public async Task<IActionResult> GetParentStudentStatistics()
        {
            var statistics = new
            {
                TotalParents = await _context.Users.CountAsync(u => u.Role == UserRole.Parent),
                TotalStudents = await _context.Users.CountAsync(u => u.Role == UserRole.Student),
                StudentsWithoutParents = await _context.Users
                    .CountAsync(u => u.Role == UserRole.Student && !u.Parents.Any()),
                ParentsWithoutChildren = await _context.Users
                    .CountAsync(u => u.Role == UserRole.Parent && !u.Children.Any()),
                AverageChildrenPerParent = await _context.Users
                    .Where(u => u.Role == UserRole.Parent)
                    .Select(p => p.Children.Count)
                    .DefaultIfEmpty()
                    .AverageAsync(),
                AverageParentsPerStudent = await _context.Users
                    .Where(u => u.Role == UserRole.Student)
                    .Select(s => s.Parents.Count)
                    .DefaultIfEmpty()
                    .AverageAsync(),
                TopParentsByChildrenCount = await _context.Users
                    .Where(u => u.Role == UserRole.Parent)
                    .Select(p => new
                    {
                        p.Id,
                        p.FullName,
                        ChildrenCount = p.Children.Count
                    })
                    .OrderByDescending(p => p.ChildrenCount)
                    .Take(5)
                    .ToListAsync()
            };

            return Ok(statistics);
        }

        // Получение статистики успеваемости для детей родителя
        [HttpGet("children/performance")]
        [Authorize(Roles = UserRole.Parent)]
        public async Task<IActionResult> GetChildrenPerformance(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            var parentId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // Получаем всех детей родителя
            var children = await _context.Users
                .Include(u => u.Group)
                .Where(u => u.Parents.Any(p => p.Id == parentId))
                .ToListAsync();

            if (!children.Any())
                return NotFound("У вас нет привязанных студентов");

            var performanceData = new List<object>();

            foreach (var child in children)
            {
                // Получаем оценки
                var gradesQuery = _context.Grades
                    .Include(g => g.Assignment)
                        .ThenInclude(a => a.Course)
                            .ThenInclude(c => c.Subject)
                    .Include(g => g.Instructor)
                    .Where(g => g.StudentId == child.Id);

                // Применяем фильтр по датам
                if (startDate.HasValue)
                    gradesQuery = gradesQuery.Where(g => g.GradedAt.Date >= startDate.Value.Date);
                if (endDate.HasValue)
                    gradesQuery = gradesQuery.Where(g => g.GradedAt.Date <= endDate.Value.Date);

                var grades = await gradesQuery
                    .OrderByDescending(g => g.GradedAt)
                    .ToListAsync();

                // Получаем пропуски
                var absencesQuery = _context.Absences
                    .Where(a => a.StudentId == child.Id);

                if (startDate.HasValue)
                    absencesQuery = absencesQuery.Where(a => a.Date.Date >= startDate.Value.Date);
                if (endDate.HasValue)
                    absencesQuery = absencesQuery.Where(a => a.Date.Date <= endDate.Value.Date);

                var absences = await absencesQuery.ToListAsync();

                // Группируем оценки по предметам
                var subjectPerformance = grades
                    .GroupBy(g => g.Assignment.Course.Subject.Name)
                    .Select(g => new
                    {
                        Subject = g.Key,
                        AverageGrade = g.Average(x => x.Value),
                        GradesCount = g.Count(),
                        MinGrade = g.Min(x => x.Value),
                        MaxGrade = g.Max(x => x.Value),
                        LatestGrades = g.OrderByDescending(x => x.GradedAt)
                            .Take(5)
                            .Select(x => new
                            {
                                x.Value,
                                x.GradedAt,
                                Assignment = x.Assignment.Title,
                                Instructor = x.Instructor.FullName
                            })
                    })
                    .OrderByDescending(x => x.AverageGrade)
                    .ToList();

                // Формируем статистику по пропускам
                var absenceStats = new
                {
                    TotalHours = absences.Sum(a => a.Hours),
                    ExcusedHours = absences.Where(a => a.IsExcused).Sum(a => a.Hours),
                    UnexcusedHours = absences.Where(a => !a.IsExcused).Sum(a => a.Hours),
                    LatestAbsences = absences
                        .OrderByDescending(a => a.Date)
                        .Take(5)
                        .Select(a => new
                        {
                            a.Date,
                            a.Hours,
                            a.IsExcused,
                            a.Reason
                        })
                };

                performanceData.Add(new
                {
                    StudentInfo = new
                    {
                        child.Id,
                        child.FullName,
                        child.StudentId,
                        Group = child.Group != null ? new { child.Group.Id, child.Group.Name } : null
                    },
                    OverallPerformance = new
                    {
                        AverageGrade = grades.Any() ? Math.Round(grades.Average(g => g.Value), 2) : 0,
                        TotalGrades = grades.Count,
                        GradeDistribution = new
                        {
                            Excellent = grades.Count(g => g.Value >= 90),
                            Good = grades.Count(g => g.Value >= 75 && g.Value < 90),
                            Satisfactory = grades.Count(g => g.Value >= 60 && g.Value < 75),
                            Poor = grades.Count(g => g.Value < 60)
                        }
                    },
                    SubjectsPerformance = subjectPerformance,
                    AttendanceStats = absenceStats,
                    Period = new
                    {
                        StartDate = startDate?.ToString("yyyy-MM-dd"),
                        EndDate = endDate?.ToString("yyyy-MM-dd"),
                        FirstGradeDate = grades.Any() ? grades.Min(g => g.GradedAt).ToString("yyyy-MM-dd") : null,
                        LastGradeDate = grades.Any() ? grades.Max(g => g.GradedAt).ToString("yyyy-MM-dd") : null
                    }
                });
            }

            return Ok(new
            {
                TotalChildren = children.Count,
                PerformanceData = performanceData
            });
        }

        // Получение детальной статистики по конкретному студенту
        [HttpGet("student/{studentId}/detailed-performance")]
        [Authorize(Roles = UserRole.Parent)]
        public async Task<IActionResult> GetDetailedStudentPerformance(
            int studentId,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            var parentId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // Проверяем, является ли указанный студент ребенком этого родителя
            var student = await _context.Users
                .Include(u => u.Group)
                .Include(u => u.Parents)
                .FirstOrDefaultAsync(u => u.Id == studentId && u.Role == UserRole.Student);

            if (student == null || !student.Parents.Any(p => p.Id == parentId))
                return NotFound("Студент не найден или не является вашим ребенком");

            // Получаем все оценки с детальной информацией
            var gradesQuery = _context.Grades
                .Include(g => g.Assignment)
                    .ThenInclude(a => a.Course)
                        .ThenInclude(c => c.Subject)
                .Include(g => g.Instructor)
                .Include(g => g.Submission)
                .Where(g => g.StudentId == studentId);

            // Применяем фильтры по датам
            if (startDate.HasValue)
                gradesQuery = gradesQuery.Where(g => g.GradedAt.Date >= startDate.Value.Date);
            if (endDate.HasValue)
                gradesQuery = gradesQuery.Where(g => g.GradedAt.Date <= endDate.Value.Date);

            var grades = await gradesQuery.OrderByDescending(g => g.GradedAt).ToListAsync();

            // Группируем по предметам и месяцам
            var subjectPerformanceByMonth = grades
                .GroupBy(g => new { Subject = g.Assignment.Course.Subject.Name, Month = g.GradedAt.ToString("yyyy-MM") })
                .Select(g => new
                {
                    g.Key.Subject,
                    g.Key.Month,
                    AverageGrade = Math.Round(g.Average(x => x.Value), 2),
                    GradesCount = g.Count(),
                    AllGrades = g.Select(x => new
                    {
                        x.Value,
                        x.GradedAt,
                        Assignment = x.Assignment.Title,
                        x.Comment,
                        HasSubmission = x.Submission != null,
                        Instructor = x.Instructor.FullName
                    }).OrderByDescending(x => x.GradedAt)
                })
                .GroupBy(g => g.Subject)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderBy(x => x.Month).ToList()
                );

            return Ok(new
            {
                StudentInfo = new
                {
                    student.Id,
                    student.FullName,
                    student.StudentId,
                    Group = student.Group != null ? new { student.Group.Id, student.Group.Name } : null
                },
                Period = new
                {
                    StartDate = startDate?.ToString("yyyy-MM-dd"),
                    EndDate = endDate?.ToString("yyyy-MM-dd")
                },
                PerformanceBySubject = subjectPerformanceByMonth,
                Summary = new
                {
                    TotalGrades = grades.Count,
                    AverageGrade = grades.Any() ? Math.Round(grades.Average(g => g.Value), 2) : 0,
                    HighestGrade = grades.Any() ? grades.Max(g => g.Value) : 0,
                    LowestGrade = grades.Any() ? grades.Min(g => g.Value) : 0,
                    GradeDistribution = new
                    {
                        Excellent = grades.Count(g => g.Value >= 90),
                        Good = grades.Count(g => g.Value >= 75 && g.Value < 90),
                        Satisfactory = grades.Count(g => g.Value >= 60 && g.Value < 75),
                        Poor = grades.Count(g => g.Value < 60)
                    }
                }
            });
        }


        // Получение пропусков всех детей родителя
        [HttpGet("children/absences")]
        [Authorize(Roles = UserRole.Parent)]
        public async Task<IActionResult> GetChildrenAbsences(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            var parentId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // Получаем всех детей родителя
            var children = await _context.Users
                .Include(u => u.Group)
                .Where(u => u.Parents.Any(p => p.Id == parentId))
                .ToListAsync();

            if (!children.Any())
                return NotFound("У вас нет привязанных студентов");

            var absencesData = new List<object>();

            foreach (var child in children)
            {
                // Формируем запрос пропусков
                var absencesQuery = _context.Absences
                    .Include(a => a.StudentGroup)
                    .Include(a => a.Instructor)
                    .Where(a => a.StudentId == child.Id);

                // Применяем фильтры по датам
                if (startDate.HasValue)
                    absencesQuery = absencesQuery.Where(a => a.Date.Date >= startDate.Value.Date);
                if (endDate.HasValue)
                    absencesQuery = absencesQuery.Where(a => a.Date.Date <= endDate.Value.Date);

                var absences = await absencesQuery
                    .OrderByDescending(a => a.Date)
                    .Select(a => new
                    {
                        a.Id,
                        a.Date,
                        a.Hours,
                        a.Reason,
                        a.IsExcused,
                        a.Comment,
                        a.CreatedAt,
                        Instructor = new { a.Instructor.Id, a.Instructor.FullName }
                    })
                    .ToListAsync();

                // Группируем пропуски по месяцам
                var absencesByMonth = absences
                    .GroupBy(a => a.Date.ToString("yyyy-MM"))
                    .Select(g => new
                    {
                        Month = g.Key,
                        TotalHours = g.Sum(a => a.Hours),
                        ExcusedHours = g.Where(a => a.IsExcused).Sum(a => a.Hours),
                        UnexcusedHours = g.Where(a => !a.IsExcused).Sum(a => a.Hours),
                        Absences = g.OrderByDescending(a => a.Date)
                    })
                    .OrderByDescending(m => m.Month)
                    .ToList();

                absencesData.Add(new
                {
                    StudentInfo = new
                    {
                        child.Id,
                        child.FullName,
                        child.StudentId,
                        Group = child.Group != null ? new { child.Group.Id, child.Group.Name } : null
                    },
                    AbsenceSummary = new
                    {
                        TotalHours = absences.Sum(a => a.Hours),
                        ExcusedHours = absences.Where(a => a.IsExcused).Sum(a => a.Hours),
                        UnexcusedHours = absences.Where(a => !a.IsExcused).Sum(a => a.Hours),
                        AbsencesCount = absences.Count,
                        AbsenceDays = absences.Select(a => a.Date.Date).Distinct().Count()
                    },
                    AbsencesByMonth = absencesByMonth,
                    Period = new
                    {
                        StartDate = startDate?.ToString("yyyy-MM-dd"),
                        EndDate = endDate?.ToString("yyyy-MM-dd"),
                        FirstAbsenceDate = absences.Any() ? absences.Min(a => a.Date).ToString("yyyy-MM-dd") : null,
                        LastAbsenceDate = absences.Any() ? absences.Max(a => a.Date).ToString("yyyy-MM-dd") : null
                    }
                });
            }

            return Ok(new
            {
                TotalChildren = children.Count,
                AbsencesData = absencesData
            });
        }

        // Получение детальной информации о пропусках конкретного студента
        [HttpGet("student/{studentId}/absences")]
        [Authorize(Roles = UserRole.Parent)]
        public async Task<IActionResult> GetStudentDetailedAbsences(
            int studentId,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            var parentId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // Проверяем, является ли указанный студент ребенком этого родителя
            var student = await _context.Users
                .Include(u => u.Group)
                .Include(u => u.Parents)
                .FirstOrDefaultAsync(u => u.Id == studentId && u.Role == UserRole.Student);

            if (student == null || !student.Parents.Any(p => p.Id == parentId))
                return NotFound("Студент не найден или не является вашим ребенком");

            // Формируем запрос пропусков
            var absencesQuery = _context.Absences
                .Include(a => a.StudentGroup)
                .Include(a => a.Instructor)
                .Where(a => a.StudentId == studentId);

            // Применяем фильтры по датам
            if (startDate.HasValue)
                absencesQuery = absencesQuery.Where(a => a.Date.Date >= startDate.Value.Date);
            if (endDate.HasValue)
                absencesQuery = absencesQuery.Where(a => a.Date.Date <= endDate.Value.Date);

            var absences = await absencesQuery
                .OrderByDescending(a => a.Date)
                .Select(a => new
                {
                    a.Id,
                    a.Date,
                    a.Hours,
                    a.Reason,
                    a.IsExcused,
                    a.Comment,
                    a.CreatedAt,
                    Instructor = new { a.Instructor.Id, a.Instructor.FullName }
                })
                .ToListAsync();

            // Группируем по месяцам и причинам
            var absenceAnalytics = new
            {
                ByMonth = absences
                    .GroupBy(a => a.Date.ToString("yyyy-MM"))
                    .Select(g => new
                    {
                        Month = g.Key,
                        TotalHours = g.Sum(a => a.Hours),
                        ExcusedHours = g.Where(a => a.IsExcused).Sum(a => a.Hours),
                        UnexcusedHours = g.Where(a => !a.IsExcused).Sum(a => a.Hours),
                        Absences = g.OrderByDescending(a => a.Date)
                    })
                    .OrderByDescending(m => m.Month),

                ByReason = absences
                    .GroupBy(a => a.Reason ?? "Причина не указана")
                    .Select(g => new
                    {
                        Reason = g.Key,
                        TotalHours = g.Sum(a => a.Hours),
                        OccurrenceCount = g.Count(),
                        IsExcused = g.All(a => a.IsExcused)
                    })
                    .OrderByDescending(r => r.TotalHours)
            };

            return Ok(new
            {
                StudentInfo = new
                {
                    student.Id,
                    student.FullName,
                    student.StudentId,
                    Group = student.Group != null ? new { student.Group.Id, student.Group.Name } : null
                },
                Period = new
                {
                    StartDate = startDate?.ToString("yyyy-MM-dd"),
                    EndDate = endDate?.ToString("yyyy-MM-dd")
                },
                Summary = new
                {
                    TotalHours = absences.Sum(a => a.Hours),
                    ExcusedHours = absences.Where(a => a.IsExcused).Sum(a => a.Hours),
                    UnexcusedHours = absences.Where(a => !a.IsExcused).Sum(a => a.Hours),
                    AbsencesCount = absences.Count,
                    AbsenceDays = absences.Select(a => a.Date.Date).Distinct().Count(),
                    AverageHoursPerAbsence = absences.Any() ?
                        Math.Round(absences.Average(a => a.Hours), 1) : 0
                },
                Analytics = absenceAnalytics,
                AllAbsences = absences
            });
        }
    }
}