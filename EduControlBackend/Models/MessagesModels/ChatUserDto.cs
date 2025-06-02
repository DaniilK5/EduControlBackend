namespace EduControlBackend.Models.MessagesModels
{
    public class ChatUserDto
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public string? StudentGroup { get; set; }
    }
}
