using EduControlBackend.Models.Chat;
using System.Text.Json.Serialization;
namespace EduControlBackend.Models
{
    public class GroupChat
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }
        
        [JsonIgnore]
        public List<GroupChatMember> Members { get; set; } = new();
        
        [JsonIgnore]
        public List<Message> Messages { get; set; } = new();
    }
}