namespace EduControlBackend.Models.CourseModels
{
    public class CreateCourseDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public List<int> TeacherIds { get; set; } = new();
        public List<int> StudentIds { get; set; } = new();
    }
}