namespace EduControlBackend.Models.CourseModels
{
    public class UpdateCourseDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
        public int SubjectId { get; set; }
        public List<int> TeacherIds { get; set; } = new();
        public List<int> StudentIds { get; set; } = new();
    }
}