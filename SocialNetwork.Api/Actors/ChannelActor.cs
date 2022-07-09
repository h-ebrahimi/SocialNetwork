﻿using Akka.Actor;
using Akka.Cluster.Tools.PublishSubscribe;
using SocialNetwork.Api.Messages;

namespace SocialNetwork.Api.Actors
{
    public class ChannelActor : ReceiveActor
    {
        public List<string> _channelMessage;
        public List<string> _members;
        public string _owner;
        IActorRef _mediator;

        public static Props CreateProps()
        {
            return Props.Create(() => new ChannelActor());
        }

        public ChannelActor()
        {
            Receive<IChannelIdentifier>(message =>
            {
                switch (message)
                {
                    case CreateChannelMessage createChannel:
                        {
                            if (_channelMessage is null)
                            {
                                _mediator = DistributedPubSub.Get(Context.System).Mediator;

                                _channelMessage = new List<string>();
                                _members = new List<string>() { createChannel.Sender };
                                _owner = createChannel.Sender;

                                Console.WriteLine($"{Sender.Path} sent CreateChannelMessage {message.ChannelId} at {DateTime.Now}");
                            }
                            else
                                Console.WriteLine($"{Sender.Path} Channel {message.ChannelId} exist.");
                            break;
                        }
                    case JoinChannelMessage joinChannel:
                        {
                            if (_members is null)
                            {
                                Console.WriteLine($" Channel {joinChannel.ChannelId} not exist.");
                                break;
                            }

                            if (!_members.Contains(joinChannel.Sender))
                            {
                                _members.Add(joinChannel.Sender);
                                Console.WriteLine($"{Sender.Path} sent JoinChannelMessage {joinChannel.ChannelId} at {DateTime.Now}");
                            }
                            else
                                Console.WriteLine($"{Sender.Path} Channel {joinChannel.ChannelId} already has {joinChannel.Sender}");
                            break;
                        }
                    case ChannelMessage channelMessage:
                        {
                            if (_members is null)
                            {
                                Console.WriteLine($"Channel {channelMessage.ChannelId} not exist.");
                                break;
                            }

                            if (!_owner.Equals(channelMessage.Sender, StringComparison.OrdinalIgnoreCase))
                            {
                                Console.WriteLine($"{channelMessage.Sender} not owner of Channel {channelMessage.ChannelId}");
                                break;
                            }

                            _mediator.Tell(new Publish(channelMessage.ChannelId, new UserChannelMessage
                            {
                                ChannelId = channelMessage.ChannelId,
                                UserId = channelMessage.Sender,
                                Message = channelMessage.Message
                            }));
                            break;
                        }
                    default:
                        break;
                }
            });
        }
    }

    public class ChannelActorProxy : ReceiveActor
    {
        private readonly IActorRef _channelShardRegion;
        private readonly IActorRef _UserShardRegion;

        public static Props CreateProps(IActorRef channelShardRegion, IActorRef userShardRegion)
        {
            return Props.Create(() => new ChannelActorProxy(channelShardRegion, userShardRegion));
        }

        public ChannelActorProxy(IActorRef channelShardRegion, IActorRef userShardRegion)
        {
            _UserShardRegion = userShardRegion;
            _channelShardRegion = channelShardRegion;

            Receive<IChannelIdentifier>(message =>
            {
                switch (message)
                {
                    case CreateChannelMessage createGroup:
                    case JoinChannelMessage joinGroup:
                        {
                            _channelShardRegion.Forward(message);
                            _UserShardRegion.Forward(new ChannelMemberMessage { ChannelId = message.ChannelId, UserId = message.Sender });
                            break;
                        }
                    default:
                        _channelShardRegion.Tell(message);
                        break;
                }
            });
        }
    }
}
