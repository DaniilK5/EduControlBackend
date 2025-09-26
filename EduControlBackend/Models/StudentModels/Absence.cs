using System.ComponentModel.DataAnnotations;
using EduControlBackend.Models.LoginAndReg;

namespace EduControlBackend.Models.StudentModels
{
    public class Absence
    {
        public int Id { get; set; }

        // Связь со студентом
        public int StudentId { get; set; }
        public User Student { get; set; }

        // Связь с группой
        public int StudentGroupId { get; set; }
        public StudentGroup StudentGroup { get; set; }

        // Преподаватель, отметивший пропуск
        public int InstructorId { get; set; }
        public User Instructor { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        [Range(2, 16, ErrorMessage = "Количество пропущенных часов должно быть от 2 до 16")]
        public int Hours { get; set; }

        public string? Reason { get; set; } // Причина пропуска (если известна)

        public bool IsExcused { get; set; } // Уважительная причина

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string? Comment { get; set; }
    }
}
