using System.ComponentModel.DataAnnotations;

namespace EduControlBackend.Models.StudentModels
{
    public class CreateAbsenceDto
    {
        [Required(ErrorMessage = "ID �������� ����������")]
        public int StudentId { get; set; }

        [Required(ErrorMessage = "���� �������� �����������")]
        public DateTime Date { get; set; }

        [Required]
        [Range(2, 16, ErrorMessage = "���������� ����������� ����� ������ ���� �� 2 �� 16")]
        public int Hours { get; set; }

        public string? Reason { get; set; }

        public bool IsExcused { get; set; }

        public string? Comment { get; set; }
    }
}