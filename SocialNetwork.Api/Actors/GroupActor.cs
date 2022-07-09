using Akka.Actor;
using Akka.Cluster.Tools.PublishSubscribe;
using SocialNetwork.Api.Messages;
using System;
using System.Collections.Generic;

namespace SocialNetwork.Api.Actors
{
    public class GroupActor : ReceiveActor
    {
        public List<string> _groupMessage;
        public List<string> _members;
        public string _owner;
        IActorRef _mediator;

        public static Props CreateProps()
        {
            return Props.Create(() => new GroupActor());
        }

        public GroupActor()
        {
            Receive<IGroupMessage>(message =>
            {
                switch (message)
                {
                    case CreateGroupMessage createGroup:
                        {
                            if (_groupMessage is null)
                            {
                                _mediator = DistributedPubSub.Get(Context.System).Mediator;
                                
                                _groupMessage = new List<string>();
                                _members = new List<string> { createGroup.Sender };
                                _owner = createGroup.Sender;

                                Console.WriteLine($"{Sender.Path} sent CreateGroupMessage {message.GroupId} at {DateTime.Now}");
                            }
                            else
                                Console.WriteLine($"{Sender.Path} Group {message.GroupId} exist.");
                            break;
                        }
                    case JoinGroupMessage joinGroup:
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
                            break;
                        }
                    case GroupMessage groupMessage:
                        {
                            if (_members is null)
                            {
                                Console.WriteLine($"Group {groupMessage.GroupId} not exist.");
                                break;
                            }

                            if (!_members.Contains(groupMessage.Sender))
                            {
                                Console.WriteLine($"{groupMessage.Sender} no member of group {groupMessage.GroupId}");
                                break;
                            }

                            _mediator.Tell(new Publish(groupMessage.GroupId, new UserGroupMessage
                            {
                                GroupId = groupMessage.GroupId,
                                UserId = groupMessage.Sender,
                                Message = groupMessage.Message
                            }));
                            break;
                        }
                    default:
                        break;
                }
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

            Receive<IGroupMessage>(message =>
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
                    default:
                        _groupShardRegion.Tell(message);
                        break;
                }
            });
        }
    }
}