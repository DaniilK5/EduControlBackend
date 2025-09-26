using System.ComponentModel.DataAnnotations;

namespace EduControlBackend.Models.CourseModels
{
    public class Subject
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string Code { get; set; } // ���������� ��� ��������

        public string? Description { get; set; }

        // �����
        public ICollection<Course> Courses { get; set; } = new List<Course>();
    }
}