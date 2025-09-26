namespace EduControlBackend.Models.LoginAndReg
{
    public class RegisterDto
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }
        public string? StudentGroup { get; set; } // Ќеоб€зательное поле, используетс€ только дл€ студентов

        // ƒл€ родителей - список ID детей
        public List<int>? ChildrenIds { get; set; }
    }
}