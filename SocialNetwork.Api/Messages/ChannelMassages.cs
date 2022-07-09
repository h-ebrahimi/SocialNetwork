using Akka.Cluster.Sharding;

namespace SocialNetwork.Api.Messages
{
    public interface IChannelIdentifier
    {
        string ChannelId { get; set; }
        string Sender { get; set; }
    }

    public interface IChannelMessage
    {
        Guid MessageId { get; set; }   // MessageId is used to identify the message in the channel.
        string Message { get; set; }
    }

    public class CreateChannelMessage : IChannelIdentifier
    {
        public string ChannelId { get; set; }
        public string Sender { get; set; }
    }

    public class JoinChannelMessage : IChannelIdentifier
    {
        public string ChannelId { get; set; }
        public string Sender { get; set; }
    }

    public class ChannelMessage : IChannelIdentifier, IChannelMessage
    {
        public Guid MessageId { get; set; }
        public string ChannelId { get; set; }
        public string Sender { get; set; }
        public string Message { get; set; }
    }

    public class ChannelMessageExtractor : HashCodeMessageExtractor
    {
        public ChannelMessageExtractor(int maxNumberOfShards) : base(maxNumberOfShards)
        {
        }

        public override string EntityId(object message)
        {
            var channelMessage = message as IChannelIdentifier;
            return channelMessage.ChannelId;
        }

        public override object EntityMessage(object message)
        {
            var channelMessage = message as IChannelIdentifier;
            return channelMessage;
        }
    }
}