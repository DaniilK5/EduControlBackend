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
    public class StudentGroupController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public StudentGroupController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ��������� �����, ��� ������������ �������� ���������
        [HttpGet("curated")]
        [Authorize(Policy = UserRole.Policies.ManageStudents)]
        public async Task<IActionResult> GetCuratedGroups()
        {
            var curatorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var groups = await _context.StudentGroups
                .Include(g => g.Students)
                .Where(g => g.CuratorId == curatorId)
                .Select(g => new
                {
                    g.Id,
                    g.Name,
                    g.Description,
                    StudentsCount = g.Students.Count
                })
                .ToListAsync();

            return Ok(groups);
        }

        // ��������� ��������� ���������� � ������
        [HttpGet("{groupId}")]
        [Authorize(Policy = UserRole.Policies.ManageStudents)]
        public async Task<IActionResult> GetGroupDetails(int groupId)
        {
            var curatorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            var group = await _context.StudentGroups
                .Include(g => g.Students)
                .Include(g => g.Curator)
                .FirstOrDefaultAsync(g => g.Id == groupId);

            if (group == null)
                return NotFound("������ �� �������");

            // ��������� ����� �������
            if (userRole != UserRole.Administrator && group.CuratorId != curatorId)
                return Forbid("�� �� ��������� ��������� ���� ������");

            var students = await _context.Users
                .Where(u => u.StudentGroupId == groupId)
                .Select(s => new
                {
                    s.Id,
                    s.FullName,
                    s.Email,
                    s.PhoneNumber,
                    s.Address,
                    s.SocialStatus,
                    s.StudentId
                })
                .ToListAsync();

            return Ok(new
            {
                group.Id,
                group.Name,
                group.Description,
                Curator = new
                {
                    group.Curator.Id,
                    group.Curator.FullName,
                    group.Curator.Email
                },
                Students = students
            });
        }

        // ��������� ����� ������������ ������
        [HttpGet("{groupId}/performance")]
        [Authorize(Policy = UserRole.Policies.ManageStudents)]
        public async Task<IActionResult> GetGroupPerformance(int groupId)
        {
            var curatorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            var group = await _context.StudentGroups
                .FirstOrDefaultAsync(g => g.Id == groupId);

            if (group == null)
                return NotFound("������ �� �������");

            if (userRole != UserRole.Administrator && group.CuratorId != curatorId)
                return Forbid("�� �� ��������� ��������� ���� ������");

            var students = await _context.Users
                .Where(u => u.StudentGroupId == groupId)
                .Select(s => new
                {
                    s.Id,
                    s.FullName,
                    s.StudentId,
                    Grades = _context.Grades
                        .Where(g => g.StudentId == s.Id)
                        .Include(g => g.Assignment)
                            .ThenInclude(a => a.Course)
                                .ThenInclude(c => c.Subject)
                        .OrderByDescending(g => g.GradedAt)
                        .Select(g => new
                        {
                            g.Value,
                            g.Comment,
                            g.GradedAt,
                            Assignment = new
                            {
                                g.Assignment.Title,
                                Course = new
                                {
                                    g.Assignment.Course.Name,
                                    Subject = g.Assignment.Course.Subject.Name
                                }
                            }
                        })
                        .ToList(),
                    Performance = new
                    {
                        AverageGrade = _context.Grades
                            .Where(g => g.StudentId == s.Id)
                            .Average(g => (double?)g.Value) ?? 0,
                        LatestGrades = _context.Grades
                            .Where(g => g.StudentId == s.Id)
                            .OrderByDescending(g => g.GradedAt)
                            .Take(5)
                            .Select(g => new
                            {
                                g.Value,
                                g.GradedAt,
                                Course = g.Assignment.Course.Name
                            })
                            .ToList(),
                        SubjectsPerformance = _context.Grades
                            .Where(g => g.StudentId == s.Id)
                            .GroupBy(g => g.Assignment.Course.Subject.Name)
                            .Select(g => new
                            {
                                Subject = g.Key,
                                AverageGrade = g.Average(x => x.Value),
                                GradesCount = g.Count()
                            })
                            .ToList()
                    }
                })
                .ToListAsync();

            return Ok(new
            {
                group.Name,
                StudentsCount = students.Count,
                GroupAverage = students.Select(s => s.Performance.AverageGrade).Average(),
                Students = students.OrderByDescending(s => s.Performance.AverageGrade)
            });
        }


        [HttpPost("{groupId}/curator")]
        [Authorize(Policy = UserRole.Policies.ManageStudents)]
        public async Task<IActionResult> AssignCurator(int groupId, [FromBody] AssignCuratorDto dto)
        {
            // ��������� ����� ������� - ������ ������������� ����� ��������� ���������
            if (User.FindFirstValue(ClaimTypes.Role) != UserRole.Administrator)
                return Forbid("������ ������������� ����� ��������� ��������� �����");

            var group = await _context.StudentGroups
                .Include(g => g.Curator)
                .FirstOrDefaultAsync(g => g.Id == groupId);

            if (group == null)
                return NotFound("������ �� �������");

            // ��������� ������������� �������������
            var teacher = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == dto.TeacherId &&
                    (u.Role == UserRole.Teacher || u.Role == UserRole.Administrator));

            if (teacher == null)
                return BadRequest("��������� ������������ �� ������ ��� �� �������� ��������������");

            // ���������, �� �������� �� ������������� ��� ��������� ������ ������
            if (await _context.StudentGroups.AnyAsync(g => g.CuratorId == dto.TeacherId && g.Id != groupId))
                return BadRequest("������������� ��� �������� ��������� ������ ������");

            group.CuratorId = dto.TeacherId;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                groupId = group.Id,
                groupName = group.Name,
                curator = new
                {
                    teacher.Id,
                    teacher.FullName,
                    teacher.Email
                }
            });
        }


        [HttpDelete("{groupId}/curator")]
        [Authorize(Policy = UserRole.Policies.ManageStudents)]
        public async Task<IActionResult> RemoveCurator(int groupId)
        {
            // ��������� ����� �������
            if (User.FindFirstValue(ClaimTypes.Role) != UserRole.Administrator)
                return Forbid("������ ������������� ����� ������� ��������� �����");

            var group = await _context.StudentGroups
                .Include(g => g.Curator)
                .FirstOrDefaultAsync(g => g.Id == groupId);

            if (group == null)
                return NotFound("������ �� �������");

            if (group.CuratorId == null)
                return BadRequest("� ������ ��� ��������");

            // ��������� ���������� � ������� �������� ��� ������
            var currentCurator = group.Curator;

            // ������� ��������
            group.CuratorId = null;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "������� ������� ������",
                groupId = group.Id,
                groupName = group.Name,
                removedCurator = new
                {
                    currentCurator.Id,
                    currentCurator.FullName,
                    currentCurator.Email
                }
            });
        }


        [HttpPut("{groupId}/curator/{newCuratorId}")]
        [Authorize(Policy = UserRole.Policies.ManageStudents)]
        public async Task<IActionResult> ChangeCurator(int groupId, int newCuratorId)
        {
            // ��������� ����� �������
            if (User.FindFirstValue(ClaimTypes.Role) != UserRole.Administrator)
                return Forbid("������ ������������� ����� ������ ��������� �����");

            var group = await _context.StudentGroups
                .Include(g => g.Curator)
                .FirstOrDefaultAsync(g => g.Id == groupId);

            if (group == null)
                return NotFound("������ �� �������");

            // ��������� ������������� ������ ��������
            var newCurator = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == newCuratorId &&
                    (u.Role == UserRole.Teacher || u.Role == UserRole.Administrator));

            if (newCurator == null)
                return BadRequest("����� ������� �� ������ ��� �� �������� ��������������");

            // ���������, �� �������� �� ������������� ��� ��������� ������ ������
            if (await _context.StudentGroups.AnyAsync(g => g.CuratorId == newCuratorId && g.Id != groupId))
                return BadRequest("������������� ��� �������� ��������� ������ ������");

            var oldCurator = group.Curator;
            group.CuratorId = newCuratorId;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                groupId = group.Id,
                groupName = group.Name,
                oldCurator = oldCurator != null ? new
                {
                    oldCurator.Id,
                    oldCurator.FullName,
                    oldCurator.Email
                } : null,
                newCurator = new
                {
                    newCurator.Id,
                    newCurator.FullName,
                    newCurator.Email
                }
            });
        }

        // ���������� ��������� � ������
        [HttpPost("{groupId}/students")]
        [Authorize(Policy = UserRole.Policies.ManageStudents)]
        public async Task<IActionResult> AddStudentsToGroup(int groupId, [FromBody] List<int> studentIds)
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            var group = await _context.StudentGroups
                .Include(g => g.Students)
                .FirstOrDefaultAsync(g => g.Id == groupId);

            if (group == null)
                return NotFound("������ �� �������");

            // ��������� ����� �������
            if (userRole != UserRole.Administrator && group.CuratorId != currentUserId)
                return Forbid("�� �� ��������� ��������� ���� ������");

            // �������� ��������� ��� ����������
            var studentsToAdd = await _context.Users
                .Where(u => studentIds.Contains(u.Id) && u.Role == UserRole.Student)
                .ToListAsync();

            if (studentsToAdd.Count != studentIds.Count)
                return BadRequest("��������� ������������ �� ������� ��� �� �������� ����������");

            // ���������, �� ������� �� �������� ��� � ������ �������
            var studentsInGroups = studentsToAdd.Where(s => s.StudentGroupId != null && s.StudentGroupId != groupId).ToList();
            if (studentsInGroups.Any())
            {
                return BadRequest($"��������� �������� ��� ������� � ������ �������: {string.Join(", ", studentsInGroups.Select(s => s.FullName))}");
            }

            foreach (var student in studentsToAdd)
            {
                student.StudentGroupId = groupId;
                student.Group = group;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                AddedStudents = studentsToAdd.Select(s => new
                {
                    s.Id,
                    s.FullName,
                    s.Email,
                    s.StudentId
                })
            });
        }

        // �������� ��������� �� ������
        [HttpDelete("{groupId}/students")]
        [Authorize(Policy = UserRole.Policies.ManageStudents)]
        public async Task<IActionResult> RemoveStudentsFromGroup(int groupId, [FromBody] List<int> studentIds)
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            var group = await _context.StudentGroups
                .Include(g => g.Students)
                .FirstOrDefaultAsync(g => g.Id == groupId);

            if (group == null)
                return NotFound("������ �� �������");

            // ��������� ����� �������
            if (userRole != UserRole.Administrator && group.CuratorId != currentUserId)
                return Forbid("�� �� ��������� ��������� ���� ������");

            // �������� ��������� ��� ��������
            var studentsToRemove = await _context.Users
                .Where(u => studentIds.Contains(u.Id) && u.StudentGroupId == groupId)
                .ToListAsync();

            if (!studentsToRemove.Any())
                return BadRequest("��������� �������� �� ������� � ���� ������");

            foreach (var student in studentsToRemove)
            {
                student.StudentGroupId = null;
                student.Group = null;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                RemovedStudents = studentsToRemove.Select(s => new
                {
                    s.Id,
                    s.FullName,
                    s.Email,
                    s.StudentId
                })
            });
        }

        // ���������� ������ ��������� ������
        [HttpPut("{groupId}/students")]
        [Authorize(Policy = UserRole.Policies.ManageStudents)]
        public async Task<IActionResult> UpdateGroupStudents(int groupId, [FromBody] UpdateGroupStudentsDto dto)
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            var group = await _context.StudentGroups
                .Include(g => g.Students)
                .FirstOrDefaultAsync(g => g.Id == groupId);

            if (group == null)
                return NotFound("������ �� �������");

            // ��������� ����� �������
            if (userRole != UserRole.Administrator && group.CuratorId != currentUserId)
                return Forbid("�� �� ��������� ��������� ���� ������");

            // �������� ���� ���������, ������� ������ ���� � ������
            var newStudents = await _context.Users
                .Where(u => dto.StudentIds.Contains(u.Id) && u.Role == UserRole.Student)
                .ToListAsync();

            if (newStudents.Count != dto.StudentIds.Count)
                return BadRequest("��������� ������������ �� ������� ��� �� �������� ����������");

            // ���������, �� ������� �� ����� �������� ��� � ������ �������
            var studentsInGroups = newStudents.Where(s => s.StudentGroupId != null && s.StudentGroupId != groupId).ToList();
            if (studentsInGroups.Any())
            {
                return BadRequest($"��������� �������� ��� ������� � ������ �������: {string.Join(", ", studentsInGroups.Select(s => s.FullName))}");
            }

            // �������� ������� ��������� ������
            var currentStudents = await _context.Users
                .Where(u => u.StudentGroupId == groupId)
                .ToListAsync();

            // ������� ���������, ������� ��� � ����� ������
            var studentsToRemove = currentStudents.Where(s => !dto.StudentIds.Contains(s.Id));
            foreach (var student in studentsToRemove)
            {
                student.StudentGroupId = null;
                student.Group = null;
            }

            // ��������� ����� ���������
            foreach (var student in newStudents)
            {
                student.StudentGroupId = groupId;
                student.Group = group;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                UpdatedStudentList = newStudents.Select(s => new
                {
                    s.Id,
                    s.FullName,
                    s.Email,
                    s.StudentId
                })
            });
        }


        // �������� ����� ������
        [HttpPost]
        [Authorize(Policy = UserRole.Policies.ManageStudents)]
        public async Task<IActionResult> CreateGroup([FromBody] CreateStudentGroupDto dto)
        {
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            // ��������� ������������ �������� ������
            if (await _context.StudentGroups.AnyAsync(g => g.Name.ToLower() == dto.Name.ToLower()))
                return BadRequest("������ � ����� ��������� ��� ����������");

            var group = new StudentGroup
            {
                Name = dto.Name.Trim(),
                Description = dto.Description,
                CuratorId = dto.CuratorId
            };

            // ��������� ������������� ��������, ���� �� ������
            if (dto.CuratorId.HasValue)
            {
                var curator = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == dto.CuratorId &&
                        (u.Role == UserRole.Teacher || u.Role == UserRole.Administrator));

                if (curator == null)
                    return BadRequest("��������� ������� �� ������ ��� �� �������� ��������������");

                // ���������, �� �������� �� ������������� ��� ��������� ������ ������
                if (await _context.StudentGroups.AnyAsync(g => g.CuratorId == dto.CuratorId))
                    return BadRequest("������������� ��� �������� ��������� ������ ������");
            }

            _context.StudentGroups.Add(group);
            await _context.SaveChangesAsync();

            return Ok(new { groupId = group.Id });
        }

        // ��������� ������ ���� ����� (��� ��������������)
        [HttpGet]
        [Authorize(Policy = UserRole.Policies.ManageStudents)]
        public async Task<IActionResult> GetAllGroups()
        {
            var groups = await _context.StudentGroups
                .Include(g => g.Curator)
                .Include(g => g.Students)
                .Select(g => new
                {
                    g.Id,
                    g.Name,
                    g.Description,
                    Curator = g.Curator != null ? new
                    {
                        g.Curator.Id,
                        g.Curator.FullName,
                        g.Curator.Email
                    } : null,
                    StudentsCount = g.Students.Count,
                    Students = g.Students.Select(s => new
                    {
                        s.Id,
                        s.FullName,
                        s.Email,
                        s.StudentId
                    })
                })
                .ToListAsync();

            return Ok(groups);
        }

        // ��������� ���������� � ���������� ������
        [HttpGet("{groupId}/details")]
        [Authorize(Policy = UserRole.Policies.ViewStudentDetails)]
        public async Task<IActionResult> GetGroupDetailsWithStudents(int groupId)
        {
            var userRole = User.FindFirstValue(ClaimTypes.Role);
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var group = await _context.StudentGroups
                .Include(g => g.Curator)
                .Include(g => g.Students)
                .FirstOrDefaultAsync(g => g.Id == groupId);

            if (group == null)
                return NotFound("������ �� �������");

            // ��������� ����� �������
            if (userRole != UserRole.Administrator && group.CuratorId != currentUserId)
                return Forbid("� ��� ��� ���� ��� ��������� ���� ������");

            return Ok(new
            {
                group.Id,
                group.Name,
                group.Description,
                Curator = group.Curator != null ? new
                {
                    group.Curator.Id,
                    group.Curator.FullName,
                    group.Curator.Email
                } : null,
                Students = group.Students.Select(s => new
                {
                    s.Id,
                    s.FullName,
                    s.Email,
                    s.PhoneNumber,
                    s.Address,
                    s.SocialStatus,
                    s.StudentId
                })
            });
        }

        // ���������� ���������� � ������
        [HttpPut("{groupId}")]
        [Authorize(Policy = UserRole.Policies.ManageStudents)]
        public async Task<IActionResult> UpdateGroup(int groupId, [FromBody] UpdateStudentGroupDto dto)
        {
            var userRole = User.FindFirstValue(ClaimTypes.Role);
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var group = await _context.StudentGroups
                .Include(g => g.Curator)
                .FirstOrDefaultAsync(g => g.Id == groupId);

            if (group == null)
                return NotFound("������ �� �������");

            // ��������� ����� �������
            if (userRole != UserRole.Administrator && group.CuratorId != currentUserId)
                return Forbid("� ��� ��� ���� ��� ��������� ���� ������");

            // ��������� ������������ �������� ������
            if (dto.Name != group.Name && await _context.StudentGroups.AnyAsync(g => g.Name == dto.Name))
                return BadRequest("������ � ����� ��������� ��� ����������");

            // ��������� ��������
            if (dto.CuratorId.HasValue)
            {
                var curator = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == dto.CuratorId &&
                        (u.Role == UserRole.Teacher || u.Role == UserRole.Administrator));

                if (curator == null)
                    return BadRequest("��������� ������� �� ������ ��� �� �������� ��������������");

                // ���������, �� �������� �� ������������� ��� ��������� ������ ������
                if (group.CuratorId != dto.CuratorId &&
                    await _context.StudentGroups.AnyAsync(g => g.CuratorId == dto.CuratorId))
                    return BadRequest("������������� ��� �������� ��������� ������ ������");
            }

            group.Name = dto.Name;
            group.Description = dto.Description;
            group.CuratorId = dto.CuratorId;

            await _context.SaveChangesAsync();

            return Ok();
        }

        // �������� ������
        [HttpDelete("{groupId}")]
        [Authorize(Policy = UserRole.Policies.ManageStudents)]
        public async Task<IActionResult> DeleteGroup(int groupId)
        {
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            // ������ ������������� ����� ������� ������
            if (userRole != UserRole.Administrator)
                return Forbid("������ ������������� ����� ������� ������");

            var group = await _context.StudentGroups
                .Include(g => g.Students)
                .FirstOrDefaultAsync(g => g.Id == groupId);

            if (group == null)
                return NotFound("������ �� �������");

            // ������� ����� ��������� � �������
            foreach (var student in group.Students)
            {
                student.StudentGroupId = null;
                student.Group = null;
            }

            _context.StudentGroups.Remove(group);
            await _context.SaveChangesAsync();

            return Ok();
        }


        // ��������� ������ ��������
        [HttpPut("student/{studentId}/group")]
        [Authorize(Policy = UserRole.Policies.ManageStudents)]
        public async Task<IActionResult> ChangeStudentGroup(int studentId, [FromBody] ChangeStudentGroupDto dto)
        {
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            // ��������� ����� ������� - ������ ������������� ����� ������ ������
            if (userRole != UserRole.Administrator)
                return Forbid("������ ������������� ����� �������� ������ ��������");

            // ������� ��������
            var student = await _context.Users
                .Include(u => u.Group)
                .FirstOrDefaultAsync(u => u.Id == studentId && u.Role == UserRole.Student);

            if (student == null)
                return NotFound("������� �� ������");

            // ���� ������ ����� ID ������
            if (dto.NewGroupId.HasValue)
            {
                // ��������� ������������� ����� ������
                var newGroup = await _context.StudentGroups
                    .Include(g => g.Students)
                    .FirstOrDefaultAsync(g => g.Id == dto.NewGroupId);

                if (newGroup == null)
                    return NotFound("����� ������ �� �������");

                // ��������� ����� � �������
                student.StudentGroupId = dto.NewGroupId;
                student.Group = newGroup;
            }
            else
            {
                // ���� ID ������ �� ������, ������� �������� �� ������� ������
                student.StudentGroupId = null;
                student.Group = null;
            }

            // ��������� ��������� ���� � ��������� ������
            student.StudentGroup = dto.GroupName;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                StudentId = student.Id,
                StudentName = student.FullName,
                OldGroup = student.Group?.Name,
                NewGroup = dto.GroupName,
                Message = dto.NewGroupId.HasValue
                    ? "������� ������� �������� � ����� ������"
                    : "������� ����� �� ������"
            });
        }
    }
}