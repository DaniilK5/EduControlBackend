using System.ComponentModel.DataAnnotations;

namespace EduControlBackend.Models.StudentModels
{
    public class CreateAbsenceDto
    {
        [Required(ErrorMessage = "ID студента об€зателен")]
        public int StudentId { get; set; }

        [Required(ErrorMessage = "ƒата пропуска об€зательна")]
        public DateTime Date { get; set; }

        [Required]
        [Range(2, 16, ErrorMessage = " оличество пропущенных часов должно быть от 2 до 16")]
        public int Hours { get; set; }

        public string? Reason { get; set; }

        public bool IsExcused { get; set; }

        public string? Comment { get; set; }
    }
}