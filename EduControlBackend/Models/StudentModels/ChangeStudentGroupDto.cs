using System.ComponentModel.DataAnnotations;

namespace EduControlBackend.Models.StudentModels
{
    public class ChangeStudentGroupDto
    {
        public int? NewGroupId { get; set; }

        [Required(ErrorMessage = "�������� ������ �����������")]
        public string GroupName { get; set; }
    }
}