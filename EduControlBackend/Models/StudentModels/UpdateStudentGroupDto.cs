using System.ComponentModel.DataAnnotations;

namespace EduControlBackend.Models.StudentModels
{
    public class UpdateStudentGroupDto
    {
        [Required(ErrorMessage = "Название группы обязательно")]
        public string Name { get; set; }

        public string? Description { get; set; }

        public int? CuratorId { get; set; }
    }
}