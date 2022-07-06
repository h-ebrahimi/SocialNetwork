using Akka.Cluster.Sharding;
using System;

namespace SocialNetwork.Api.Messages
{
    public interface IConversationMessage
    {
        public string ConversationId { get; set; }
    }

    public class GetConversationMessage : IConversationMessage
    {
        public string ConversationId { get; set; }
    }

    public class ConversationMessage : IConversationMessage
    {
        public string ConversationId { get { return $"{UserId1}-{UserId2}"; } set { } }
        public string UserId1 { get; set; }
        public string UserId2 { get; set; }
        public string Message { get; set; }
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    }

    public class ConversationMessageExtractor : HashCodeMessageExtractor
    {
        public ConversationMessageExtractor(int maxNumberOfShards) : base(maxNumberOfShards)
        {
        }

        public string ConversationId { get { return $"{UserId1}-{UserId2}"; } }
        public string UserId1 { get; set; }
        public string UserId2 { get; set; }
        public string Message { get; set; }
        public DateTime CreatedAt { get { return DateTime.UtcNow; } }

        public override string EntityId(object message)
        {
            switch (message)
            {
                case ShardRegion.StartEntity start: return start.EntityId;
                case IConversationMessage cme:
                    return cme.ConversationId;
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