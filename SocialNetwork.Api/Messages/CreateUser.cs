using Akka.Cluster.Sharding;
using System;

namespace SocialNetwork.Api.Messages
{
    public interface IUserId
    {
        string UserId { get; }
    }

    public class CreateUser : IUserId
    {
        public string UserId { get; set; }
        
        public CreateUser(string userId)
        {
            UserId = userId;
        }
    }

    public class UserConversationMessage : IUserId
    {
        public string UserId { get; set; }
        public string ConversationId { get; set; }
        public string AnotherUserId { get; set; }
        public string Message { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class CreateUserExtractor : HashCodeMessageExtractor
    {
        public CreateUserExtractor(int maxNumberOfShards) : base(maxNumberOfShards)
        {
        }

        public override string EntityId(object message)
        {
            var createUser = message as CreateUser;
            return createUser.UserId;
        }
    }
}
