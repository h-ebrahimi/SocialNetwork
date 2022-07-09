using Akka.Cluster.Sharding;

namespace SocialNetwork.Api.Messages
{
    public interface IChannelMessage
    {
        string ChannelId { get; set; }
        string Sender { get; set; }
        string Message { get; set; }
    }

    public class CreateChannelMessage : IChannelMessage
    {
        public string ChannelId { get; set; }
        public string Sender { get; set; }
        public string Message { get; set; }
    }

    public class JoinChannelMessage : IChannelMessage
    {
        public string ChannelId { get; set; }
        public string Sender { get; set; }
        public string Message { get; set; }
    }

    public class ChannelMessage : IChannelMessage
    {
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
            var channelMessage = message as IChannelMessage;
            return channelMessage.ChannelId;
        }

        public override object EntityMessage(object message)
        {
            var channelMessage = message as IChannelMessage;
            return channelMessage;
        }
    }
}