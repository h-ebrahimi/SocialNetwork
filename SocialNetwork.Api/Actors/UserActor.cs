using Akka.Actor;

namespace SocialNetwork.Api.Actors
{
    public class UserActor : ReceiveActor
    {
        public UserActor()
        {
            Receive<string>(message =>
            {
                Console.WriteLine($"{Sender.Path} sent {message}");
            });
        }
    }
}