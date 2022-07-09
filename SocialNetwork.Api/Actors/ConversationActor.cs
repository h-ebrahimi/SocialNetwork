using Akka.Actor;
using SocialNetwork.Api.Messages;
using System;
using System.Collections.Generic;

namespace SocialNetwork.Api.Actors
{
    public class ConversationActor : ReceiveActor
    {
        private List<ConversationMessage> _conversations;
        public static Props CreateProps()
        {
            return Props.Create(() => new ConversationActor());
        }

        public ConversationActor()
        {
            Context.SetReceiveTimeout(TimeSpan.FromMinutes(10));
            _conversations = new List<ConversationMessage>();
            Receive<IConversationMessage>(message =>
            {
                switch (message)
                {
                    case ConversationMessage conversationMessage:
                        {
                            Console.WriteLine($"{Sender.Path} sent {message.ConversationId}");
                            _conversations.Add(conversationMessage);
                            break;
                        }
                    case GetConversationMessage getConversation:
                        {
                            Sender.Tell(_conversations);
                            Console.WriteLine($"Send conversation to {Sender.Path} ");
                            break;
                        }
                    default:
                        break;
                }

            });
        }
    }

    public class ConversationActorProxy : ReceiveActor
    {
        private readonly IActorRef _selfShardRegion;
        private readonly IActorRef _userShardRegion;

        public ConversationActorProxy(IActorRef selfShardRegion, IActorRef userShardRegion)
        {
            _selfShardRegion = selfShardRegion;
            _userShardRegion = userShardRegion;

            Receive<IConversationMessage>(message =>
            {
                switch (message)
                {
                    case ConversationMessage conversationMessage:
                        {
                            _selfShardRegion.Tell(message, Self);
                            // Send to sender and receiver
                            _userShardRegion.Tell(new UserConversationMessage
                            {
                                AnotherUserId = conversationMessage.UserId2,
                                ConversationId = conversationMessage.ConversationId,
                                CreatedAt = conversationMessage.CreatedAt,
                                UserId = conversationMessage.UserId1
                            }, Self);
                            _userShardRegion.Tell(new UserConversationMessage
                            {
                                AnotherUserId = conversationMessage.UserId1,
                                ConversationId = conversationMessage.ConversationId,
                                CreatedAt = conversationMessage.CreatedAt,
                                UserId = conversationMessage.UserId2
                            }, Self);
                            break;
                        }
                    case GetConversationMessage getConversation:
                        {
                            _selfShardRegion.Forward(message);
                            break;
                        }
                    default:
                        break;
                }

            });
        }
    }
}