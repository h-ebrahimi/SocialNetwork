using Akka.Actor;
using Akka.Cluster.Tools.PublishSubscribe;
using SocialNetwork.Api.Messages;
using SocialNetwork.Api.Models;

namespace SocialNetwork.Api.Actors
{
    public class UserActor : ReceiveActor
    {
        private List<Conversation> _conversations;
        private List<string> _groups;
        private List<string> _channels;
        private int point = 0;
        private DateTime? _creationDate = null;
        private string _userId;

        public UserActor()
        {
            Context.SetReceiveTimeout(TimeSpan.FromMinutes(10));
            _conversations = new List<Conversation>();
            _groups = new List<string>();
            _channels = new List<string>();

            Receive<CreateUser>(createUser =>
            {
                _creationDate = DateTime.Now;
                point = 0;
                _userId = createUser.UserId;
                Console.WriteLine($"{createUser.UserId} created at {_creationDate}");
            });

            Receive<IUserId>(message =>
            {
                if (_creationDate is null)
                {
                    Console.WriteLine($"{_userId} not exist.");
                    Sender.Tell(new UserStatusResponseMessage());
                    return;
                }
                
                switch (message)
                {
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
                            point++;
                            break;
                        }
                    case UserStatusRequestMessage userStatus:
                        {
                            Sender.Tell(new UserStatusResponseMessage
                            {
                                UserId = userStatus.UserId,
                                Point = point,
                                Channels = _channels,
                                Conversations = _conversations.Select(s => s.ConversationId).ToList(),
                                Groups = _groups
                            });
                            break;
                        }
                    case GroupMemberMessage memberMessage:
                        {
                            if (_groups.Any(g => g.Equals(memberMessage.GroupId, StringComparison.OrdinalIgnoreCase)))
                                break;

                            var mediator = DistributedPubSub.Get(Context.System).Mediator;
                            mediator.Tell(new Subscribe(memberMessage.GroupId, Self));

                            _groups.Add(memberMessage.GroupId);
                            point += 2;
                            break;
                        }
                    case UserGroupMessage groupMessage:
                        {
                            Console.WriteLine($"Group {groupMessage.GroupId} , {groupMessage.UserId} sent {groupMessage.Message}");
                            point++;
                            break;
                        }
                    case ChannelMemberMessage memberMessage:
                        {
                            if (_channels.Any(g => g.Equals(memberMessage.ChannelId, StringComparison.OrdinalIgnoreCase)))
                            {
                                Console.WriteLine($"You must first join to Channel {memberMessage.ChannelId} and then sent messages.");
                                break;
                            }

                            var mediator = DistributedPubSub.Get(Context.System).Mediator;
                            mediator.Tell(new Subscribe(memberMessage.ChannelId, Self));

                            _channels.Add(memberMessage.ChannelId);
                            point += 3;
                            break;
                        }
                    case UserChannelMessage channelMessage:
                        {
                            Console.WriteLine($"Channel {channelMessage.ChannelId} , {channelMessage.UserId} sent {channelMessage.Message}");
                            point++;
                            break;
                        }
                    default:
                        break;
                }
            });

            Receive<SubscribeAck>(ack =>
            {
                Console.WriteLine($"{_userId} joined to {ack.Subscribe.Topic}.");
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
                switch (message)
                {
                    case UserStatusRequestMessage userStatus:
                        _shardRegion.Forward(message);
                        break;
                    default:
                        _shardRegion.Tell(message, Self);
                        break;
                }
            });
        }

        public static Props CreateProps(IActorRef shardRegion)
        {
            return Props.Create(() => new UserActorProxy(shardRegion));
        }
    }
}