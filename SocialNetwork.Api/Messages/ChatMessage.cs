using Akka.Cluster.Sharding;
using System.Collections.Generic;

namespace SocialNetwork.Api.Messages
{
    public abstract class BaseMessage : IUserId
    {
        public string UserId { get; set; }
        public string Message { get; set; }
        public string Sender { get; set; }
        public string Receiver { get; set; }
    }

    public class UserStatusRequestMessage : IUserId
    {
        public string UserId { get; set; }
    }

    public class GroupMemberMessage : IUserId
    {
        public string UserId { get; set; }
        public string GroupId { get; set; }
    }

    public class ChannelMemberMessage : IUserId
    {
        public string UserId { get; set; }
        public string ChannelId { get; set; }
    }

    public class UserGroupMessage : IUserId
    {
        public string GroupId { get; set; }
        public string UserId { get; set; }
        public string Message { get; set; }
    }

    public class UserChannelMessage : IUserId
    {
        public string UserId { get; set; }
        public string ChannelId { get; set; }
        public string Message { get; set; }
    }

    public class UserStatusResponseMessage : IUserId
    {
        public string UserId { get; set; }
        public int Point { get; set; }
        public List<string> Conversations { get; set; }
        public List<string> Groups { get; set; }
        public List<string> Channels { get; set; }        
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
