using EduControlBackend.Models.LoginAndReg;
using System.Text.Json.Serialization;

namespace EduControlBackend.Models.Chat
{
    public class GroupChatMember
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
        public int GroupChatId { get; set; }
        
        [JsonIgnore]
        public GroupChat GroupChat { get; set; }
        
        public bool IsAdmin { get; set; }
        public DateTime JoinedAt { get; set; }
    }
}