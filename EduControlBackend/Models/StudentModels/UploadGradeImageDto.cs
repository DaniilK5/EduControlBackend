using System.ComponentModel.DataAnnotations;

namespace EduControlBackend.Models.StudentModels
{
    public class UploadGradeImageDto
    {
        [Required]
        public IFormFile File { get; set; }

        [Required]
        public ImageType Type { get; set; }

        public int? SubjectId { get; set; }

        public int? StudentGroupId { get; set; }
    }
}