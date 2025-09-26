using System.ComponentModel.DataAnnotations;

namespace EduControlBackend.Models.AssignmentModels
{
    public class GradeSubmissionDto
    {
        [Required(ErrorMessage = "������ �����������")]
        [Range(0, 100, ErrorMessage = "������ ������ ���� � ��������� �� 0 �� 100 ������")]
        public int Value { get; set; }

        public string? Comment { get; set; }
    }
}