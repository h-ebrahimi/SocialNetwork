using Akka.Actor;
using SocialNetwork.Api.Messages;
using System;
using System.Collections.Generic;

namespace SocialNetwork.Api.Actors
{
    public class ChannelActor : ReceiveActor
    {
        public List<string> _channelMessage;
        public List<string> _members;
        public string _owner;

        public static Props CreateProps()
        {
            return Props.Create(() => new ChannelActor());
        }

        public ChannelActor()
        {
            Receive<IChannelMessage>(message =>
            {
                switch (message)
                {
                    case CreateChannelMessage createChannel:
                        {
                            if (_channelMessage is null)
                            {
                                _channelMessage = new List<string>();
                                _members = new List<string>();
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

            Receive<IChannelMessage>(message =>
            {
                _channelShardRegion.Tell(message);
            });
        }
    }
}
