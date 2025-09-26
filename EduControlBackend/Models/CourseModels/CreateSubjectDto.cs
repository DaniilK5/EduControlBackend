using System.ComponentModel.DataAnnotations;

namespace EduControlBackend.Models.CourseModels
{
    public class CreateSubjectDto
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public string Code { get; set; }

        public string? Description { get; set; }


    }
}