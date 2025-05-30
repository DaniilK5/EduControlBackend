using System.ComponentModel.DataAnnotations;

namespace EduControlBackend.Models.CourseModels
{
    public class CreateCourseDto
    {
        [Required(ErrorMessage = "Название курса обязательно")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Описание курса обязательно")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Предмет обязателен")]
        public int SubjectId { get; set; }

        public List<int> TeacherIds { get; set; } = new List<int>();
        public List<int> StudentIds { get; set; } = new List<int>();
    }
}