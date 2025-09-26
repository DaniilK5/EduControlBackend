using EduControlBackend;
using EduControlBackend.Models;
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

        // Получение оценок текущего студента
        [HttpGet("me/grades")]
        [Authorize(Roles = UserRole.Student)]
        public async Task<IActionResult> GetMyGrades(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int? subjectId = null)
        {
            var studentId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // Формируем базовый запрос
            var gradesQuery = _context.Grades
                .Include(g => g.Assignment)
                    .ThenInclude(a => a.Course)
                        .ThenInclude(c => c.Subject)
                .Include(g => g.Instructor)
                .Where(g => g.StudentId == studentId);

            // Применяем фильтры
            if (startDate.HasValue)
                gradesQuery = gradesQuery.Where(g => g.GradedAt >= startDate.Value);
            if (endDate.HasValue)
                gradesQuery = gradesQuery.Where(g => g.GradedAt <= endDate.Value);
            if (subjectId.HasValue)
                gradesQuery = gradesQuery.Where(g => g.Assignment.Course.SubjectId == subjectId);

            // Получаем оценки
            var grades = await gradesQuery
                .OrderByDescending(g => g.GradedAt)
                .Select(g => new
                {
                    g.Id,
                    g.Value,
                    g.GradedAt,
                    g.Comment,
                    Assignment = new
                    {
                        g.Assignment.Id,
                        g.Assignment.Title,
                        Course = new
                        {
                            g.Assignment.Course.Id,
                            g.Assignment.Course.Name,
                            Subject = new
                            {
                                g.Assignment.Course.Subject.Id,
                                g.Assignment.Course.Subject.Name,
                                g.Assignment.Course.Subject.Code
                            }
                        }
                    },
                    Instructor = new
                    {
                        g.Instructor.Id,
                        g.Instructor.FullName
                    }
                })
                .ToListAsync();

            // Группируем по предметам для статистики
            var subjectsPerformance = grades
                .GroupBy(g => g.Assignment.Course.Subject.Id)
                .Select(g => new
                {
                    SubjectId = g.Key,
                    SubjectName = g.First().Assignment.Course.Subject.Name,
                    SubjectCode = g.First().Assignment.Course.Subject.Code,
                    AverageGrade = Math.Round(g.Average(x => x.Value), 2),
                    GradesCount = g.Count(),
                    MinGrade = g.Min(x => x.Value),
                    MaxGrade = g.Max(x => x.Value),
                    LatestGrades = g.OrderByDescending(x => x.GradedAt).Take(5)
                })
                .OrderByDescending(x => x.AverageGrade)
                .ToList();

            return Ok(new
            {
                TotalGrades = grades.Count,
                AverageGrade = grades.Any() ? Math.Round(grades.Average(g => g.Value), 2) : 0,
                GradeDistribution = new
                {
                    Excellent = grades.Count(g => g.Value >= 90),
                    Good = grades.Count(g => g.Value >= 75 && g.Value < 90),
                    Satisfactory = grades.Count(g => g.Value >= 60 && g.Value < 75),
                    Poor = grades.Count(g => g.Value < 60)
                },
                SubjectsPerformance = subjectsPerformance,
                Period = new
                {
                    StartDate = startDate?.ToString("yyyy-MM-dd"),
                    EndDate = endDate?.ToString("yyyy-MM-dd"),
                    FirstGradeDate = grades.Any() ? grades.Min(g => g.GradedAt).ToString("yyyy-MM-dd") : null,
                    LastGradeDate = grades.Any() ? grades.Max(g => g.GradedAt).ToString("yyyy-MM-dd") : null
                },
                Grades = grades
            });
        }
        // Получение списка преподавателей с возможностью фильтрации по занятости
        [HttpGet("teachers")]
        [Authorize(Policy = UserRole.Policies.ManageCourses)]
        public async Task<IActionResult> GetTeachers([FromQuery] bool onlyAvailable = false)
        {
            var query = _context.Users
                .Include(u => u.CuratedGroups)
                .Where(u => u.Role == UserRole.Teacher ||
                            (u.Role == UserRole.Administrator &&
                             _context.CourseTeachers.Any(ct => ct.UserId == u.Id)));

            if (onlyAvailable)
            {
                // Исключаем преподавателей, которые уже являются кураторами групп
                query = query.Where(u => !u.CuratedGroups.Any());
            }

            var teachers = await query
                .Select(t => new
                {
                    t.Id,
                    t.FullName,
                    t.Email,
                    t.PhoneNumber,
                    IsCurator = t.CuratedGroups.Any(),
                    CuratedGroups = t.CuratedGroups.Select(g => new
                    {
                        g.Id,
                        g.Name
                    }).ToList(),
                    TeachingCourses = _context.CourseTeachers
                        .Where(ct => ct.UserId == t.Id)
                        .Select(ct => new
                        {
                            ct.Course.Id,
                            ct.Course.Name,
                            SubjectName = ct.Course.Subject.Name
                        }).ToList()
                })
                .OrderBy(t => t.FullName)
                .ToListAsync();

            return Ok(teachers);
        }

        [HttpGet("students")]
        [Authorize(Policy = UserRole.Policies.ManageStudents)]
        public async Task<IActionResult> GetStudents(
            [FromQuery] bool withoutGroup = false,
            [FromQuery] int? groupId = null,
            [FromQuery] string? searchTerm = null)
        {
            // Базовый запрос студентов
            var query = _context.Users
                .Include(u => u.Group)
                    .ThenInclude(g => g.Curator)
                .Where(u => u.Role == UserRole.Student);

            // Фильтр по группе
            if (withoutGroup)
            {
                query = query.Where(u => u.StudentGroupId == null);
            }
            else if (groupId.HasValue)
            {
                query = query.Where(u => u.StudentGroupId == groupId);
            }

            // Поиск по имени или студенческому билету
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.ToLower();
                query = query.Where(u =>
                    u.FullName.ToLower().Contains(term) ||
                    (u.StudentId != null && u.StudentId.ToLower().Contains(term)));
            }

            // Получаем данные
            var students = await query
                .Select(s => new
                {
                    s.Id,
                    s.FullName,
                    s.Email,
                    s.StudentId,
                    s.PhoneNumber,
                    s.SocialStatus,
                    GroupInfo = s.Group != null ? new
                    {
                        s.Group.Id,
                        s.Group.Name,
                        CuratorInfo = s.Group.Curator != null ? new
                        {
                            s.Group.Curator.Id,
                            s.Group.Curator.FullName
                        } : null
                    } : null
                })
                .ToListAsync();

            // Получаем информацию о курсах отдельным запросом
            var studentIds = students.Select(s => s.Id).ToList();
            var courseEnrollments = await _context.CourseStudents
                .Where(cs => studentIds.Contains(cs.UserId))
                .Select(cs => new
                {
                    cs.UserId,
                    CourseInfo = new
                    {
                        cs.Course.Id,
                        cs.Course.Name,
                        SubjectName = cs.Course.Subject.Name
                    }
                })
                .ToListAsync();

            // Объединяем данные
            var result = students.Select(s => new
            {
                s.Id,
                s.FullName,
                s.Email,
                s.StudentId,
                s.PhoneNumber,
                s.SocialStatus,
                Group = s.GroupInfo,
                EnrolledCourses = courseEnrollments
                    .Where(ce => ce.UserId == s.Id)
                    .Select(ce => ce.CourseInfo)
                    .ToList()
            })
            .OrderBy(s => s.Group?.Name ?? "") // Сортировка: сначала по имени группы
            .ThenBy(s => s.FullName);          // затем по имени студента

            return Ok(new
            {
                TotalCount = students.Count,
                Students = result
            });
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

        [HttpGet("users/{userId}")]
        [Authorize]
        public async Task<ActionResult<UserProfileDto>> GetUserProfile(int userId)
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var currentUserRole = User.FindFirstValue(ClaimTypes.Role);
            
            // Получаем информацию о текущем пользователе, если он студент
            var currentStudent = currentUserRole == UserRole.Student ? 
                await _context.Users
                    .Include(u => u.Group)
                    .FirstOrDefaultAsync(u => u.Id == currentUserId) 
                : null;

            var user = await _context.Users
                .Include(u => u.Group)
                    .ThenInclude(g => g.Curator)
                .Include(u => u.Parents)
                .Include(u => u.Children)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return NotFound("Пользователь не найден");
            }

            // Базовая информация, доступная всем
            var profile = new UserProfileDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Role = user.Role,
            };

            // Администратор видит всю информацию
            if (currentUserRole == UserRole.Administrator)
            {
                profile.Email = user.Email;
                profile.StudentGroup = user.StudentGroup;
                profile.Address = user.Address;
                profile.PhoneNumber = user.PhoneNumber;
                profile.SocialStatus = user.SocialStatus;
                profile.StudentId = user.StudentId;
                profile.StudentGroupId = user.StudentGroupId;
            }
            // Преподаватель видит информацию о студентах из своих курсов или курируемых групп
            else if (currentUserRole == UserRole.Teacher)
            {
                bool isTeachingStudent = await _context.Courses
                    .AnyAsync(c => c.Teachers.Any(t => t.UserId == currentUserId) &&
                                  c.Students.Any(s => s.UserId == userId));

                bool isCuratingStudent = await _context.StudentGroups
                    .AnyAsync(g => g.CuratorId == currentUserId &&
                                  g.Students.Any(s => s.Id == userId));

                if (isTeachingStudent || isCuratingStudent)
                {
                    profile.Email = user.Email;
                    profile.StudentGroup = user.StudentGroup;
                    profile.PhoneNumber = user.PhoneNumber;
                    profile.SocialStatus = user.SocialStatus;
                    profile.StudentId = user.StudentId;
                    profile.StudentGroupId = user.StudentGroupId;
                }
            }
            // Родитель видит полную информацию только о своих детях
            else if (currentUserRole == UserRole.Parent)
            {
                var isParentOfStudent = await _context.Users
                    .AnyAsync(u => u.Id == currentUserId &&
                                  u.Children.Any(c => c.Id == userId));

                if (isParentOfStudent)
                {
                    profile.Email = user.Email;
                    profile.StudentGroup = user.StudentGroup;
                    profile.PhoneNumber = user.PhoneNumber;
                    profile.SocialStatus = user.SocialStatus;
                    profile.StudentId = user.StudentId;
                    profile.StudentGroupId = user.StudentGroupId;
                }
            }
            // Студент видит базовую информацию о других студентах из своей группы
            else if (currentUserRole == UserRole.Student && currentStudent != null)
            {
                if (currentStudent.StudentGroupId != null &&
                    currentStudent.StudentGroupId == user.StudentGroupId)
                {
                    profile.Email = user.Email;
                    profile.StudentGroup = user.StudentGroup;
                    profile.StudentGroupId = user.StudentGroupId;
                }
            }

            // Добавляем информацию о связях, если есть права на просмотр
            if (currentUserRole == UserRole.Administrator ||
                (currentUserRole == UserRole.Parent && user.Role == UserRole.Student))
            {
                profile.RelatedUsers = new
                {
                    Parents = user.Parents.Select(p => new { p.Id, p.FullName, p.Email }).ToList(),
                    Children = user.Children.Select(c => new { c.Id, c.FullName, c.Email }).ToList()
                };
            }

            // Добавляем информацию о группе
            if (user.Group != null && (currentUserRole == UserRole.Administrator ||
                                      currentUserRole == UserRole.Teacher ||
                                      (currentStudent?.StudentGroupId == user.StudentGroupId)))
            {
                profile.GroupInfo = new
                {
                    user.Group.Id,
                    user.Group.Name,
                    user.Group.Description,
                    CuratorId = user.Group.CuratorId,
                    CuratorName = user.Group.Curator?.FullName
                };
            }

            return Ok(profile);
        }

        [HttpGet("me/permissions")]
        public IActionResult GetMyPermissions()
        {
            var currentUserRole = User.FindFirstValue(ClaimTypes.Role);
            if (string.IsNullOrEmpty(currentUserRole))
                return Unauthorized();

            var permissions = new Dictionary<string, bool>();

            // Добавляем все политики и проверяем доступ к ним
            permissions.Add("ManageUsers", currentUserRole == UserRole.Administrator);
            permissions.Add("ManageSettings", currentUserRole == UserRole.Administrator);

            // Управление курсами
            permissions.Add("ManageCourses", currentUserRole == UserRole.Administrator ||
                                            currentUserRole == UserRole.Teacher);
            permissions.Add("ViewCourses", currentUserRole == UserRole.Administrator ||
                                          currentUserRole == UserRole.Teacher ||
                                          currentUserRole == UserRole.Student);

            // Управление расписанием
            permissions.Add("ManageSchedule", currentUserRole == UserRole.Administrator ||
                                            currentUserRole == UserRole.Teacher);
            permissions.Add("ViewSchedule", true); // Доступно всем авторизованным пользователям

            // Управление оценками
            permissions.Add("ManageGrades", currentUserRole == UserRole.Administrator ||
                                          currentUserRole == UserRole.Teacher);
            permissions.Add("ViewGrades", currentUserRole != UserRole.Administrator);

            // Управление сообщениями
            permissions.Add("SendMessages", true); // Доступно всем авторизованным пользователям
            permissions.Add("DeleteMessages", currentUserRole == UserRole.Administrator);
            permissions.Add("ManageGroupChats", currentUserRole == UserRole.Administrator ||
                                              currentUserRole == UserRole.Teacher);

            // Управление отчетами
            permissions.Add("ManageReports", currentUserRole == UserRole.Administrator ||
                                           currentUserRole == UserRole.Teacher);
            permissions.Add("ViewReports", true); // Доступно всем авторизованным пользователям

            // Управление заданиями
            permissions.Add("ManageAssignments", currentUserRole == UserRole.Administrator ||
                                               currentUserRole == UserRole.Teacher);
            permissions.Add("ViewAssignments", true); // Доступно всем авторизованным пользователям
            permissions.Add("SubmitAssignments", currentUserRole == UserRole.Student);

            // Управление студентами
            permissions.Add("ManageStudents", currentUserRole == UserRole.Administrator ||
                                            currentUserRole == UserRole.Teacher);
            permissions.Add("ViewStudentDetails", currentUserRole == UserRole.Administrator ||
                                                currentUserRole == UserRole.Teacher);

            var result = new
            {
                Role = currentUserRole,
                Permissions = permissions,
                Categories = new
                {
                    Users = new
                    {
                        CanManage = permissions["ManageUsers"],
                    },
                    Courses = new
                    {
                        CanManage = permissions["ManageCourses"],
                        CanView = permissions["ViewCourses"]
                    },
                    Schedule = new
                    {
                        CanManage = permissions["ManageSchedule"],
                        CanView = permissions["ViewSchedule"]
                    },
                    Grades = new
                    {
                        CanManage = permissions["ManageGrades"],
                        CanView = permissions["ViewGrades"]
                    },
                    Messages = new
                    {
                        CanSend = permissions["SendMessages"],
                        CanDelete = permissions["DeleteMessages"],
                        CanManageGroups = permissions["ManageGroupChats"]
                    },
                    Reports = new
                    {
                        CanManage = permissions["ManageReports"],
                        CanView = permissions["ViewReports"]
                    },
                    Assignments = new
                    {
                        CanManage = permissions["ManageAssignments"],
                        CanView = permissions["ViewAssignments"],
                        CanSubmit = permissions["SubmitAssignments"]
                    },
                    Students = new
                    {
                        CanManage = permissions["ManageStudents"],
                        CanViewDetails = permissions["ViewStudentDetails"]
                    }
                }
            };

            return Ok(result);
        }
    }
}