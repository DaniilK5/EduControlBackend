using EduControlBackend.Models.AssignmentModels;
using EduControlBackend.Models.LoginAndReg;

namespace EduControlBackend.Models.CourseModels
{
    public class Course
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; } = true;

        // Связи
        public ICollection<CourseTeacher> Teachers { get; set; } = new List<CourseTeacher>();
        public ICollection<CourseStudent> Students { get; set; } = new List<CourseStudent>();
        public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
    }
}
