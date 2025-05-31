using System.ComponentModel.DataAnnotations;

namespace EduControlBackend.Models.StudentModels
{
    public class CreateStudentGroupDto
    {
        [Required(ErrorMessage = "�������� ������ �����������")]
        public string Name { get; set; }

        public string? Description { get; set; }

        public int? CuratorId { get; set; }
    }
}