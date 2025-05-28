namespace EduControlBackend.Models.AssignmentModels
{
    public class AssignmentFile
    {
        public int Id { get; set; }
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public string FileType { get; set; }
        public DateTime UploadedAt { get; set; }

        // Связи
        public int AssignmentId { get; set; }
        public Assignment Assignment { get; set; }
    }
}