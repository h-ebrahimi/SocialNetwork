namespace SocialNetwork.Api.Models
{
    public class Conversation
    {
        public string ConversationId { get; set; }
        public string UserId1 { get; set; }
        public string UserId2 { get; set; }
    }

    public class SendConversationMessage
    {
        public string UserId1 { get; set; }
        public string UserId2 { get; set; }
        public string Message { get; set; }
    }
}