using Akka.Actor;
using Akka.Cluster.Tools.PublishSubscribe;
using SocialNetwork.Api.Messages;
using SocialNetwork.Api.Models;

namespace SocialNetwork.Api.Actors
{
    public class ChannelActor : ReceiveActor
    {
        public Dictionary<Guid, ChannelMessage> _channelMessages;
        public List<string> _members;
        public string _owner;
        IActorRef _mediator;

        public static Props CreateProps()
        {
            return Props.Create(() => new ChannelActor());
        }

        public ChannelActor()
        {
            Context.SetReceiveTimeout(TimeSpan.FromMinutes(10));
            
            Receive<CreateChannelMessage>(createChannel =>
            {
                if (_channelMessages is null)
                {
                    _mediator = DistributedPubSub.Get(Context.System).Mediator;

                    _channelMessages = new Dictionary<Guid, ChannelMessage>();
                    _members = new List<string>() { createChannel.Sender };
                    _owner = createChannel.Sender;

                    Console.WriteLine($"{createChannel.Sender} sent CreateChannelMessage {createChannel.ChannelId} at {DateTime.Now}");
                }
                else
                    Console.WriteLine($"{createChannel.Sender} Channel {createChannel.ChannelId} already exist.");
            });

            Receive<JoinChannelMessage>(joinChannel =>
            {
                if (_members is null)
                {
                    Console.WriteLine($" Channel {joinChannel.ChannelId} not exist.");
                    return;
                }

                if (!_members.Contains(joinChannel.Sender))
                {
                    _members.Add(joinChannel.Sender);
                    Console.WriteLine($"{joinChannel.Sender} sent JoinChannelMessage {joinChannel.ChannelId} at {DateTime.Now}");
                }
                else
                    Console.WriteLine($"{joinChannel.Sender} Channel {joinChannel.ChannelId} already has {joinChannel.Sender}");
            });

            Receive<ChannelMessage>(channelMessage =>
            {
                if (_members is null)
                {
                    Console.WriteLine($"Channel {channelMessage.ChannelId} not exist.");
                    return;
                }

                if (!_owner.Equals(channelMessage.Sender, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"{channelMessage.Sender} not owner of Channel {channelMessage.ChannelId}");
                    return;
                }

                _channelMessages.Add(channelMessage.MessageId, channelMessage);

                _mediator.Tell(new Publish(channelMessage.ChannelId, new UserChannelMessage
                {
                    ChannelId = channelMessage.ChannelId,
                    UserId = channelMessage.Sender,
                    Message = channelMessage.Message,
                    MessageId = channelMessage.MessageId
                }));
            });

            Receive<ChannelStatusMessage>(statusMessage =>
            {
                if (_members is null)
                {
                    Console.WriteLine($"Channel {statusMessage.ChannelId} not exist.");
                    return;
                }
                
                Sender.Tell(new ChannelStatusResponse
                {
                    ChannelId = statusMessage.ChannelId,
                    Members = _members,
                    Owner = _owner,
                    Messages = _channelMessages
                });
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
                    case ChannelStatusMessage channelStatus:
                        {
                            _channelShardRegion.Forward(channelStatus);
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
