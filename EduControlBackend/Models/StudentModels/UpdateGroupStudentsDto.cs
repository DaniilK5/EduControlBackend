using System.ComponentModel.DataAnnotations;

namespace EduControlBackend.Models.StudentModels
{
    public class UpdateGroupStudentsDto
    {
        [Required(ErrorMessage = "������ ID ��������� ����������")]
        public List<int> StudentIds { get; set; } = new();
    }
}