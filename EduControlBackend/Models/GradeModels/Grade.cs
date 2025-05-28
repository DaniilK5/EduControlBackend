using EduControlBackend.Models.AssignmentModels;
using EduControlBackend.Models.LoginAndReg;

namespace EduControlBackend.Models.GradeModels
{
    public class Grade
    {
        public int Id { get; set; }
        public string Value { get; set; } // Оценка (например, "5", "A", "95")
        public string? Comment { get; set; }
        public DateTime GradedAt { get; set; } = DateTime.UtcNow;

        // Связи
        public int AssignmentId { get; set; }
        public Assignment Assignment { get; set; }
        public int StudentId { get; set; }
        public User Student { get; set; }
        public int InstructorId { get; set; }
        public User Instructor { get; set; }
    }
}