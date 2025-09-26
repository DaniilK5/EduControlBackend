using EduControlBackend.Models.LoginAndReg;

namespace EduControlBackend.Models.CourseModels
{
    public class CourseTeacher
    {
        public int CourseId { get; set; }
        public Course Course { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }
}