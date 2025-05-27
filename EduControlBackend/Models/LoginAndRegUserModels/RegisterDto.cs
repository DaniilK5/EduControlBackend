namespace EduControlBackend.Models.LoginAndReg
{
    public class RegisterDto
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }
        public string? StudentGroup { get; set; } // Необязательное поле, используется только для студентов
    }
}