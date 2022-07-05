using Akka.Cluster.Sharding;

namespace SocialNetwork.Api.Messages
{
    public abstract class BaseMessage : IUserId
    {
        public string UserId { get; set; }
        public string Message { get; set; }
        public string Sender { get; set; }
        public string Receiver { get; set; }
    }

    public class InComingMessage : BaseMessage
    {

    }

    public class OutGoingMessage : BaseMessage
    {

    }

    public class MesssageExtractor : HashCodeMessageExtractor
    {
        public MesssageExtractor(int maxNumberOfShards) : base(maxNumberOfShards)
        {
        }

        public override string EntityId(object message)
        {
            switch (message)
            {
                case ShardRegion.StartEntity start: return start.EntityId;
                case IUserId e: return e.UserId;
            }

            return string.Empty;
        }

        public override object EntityMessage(object message)
        {
            switch (message)
            {
                case BaseMessage e: return e.Message;
                default:
                    return message;
            }
        }
    }
}
