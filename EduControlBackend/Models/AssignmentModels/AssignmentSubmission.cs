using EduControlBackend.Models.GradeModels;
using EduControlBackend.Models.LoginAndReg;

namespace EduControlBackend.Models.AssignmentModels
{
    public class AssignmentSubmission
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
        
        // �����, ������������� � ������ ��������
        public string? AttachmentPath { get; set; }
        public string? AttachmentName { get; set; }
        public string? AttachmentType { get; set; }
        
        // �����
        public int AssignmentId { get; set; }
        public Assignment Assignment { get; set; }
        public int StudentId { get; set; }
        public User Student { get; set; }
        
        // ����� ����-�-������ � Grade
        public Grade? Grade { get; set; }
    }
}