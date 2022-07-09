using SocialNetwork.Api.Messages;

namespace SocialNetwork.Api.Models
{
    public class CreateGroup
    {
        public string Name { get; set; }
        public string Sender { get; set; }
    }

    public class JoinGroup
    {
        public string Sender { get; set; }
    }

    public class MessageToGroup
    {
        public string Sender { get; set; }
        public string Message { get; set; }
    }

    public class GroupStatusResponse
    {
        public string GroupId { get; set; }
        public string Owner { get; set; }
        public Dictionary<Guid, GroupMessage> Messages { get; set; }
        public List<string> Members { get; set; }
    }
}