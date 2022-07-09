using SocialNetwork.Api.Messages;

namespace SocialNetwork.Api.Models
{
    public class CreateChannel
    {
        public string Name { get; set; }
        public string Sender { get; set; }
    }

    public class JoinChannel
    {
        public string Sender { get; set; }
    }

    public class MessageToChannel
    {
        public string Sender { get; set; }
        public string Message { get; set; }
    }

    public class ChannelStatusResponse
    {
        public string ChannelId { get; set; }
        public string Owner { get; set; }
        public Dictionary<Guid, ChannelMessage> Messages { get; set; }
        public List<string> Members { get; set; }
        public DateTime CreationDate { get; set; }
    }
}
