namespace EduControlBackend.Models.UserModels
{
    public class UserProfileDto
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public string? StudentGroup { get; set; }
        public string? Address { get; set; }
        public string? PhoneNumber { get; set; }
        public string? SocialStatus { get; set; }
        public string? StudentId { get; set; }
        public int? StudentGroupId { get; set; }
    }
}
