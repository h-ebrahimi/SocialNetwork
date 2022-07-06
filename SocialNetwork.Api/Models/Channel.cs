﻿namespace SocialNetwork.Api.Models
{
    public class CreateChannel
    {
        public string Name { get; set; }
        public string Sender { get; set; }
    }

    public class JoinChannel
    {
        public string Sender { get; set; }
    }
}
