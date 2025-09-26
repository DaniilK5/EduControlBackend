namespace EduControlBackend.Models.AssignmentModels
{
    public class CreateAssignmentDto
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime Deadline { get; set; }
        public int CourseId { get; set; }
        public IFormFile? Attachment { get; set; }
    }
}
