using EduControlBackend.Models.CourseModels;
using EduControlBackend.Models.LoginAndReg;

namespace EduControlBackend.Models.AssignmentModels
{
    public class Assignment
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime Deadline { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Файлы, прикрепленные к заданию преподавателем
        public string? AttachmentPath { get; set; }
        public string? AttachmentName { get; set; }
        public string? AttachmentType { get; set; }

        // Связи
        public int CourseId { get; set; }
        public Course Course { get; set; }
        public int InstructorId { get; set; }
        public User Instructor { get; set; }
        public ICollection<AssignmentSubmission> Submissions { get; set; } = new List<AssignmentSubmission>();
    }
}
