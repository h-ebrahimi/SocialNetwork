using Akka.Cluster.Sharding;

namespace SocialNetwork.Api.Messages
{
    public interface IGroupIdentifier
    {
        string GroupId { get; set; }
        string Sender { get; set; }
    }

    public interface IGroupMessage
    {
        Guid MessageId { get; set; } 
        string Message { get; set; }
    }

    public class CreateGroupMessage : IGroupIdentifier 
    {
        public string GroupId { get; set; }
        public string Sender { get; set; }
    }

    public class JoinGroupMessage : IGroupIdentifier
    {
        public string GroupId { get; set; }
        public string Sender { get; set; }
    }

    public class GroupMessage : IGroupIdentifier , IGroupMessage
    {
        public Guid MessageId { get; set; } = Guid.NewGuid();
        public string Message { get; set; }
        public string GroupId { get; set; }
        public string Sender { get; set; }
    }

    public class GroupStatusMessage : IGroupIdentifier
    {
        public string GroupId { get; set; }
        public string Sender { get; set; }
    }

    public class GroupMessageExtractor : HashCodeMessageExtractor
    {
        public GroupMessageExtractor(int maxNumberOfShards) : base(maxNumberOfShards)
        {
        }

        public override string EntityId(object message)
        {
            var groupMessage = message as IGroupIdentifier;
            return groupMessage.GroupId;
        }

        public override object EntityMessage(object message)
        {
            var groupMessage = message as IGroupIdentifier;
            return groupMessage;
        }
    }
}
