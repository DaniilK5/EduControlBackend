using System.ComponentModel.DataAnnotations;

namespace EduControlBackend.Models.StudentModels
{
    public class AssignCuratorDto
    {
        [Required(ErrorMessage = "ID ������������� ����������")]
        public int TeacherId { get; set; }
    }
}