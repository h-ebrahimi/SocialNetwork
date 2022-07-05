using Akka.Cluster.Sharding;
using System;

namespace SocialNetwork.Api.Messages
{
    public class ConversationMessage
    {
        public string ConversationId { get { return $"{UserId1}-{UserId2}"; } }
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
        public DateTime CreatedAt { get {return DateTime.UtcNow; } }

        public override string EntityId(object message)
        {
            var cme = message as ConversationMessage;
            return cme.ConversationId;
        }

        public override object EntityMessage(object message)
        {
            var cme = message as ConversationMessage;
            return cme.Message;
        }
    }
}