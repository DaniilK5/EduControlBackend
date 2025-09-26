using System.ComponentModel.DataAnnotations;

namespace EduControlBackend.Models.CourseModels
{
    public class CreateCourseDto
    {
        [Required(ErrorMessage = "�������� ����� �����������")]
        public string Name { get; set; }

        [Required(ErrorMessage = "�������� ����� �����������")]
        public string Description { get; set; }

        [Required(ErrorMessage = "������� ����������")]
        public int SubjectId { get; set; }

        public List<int> TeacherIds { get; set; } = new List<int>();
        public List<int> StudentIds { get; set; } = new List<int>();
    }
}