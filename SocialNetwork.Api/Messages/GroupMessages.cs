using Akka.Cluster.Sharding;

namespace SocialNetwork.Api.Messages
{
    public interface IGroupMessage
    {
        string GroupId { get; set; }
        string Sender { get; set; }
        string Message { get; set; }
    }

    public class CreateGroupMessage : IGroupMessage
    {
        public string GroupId { get; set; }
        public string Sender { get; set; }
        public string Message { get; set; }
    }

    public class JoinGroupMessage : IGroupMessage
    {
        public string GroupId { get; set; }
        public string Sender { get; set; }
        public string Message { get; set; }
    }

    public class GroupMessageExtractor : HashCodeMessageExtractor
    {
        public GroupMessageExtractor(int maxNumberOfShards) : base(maxNumberOfShards)
        {
        }

        public override string EntityId(object message)
        {
            var groupMessage    = message as IGroupMessage;
            return groupMessage.GroupId;
        }

        public override object EntityMessage(object message)
        {
            var groupMessage = message as IGroupMessage;
            return groupMessage.Message;
        }
    }
}
