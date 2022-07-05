namespace SocialNetwork.Api.Models
{
    public class CreateGroup
    {
        public string Name { get; set; }
        public string Sender { get; set; }
    }

    public class JoinGroup
    {        
        public string Sender { get; set; }
    }    
}