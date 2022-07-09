using Akka.Actor;
using Akka.Cluster.Tools.PublishSubscribe;
using SocialNetwork.Api.Messages;
using SocialNetwork.Api.Models;

namespace SocialNetwork.Api.Actors
{
    public class GroupActor : ReceiveActor
    {
        public Dictionary<Guid, GroupMessage> _groupMessages;
        public List<string> _members;
        public string _owner;
        IActorRef _mediator;

        public static Props CreateProps()
        {
            return Props.Create(() => new GroupActor());
        }

        public GroupActor()
        {
            Receive<CreateGroupMessage>(createGroup =>
            {
                if (_groupMessages is null)
                {
                    _mediator = DistributedPubSub.Get(Context.System).Mediator;

                    _groupMessages = new Dictionary<Guid, GroupMessage>();
                    _members = new List<string> { createGroup.Sender };
                    _owner = createGroup.Sender;

                    Console.WriteLine($"{Sender.Path} sent CreateGroupMessage {createGroup.GroupId} at {DateTime.Now}");
                }
                else
                    Console.WriteLine($"{Sender.Path} Group {createGroup.GroupId} exist.");
            });

            Receive<JoinGroupMessage>(joinGroup =>
            {
                if (_members is null)
                {
                    Console.WriteLine($"Group {joinGroup.GroupId} not exist.");
                    return;
                }

                if (!_members.Contains(joinGroup.Sender))
                {
                    _members.Add(joinGroup.Sender);
                    Console.WriteLine($"{Sender.Path} sent JoinGroupMessage {joinGroup.GroupId} at {DateTime.Now}");
                }
                else
                    Console.WriteLine($"{Sender.Path} Group {joinGroup.GroupId} already has {joinGroup.Sender}");
            });

            Receive<GroupMessage>(groupMessage =>
            {
                if (_members is null)
                {
                    Console.WriteLine($"Group {groupMessage.GroupId} not exist.");
                    return;
                }

                if (!_members.Contains(groupMessage.Sender))
                {
                    Console.WriteLine($"{groupMessage.Sender} no member of group {groupMessage.GroupId}");
                    return;
                }

                _groupMessages.Add(groupMessage.MessageId, groupMessage);

                _mediator.Tell(new Publish(groupMessage.GroupId, new UserGroupMessage
                {
                    GroupId = groupMessage.GroupId,
                    UserId = groupMessage.Sender,
                    Message = groupMessage.Message,
                    MessageId = groupMessage.MessageId
                }));
            });

            Receive<GroupStatusMessage>(message => {
                Sender.Tell(new GroupStatusResponse
                {
                    GroupId = message.GroupId,
                    Members = _members,
                    Owner = _owner,
                    Messages = _groupMessages
                });
            });                
        }
    }

    public class GroupActorProxy : ReceiveActor
    {
        private readonly IActorRef _groupShardRegion;
        private readonly IActorRef _UserShardRegion;

        public static Props CreateProps(IActorRef groupShardRegion, IActorRef userShardRegion)
        {
            return Props.Create(() => new GroupActorProxy(groupShardRegion, userShardRegion));
        }

        public GroupActorProxy(IActorRef groupShardRegion, IActorRef userShardRegion)
        {
            _UserShardRegion = userShardRegion;
            _groupShardRegion = groupShardRegion;

            Receive<IGroupIdentifier>(message =>
            {
                switch (message)
                {
                    case CreateGroupMessage createGroup:
                    case JoinGroupMessage joinGroup:
                        {
                            _groupShardRegion.Forward(message);
                            _UserShardRegion.Forward(new GroupMemberMessage { GroupId = message.GroupId, UserId = message.Sender });
                            break;
                        }
                    case GroupStatusMessage groupStatus:
                        {
                            _groupShardRegion.Forward(message);
                            break;
                        }
                    default:
                        _groupShardRegion.Tell(message);
                        break;
                }
            });
        }
    }
}