namespace EduControlBackend.Models.AssignmentModels
{
    public class SubmitAssignmentDto
    {
        public string Content { get; set; }
        public IFormFile? Attachment { get; set; }
    }
}