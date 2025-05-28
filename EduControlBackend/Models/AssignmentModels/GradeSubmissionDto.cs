using System.ComponentModel.DataAnnotations;

namespace EduControlBackend.Models.AssignmentModels
{
    public class GradeSubmissionDto
    {
        [Required(ErrorMessage = "ќценка об€зательна")]
        [Range(0, 100, ErrorMessage = "ќценка должна быть в диапазоне от 0 до 100 баллов")]
        public int Value { get; set; }

        public string? Comment { get; set; }
    }
}