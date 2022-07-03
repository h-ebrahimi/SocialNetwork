namespace SocialNetwork.Api.Options
{
    public class AkkaOptions
    {
        public string ActorSystemName { get; set; }
        public string HostName { get; set; }
        public string Port { get; set; }
        public string ProtocolType { get; set; }
    }

    public class AkkaConfiguration
    {
        public AkkaConfiguration()
        {

        }
    }
}
