public class SendMessageDto
{

        public int? ReceiverId { get; set; }
        public int? GroupChatId { get; set; }
        public string Content { get; set; }
        public IFormFile? Attachment { get; set; }

}