using EduControlBackend.Models.CourseModels;
using EduControlBackend.Models.LoginAndReg;

namespace EduControlBackend.Models.StudentModels
{
    public class GradeImage
    {
        public int Id { get; set; }
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public string FileType { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        public ImageType Type { get; set; }

        // Связи
        public int? SubjectId { get; set; }
        public Subject? Subject { get; set; }

        public int? StudentGroupId { get; set; }
        public StudentGroup? StudentGroup { get; set; }

        public int UploaderId { get; set; }
        public User Uploader { get; set; }
    }

    public enum ImageType
    {
        Schedule,
        Grades
    }
}