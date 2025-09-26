using EduControlBackend.Models.LoginAndReg;
using System.ComponentModel.DataAnnotations;

namespace EduControlBackend.Models.StudentModels
{
    public class StudentGroup
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } // Например, "ПИ-231"

        public string? Description { get; set; }

        // Куратор группы
        public int? CuratorId { get; set; }
        public User? Curator { get; set; }

        // Студенты группы
        public ICollection<User> Students { get; set; } = new List<User>();
    }
}
