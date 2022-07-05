using Akka.Actor;
using Akka.Cluster.Hosting;
using Akka.Hosting;
using Akka.Remote.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Petabridge.Cmd.Cluster;
using Petabridge.Cmd.Host;
using Petabridge.Cmd.Remote;
using SocialNetwork.Api.Actors;
using SocialNetwork.Api.Messages;
using SocialNetwork.Api.Options;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<AkkaOptions>(builder.Configuration.GetSection("AkkaOptions"));

// Resolve Akka Configuration
var _options = builder.Configuration.GetSection("AkkaOptions").Get<AkkaOptions>();
builder.Services.AddAkka(_options.CreationUser.ActorSystemName, builder =>
{
    builder
    .WithRemoting(_options.CreationUser.Hostname, _options.CreationUser.Port)
    .WithClustering(new ClusterOptions
    {
        Roles = new[] { "backend" },
        SeedNodes = new[] { new Address($"akka.{_options.CreationUser.ProtocolType}", _options.CreationUser.ActorSystemName, _options.CreationUser.Hostname, _options.CreationUser.Port) }
    })
    .AddPetabridgeCmd(cmd =>
    {
        cmd.RegisterCommandPalette(new RemoteCommands());
        cmd.RegisterCommandPalette(ClusterCommands.Instance);
    })
    .WithShardRegion<IUserId>(_options.CreationUser.ShardTypename,
                p => UserActor.CreateProps(),
                new MesssageExtractor(_options.CreationUser.MaxNumberOfShards), 
                new ShardOptions())
    .WithShardRegion<ConversationMessage>(_options.Conversation.ShardTypename,
                p => ConversationActor.CreateProps(),
                new ConversationMessageExtractor(_options.Conversation.MaxNumberOfShards),
                new ShardOptions())
    .StartActors((actorSystem, actorRegistry) =>
    {
        var userShardRegion = actorRegistry.Get<IUserId>();
        actorRegistry.Register<UserActorProxy>(actorSystem.ActorOf(Props.Create(() => new UserActorProxy(userShardRegion))));
        
        var conversationSharedRegion = actorRegistry.Get<ConversationMessage>();
        actorRegistry.Register<ConversationActorProxy>(actorSystem.ActorOf(Props.Create(() => new ConversationActorProxy(conversationSharedRegion, userShardRegion))));
    });
});
// Resolve Akka Configuration


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapPost("/CreateUser", (CreateUser createUser, IActorRegistry reg) =>
{
    var actor = reg.Get<UserActorProxy>();
    actor.Tell(new CreateUser(createUser.UserId));

    return Task.CompletedTask;
}).WithName("CreateUser");

app.MapPost("/Conversation", (ConversationMessage conversation, IActorRegistry reg) =>
{
    var actor = reg.Get<ConversationActorProxy>();
    actor.Tell(conversation);

    return Task.CompletedTask;
}).WithName("Conversation");

app.Run();