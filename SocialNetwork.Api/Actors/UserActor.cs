using Akka.Actor;
using Akka.Cluster;
using Akka.Cluster.Sharding;
using SocialNetwork.Api.Messages;
using SocialNetwork.Api.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SocialNetwork.Api.Actors
{
    public class UserActor : ReceiveActor
    {
        private List<Conversation> _conversations;

        public UserActor()
        {
            Receive<IUserId>(message =>
            {
                switch (message)
                {
                    case CreateUser createUser:
                        {
                            Console.WriteLine($"{Sender.Path} sent {message.UserId}");
                            if (_conversations == null) _conversations = new List<Conversation>();
                            break;
                        }
                    case UserConversationMessage userConversation:
                        {
                            Console.WriteLine($"{userConversation.UserId} <==> {userConversation.AnotherUserId} at {userConversation.CreatedAt}");

                            if (!_conversations.Any(c => c.ConversationId.Equals(userConversation.ConversationId, StringComparison.OrdinalIgnoreCase)))
                                _conversations.Add(new Conversation
                                {
                                    ConversationId = userConversation.ConversationId,
                                    UserId1 = userConversation.UserId,
                                    UserId2 = userConversation.AnotherUserId
                                });
                            break;
                        }
                    default:
                        break;
                }
            });
        }

        public static Props CreateProps()
        {
            return Props.Create(() => new UserActor());
        }
    }

    public class UserActorProxy : ReceiveActor
    {
        private readonly IActorRef _shardRegion;

        public UserActorProxy(IActorRef shardRegion)
        {
            _shardRegion = shardRegion;
            Receive<IUserId>(message =>
            {
                _shardRegion.Tell(message, Self);
            });

            Receive<object>(message => {
                var cluster = Cluster.Get(Context.System);
            });
        }

        public static Props CreateProps(IActorRef shardRegion)
        {
            return Props.Create(() => new UserActorProxy(shardRegion));
        }
    }
}