namespace EduControlBackend.Models
{
    public class GroupChat
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<GroupChatMember> Members { get; set; } = new();
        public List<Message> Messages { get; set; } = new();
    }
}