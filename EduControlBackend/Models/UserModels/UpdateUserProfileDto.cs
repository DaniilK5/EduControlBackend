namespace EduControlBackend.Models.UserModels
{
    public class UpdateUserProfileDto
    {
        public string FullName { get; set; }
        public string? Address { get; set; }
        public string? PhoneNumber { get; set; }
        public string? SocialStatus { get; set; }
    }
}
