using EduControlBackend.Models.Chat;
using EduControlBackend.Models.LoginAndReg;

namespace EduControlBackend.Models
{
    public class Message
    {
        public int Id { get; set; }
        public int SenderId { get; set; }
        public User Sender { get; set; }
        public int? ReceiverId { get; set; } // Nullable для личных сообщений
        public User? Receiver { get; set; }
        public int? GroupChatId { get; set; } // Для групповых сообщений
        public GroupChat? GroupChat { get; set; }
        public string Content { get; set; }
        public DateTime Timestamp { get; set; }
        public string? AttachmentPath { get; set; }
        public string? AttachmentName { get; set; }
        public string? AttachmentType { get; set; }
        public bool IsDeleted { get; set; } // Для "мягкого" удаления
    }
}
