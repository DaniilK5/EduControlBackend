using EduControlBackend.Models.StudentModels;

namespace EduControlBackend.Models.LoginAndReg
{
    public class User
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }

        // Информация о студенте
        public string? StudentGroup { get; set; }
        public string? Address { get; set; }
        public string? PhoneNumber { get; set; }
        public string? SocialStatus { get; set; } // Например: "Сирота", "Многодетная семья" и т.д.
        public string? StudentId { get; set; } // Номер студенческого билета

        // Связь со StudentGroup для студентов
        public int? StudentGroupId { get; set; }
        public StudentGroup? Group { get; set; }


        // Для студентов - связь с родителями
        public ICollection<User> Parents { get; set; } = new List<User>();

        // Для родителей - связь с детьми
        public ICollection<User> Children { get; set; } = new List<User>();

        // Группы, где пользователь является куратором (для преподавателей)
        public ICollection<StudentGroup> CuratedGroups { get; set; } = new List<StudentGroup>();
    }
}
