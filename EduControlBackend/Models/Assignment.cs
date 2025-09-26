namespace EduControlBackend.Models
{
    public class Assignment
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime Deadline { get; set; }
        public int CourseId { get; set; }
        public Course Course { get; set; }
        public int InstructorId { get; set; }
        public User Instructor { get; set; }
    }
}
