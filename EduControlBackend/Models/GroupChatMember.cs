namespace EduControlBackend.Models
{
    public class GroupChatMember
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
        public int GroupChatId { get; set; }
        public GroupChat GroupChat { get; set; }
        public bool IsAdmin { get; set; }
        public DateTime JoinedAt { get; set; }
    }
}