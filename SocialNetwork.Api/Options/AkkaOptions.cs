namespace SocialNetwork.Api.Options
{
    public class AkkaOptions
    {
        public string ActorSystemName { get; set; }
        public string Hostname { get; set; }
        public int Port { get; set; }
        public string ProtocolType { get; set; }
        
        public BaseAkkaOptions CreationUser { get; set; }
        public BaseAkkaOptions Conversation { get; set; }
        public BaseAkkaOptions Group { get; set; }
        public BaseAkkaOptions Channel { get; set; }
    }

    public class BaseAkkaOptions
    {
        public int MaxNumberOfShards { get; set; }
        public string ShardTypename { get; set; }
    }
}