using EduControlBackend.Models.AssignmentModels;
using EduControlBackend.Models.LoginAndReg;
using System.ComponentModel.DataAnnotations;

namespace EduControlBackend.Models.GradeModels
{
    public class Grade
    {
        public int Id { get; set; }

        [Range(0, 100, ErrorMessage = "Оценка должна быть в диапазоне от 0 до 100 баллов")]
        public int Value { get; set; }
        
        public string? Comment { get; set; }
        public DateTime GradedAt { get; set; } = DateTime.UtcNow;

        // Связи
        public int AssignmentId { get; set; }
        public Assignment Assignment { get; set; }
        public int StudentId { get; set; }
        public User Student { get; set; }
        public int InstructorId { get; set; }
        public User Instructor { get; set; }
        
        // Новая связь один-к-одному
        public int SubmissionId { get; set; }
        public AssignmentSubmission Submission { get; set; }
    }
}