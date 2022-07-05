namespace SocialNetwork.Api.Options
{
    public class AkkaOptions
    {
        public BaseAkkaOptions CreationUser { get; set; }
        public BaseAkkaOptions Conversation { get; set; }
    }

    public class BaseAkkaOptions
    {
        public string ActorSystemName { get; set; }
        public string Hostname { get; set; }
        public int Port { get; set; }
        public string ProtocolType { get; set; }
        public int MaxNumberOfShards { get; set; }
        public string ShardTypename { get; set; }
    }
}
