using EduControlBackend.Models.LoginAndReg;

namespace EduControlBackend.Models.CourseModels
{
    public class CourseStudent
    {
        public int CourseId { get; set; }
        public Course Course { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
        public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
    }
}