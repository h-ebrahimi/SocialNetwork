using Akka.Actor;
using SocialNetwork.Api.Messages;
using System;
using System.Collections.Generic;

namespace SocialNetwork.Api.Actors
{
    public class GroupActor : ReceiveActor
    {
        public List<string> _groupMessage;
        public List<string> _members;

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
                                _groupMessage = new List<string>();
                                _members = new List<string>
                                {
                                    createGroup.Sender
                                };

                                Console.WriteLine($"{Sender.Path} sent CreateGroupMessage {message.GroupId} at {DateTime.Now}");
                            }
                            else
                                Console.WriteLine($"{Sender.Path} Group {message.GroupId} exist.");
                            break;
                        }
                    case JoinGroupMessage joinGroup:
                        {
                            if (!_members.Contains(joinGroup.Sender))
                            {
                                _members.Add(joinGroup.Sender);
                                Console.WriteLine($"{Sender.Path} sent JoinGroupMessage {joinGroup.GroupId} at {DateTime.Now}");
                            }
                            else
                                Console.WriteLine($"{Sender.Path} Group {joinGroup.GroupId} already has {joinGroup.Sender}");
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
                _groupShardRegion.Tell(message);
            });
        }
    }
}