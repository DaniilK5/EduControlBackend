using EduControlBackend.Models;
using EduControlBackend.Models.StudentModels;
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
    public class GradeImagesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly FileService _fileService;

        public GradeImagesController(ApplicationDbContext context, FileService fileService)
        {
            _context = context;
            _fileService = fileService;
        }

        // �������� �����������
        [HttpPost("upload")]
        [Authorize(Policy = UserRole.Policies.ManageGrades)]
        public async Task<IActionResult> UploadImage([FromForm] UploadGradeImageDto dto)
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

                // ��������� ������������� ������, ���� �������
                if (dto.StudentGroupId.HasValue)
                {
                    var group = await _context.StudentGroups.FindAsync(dto.StudentGroupId);
                    if (group == null)
                        return NotFound("������ �� �������");
                }

                // ��������� ������������� ��������, ���� ������
                if (dto.SubjectId.HasValue)
                {
                    var subject = await _context.Subjects.FindAsync(dto.SubjectId);
                    if (subject == null)
                        return NotFound("������� �� ������");
                }

                // ��������� ����
                var (path, fileName) = dto.Type == ImageType.Grades ? 
                    await _fileService.SaveGradeImageAsync(dto.File) : 
                    await _fileService.SaveScheduleImageAsync(dto.File);

                var image = new GradeImage
                {
                    FilePath = path, // ���������� path ������ filePath
                    FileName = fileName,
                    FileType = Path.GetExtension(dto.File.FileName).ToLowerInvariant(),
                    Type = dto.Type,
                    SubjectId = dto.SubjectId,
                    StudentGroupId = dto.StudentGroupId,
                    UploaderId = currentUserId,
                    UploadedAt = DateTime.UtcNow
                };

                _context.GradeImages.Add(image);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    image.Id,
                    image.FileName,
                    image.FileType,
                    image.Type,
                    image.UploadedAt
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception)
            {
                return StatusCode(500, "��������� ������ ��� �������� �����");
            }
        }

        // ��������� ������ �����������
        [HttpGet]
        [AllowAnonymous] // ������ ��������� ��� ����
        public async Task<IActionResult> GetImages(
            [FromQuery] ImageType? type = null,
            [FromQuery] int? subjectId = null,
            [FromQuery] int? groupId = null)
        {
            var query = _context.GradeImages
                .Include(g => g.Subject)
                .Include(g => g.StudentGroup)
                .Include(g => g.Uploader)
                .AsQueryable();

            // ���������� �� ����
            if (type.HasValue)
                query = query.Where(g => g.Type == type);

            // ���������� �� ��������
            if (subjectId.HasValue)
                query = query.Where(g => g.SubjectId == subjectId);

            // ���������� �� ������
            if (groupId.HasValue)
                query = query.Where(g => g.StudentGroupId == groupId);

            var images = await query
                .OrderByDescending(g => g.UploadedAt)
                .Select(g => new
                {
                    g.Id,
                    g.FileName,
                    g.FileType,
                    g.UploadedAt,
                    g.Type,
                    Subject = g.Subject != null ? new { g.Subject.Id, g.Subject.Name } : null,
                    Group = g.StudentGroup != null ? new { g.StudentGroup.Id, g.StudentGroup.Name } : null,
                    Uploader = new { g.Uploader.Id, g.Uploader.FullName }
                })
                .ToListAsync();

            return Ok(new
            {
                TotalCount = images.Count,
                FilterInfo = new
                {
                    Type = type,
                    SubjectId = subjectId,
                    GroupId = groupId
                },
                Images = images
            });
        }

        // ���������� �����������
        [HttpGet("{id}/download")]
        [AllowAnonymous] // ������ ��������� ��� ����
        public async Task<IActionResult> DownloadImage(int id)
        {
            var image = await _context.GradeImages
                .FirstOrDefaultAsync(g => g.Id == id);

            if (image == null)
                return NotFound("����������� �� �������");

            var fileStream = image.Type == ImageType.Grades ? 
                _fileService.GetGradeImageStream(image.FilePath) : 
                _fileService.GetScheduleImageStream(image.FilePath);

            if (fileStream == null)
                return NotFound("���� �� ������");

            // ���������� MIME-��� �� ������ ���������� �����
            var mimeType = image.FileType.ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                _ => "application/octet-stream"
            };

            return File(fileStream, mimeType, image.FileName);
        }

        // �������� �����������
        [HttpDelete("{id}")]
        [Authorize(Policy = UserRole.Policies.ManageGrades)]
        public async Task<IActionResult> DeleteImage(int id)
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            var image = await _context.GradeImages.FindAsync(id);
            if (image == null)
                return NotFound("����������� �� �������");

            // �������� ���� �� ��������
            if (userRole != UserRole.Administrator && image.UploaderId != currentUserId)
                return Forbid();

            _fileService.DeleteFile(image.FilePath);
            _context.GradeImages.Remove(image);
            await _context.SaveChangesAsync();

            return Ok();
        }


        // �������� ���������� (������ ��� ���������������)
        [HttpPost("schedule/upload")]
        [Authorize(Roles = UserRole.Administrator)]
        public async Task<IActionResult> UploadSchedule([FromForm] UploadGradeImageDto dto)
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

                // ��������� ������������� ������, ���� �������
                if (dto.StudentGroupId.HasValue)
                {
                    var group = await _context.StudentGroups.FindAsync(dto.StudentGroupId);
                    if (group == null)
                        return NotFound("������ �� �������");
                }

                // ������������� ��� ����������� ��� ����������
                dto.Type = ImageType.Schedule;

                // ��������� ����
                var (path, fileName) = await _fileService.SaveScheduleImageAsync(dto.File);

                var image = new GradeImage
                {
                    FilePath = path,
                    FileName = fileName,
                    FileType = Path.GetExtension(dto.File.FileName).ToLowerInvariant(),
                    Type = ImageType.Schedule,
                    StudentGroupId = dto.StudentGroupId,
                    UploaderId = currentUserId,
                    UploadedAt = DateTime.UtcNow
                };

                _context.GradeImages.Add(image);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    image.Id,
                    image.FileName,
                    image.FileType,
                    image.Type,
                    image.UploadedAt,
                    Group = dto.StudentGroupId.HasValue ? new { Id = dto.StudentGroupId.Value } : null
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception)
            {
                return StatusCode(500, "��������� ������ ��� �������� ����������");
            }
        }

        // ��������� ������ ���������� (�������� ����)
        [HttpGet("schedule")]
        [AllowAnonymous]
        public async Task<IActionResult> GetSchedules(
            [FromQuery] int? groupId = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            var query = _context.GradeImages
                .Include(g => g.StudentGroup)
                .Include(g => g.Uploader)
                .Where(g => g.Type == ImageType.Schedule);

            // ���������� �� ������
            if (groupId.HasValue)
                query = query.Where(g => g.StudentGroupId == groupId);

            // ���������� �� ���� ��������
            if (startDate.HasValue)
                query = query.Where(g => g.UploadedAt.Date >= startDate.Value.Date);

            if (endDate.HasValue)
                query = query.Where(g => g.UploadedAt.Date <= endDate.Value.Date);

            var schedules = await query
                .OrderByDescending(g => g.UploadedAt)
                .Select(g => new
                {
                    g.Id,
                    g.FileName,
                    g.FileType,
                    g.UploadedAt,
                    Group = g.StudentGroup != null ? new 
                    { 
                        g.StudentGroup.Id, 
                        g.StudentGroup.Name 
                    } : null,
                    Uploader = new 
                    { 
                        g.Uploader.Id, 
                        g.Uploader.FullName 
                    },
                    // ��������� ���������� � ����� ��� �������
                    UploadDate = g.UploadedAt.Date.ToString("yyyy-MM-dd"),
                    UploadTime = g.UploadedAt.ToString("HH:mm:ss")
                })
                .ToListAsync();

            // ��������� ���������� � ����������
            return Ok(new
            {
                TotalCount = schedules.Count,
                FilterInfo = new
                {
                    GroupId = groupId,
                    StartDate = startDate?.ToString("yyyy-MM-dd"),
                    EndDate = endDate?.ToString("yyyy-MM-dd")
                },
                DateRange = new
                {
                    Earliest = schedules.Any() ? schedules.Min(s => s.UploadedAt).ToString("yyyy-MM-dd") : null,
                    Latest = schedules.Any() ? schedules.Max(s => s.UploadedAt).ToString("yyyy-MM-dd") : null
                },
                Schedules = schedules
            });
        }

        // ���������� ���������� (�������� ����)
        [HttpGet("schedule/{id}/download")]
        [AllowAnonymous]
        public async Task<IActionResult> DownloadSchedule(int id)
        {
            var image = await _context.GradeImages
                .FirstOrDefaultAsync(g => g.Id == id && g.Type == ImageType.Schedule);

            if (image == null)
                return NotFound("���������� �� �������");

            var fileStream = _fileService.GetScheduleImageStream(image.FilePath);
            if (fileStream == null)
                return NotFound("���� �� ������");

            // ���������� MIME-��� �� ������ ���������� �����
            var mimeType = image.FileType.ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                _ => "application/octet-stream"
            };

            return File(fileStream, mimeType, image.FileName);
        }

        // �������� ���������� (������ ��� ���������������)
        [HttpDelete("schedule/{id}")]
        [Authorize(Roles = UserRole.Administrator)]
        public async Task<IActionResult> DeleteSchedule(int id)
        {
            var image = await _context.GradeImages
                .FirstOrDefaultAsync(g => g.Id == id && g.Type == ImageType.Schedule);

            if (image == null)
                return NotFound("���������� �� �������");

            _fileService.DeleteFile(image.FilePath);
            _context.GradeImages.Remove(image);
            await _context.SaveChangesAsync();

            return Ok();
        }
    }
}