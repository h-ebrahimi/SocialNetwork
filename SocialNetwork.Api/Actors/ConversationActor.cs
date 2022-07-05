using Akka.Actor;
using Akka.Persistence;
using SocialNetwork.Api.Messages;
using System;
using System.Collections.Generic;

namespace SocialNetwork.Api.Actors
{
    public class ConversationActor : ReceivePersistentActor
    {
        public override string PersistenceId { get; } = "conversation";
        private List<ConversationMessage> _conversations;
        public static Props CreateProps()
        {            
            return Props.Create(() => new ConversationActor());
        }

        public ConversationActor()
        {
            _conversations = new List<ConversationMessage>();
            //PersistenceId = persistenceId;
            Command<ConversationMessage>(message =>
            {
                Console.WriteLine($"{Sender.Path} sent {message.ConversationId}");
                Persist(message, evt =>
                {
                    _conversations.Add(message);
                });

                if (LastSequenceNr % 10 == 0)
                    SaveSnapshot(message);
            });

            Recover<SnapshotOffer>(offer =>
            {
                if (offer.Snapshot is ConversationMessage state)
                {
                    // Do
                }
            });

            Command<SaveSnapshotSuccess>(success =>
            {
                Console.WriteLine($"{success.Metadata.SequenceNr} {success.Metadata.Timestamp}");
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

            Receive<ConversationMessage>(message =>
            {
                _selfShardRegion.Tell(message, Self);
                // Send to sender and receiver
                _userShardRegion.Tell(new UserConversationMessage
                {
                    AnotherUserId = message.UserId2,
                    ConversationId = message.ConversationId,
                    CreatedAt = message.CreatedAt,
                    UserId = message.UserId1
                }, Self);
                _userShardRegion.Tell(new UserConversationMessage
                {
                    AnotherUserId = message.UserId1,
                    ConversationId = message.ConversationId,
                    CreatedAt = message.CreatedAt,
                    UserId = message.UserId2
                }, Self);
            });
        }
    }
}