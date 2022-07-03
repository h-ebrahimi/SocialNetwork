using System;

namespace SocialNetwork.Api.Messages
{
    public abstract class BaseMessage
    {
        public Guid MessageId { get; set; }
        public string Message { get; set; }
        public string Sender { get; set; }
        public string Receiver { get; set; }
    }

    public class InComingMessage : BaseMessage
    {

    }

    public class OutGoingMessage : BaseMessage
    {

    }
}
