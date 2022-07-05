using Akka.Actor;
using Akka.Cluster.Hosting;
using Akka.Hosting;
using Akka.Remote.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Petabridge.Cmd.Cluster;
using Petabridge.Cmd.Cluster.Sharding;
using Petabridge.Cmd.Host;
using Petabridge.Cmd.Remote;
using SocialNetwork.Api.Actors;
using SocialNetwork.Api.Messages;
using SocialNetwork.Api.Models;
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
builder.Services.AddAkka(_options.ActorSystemName, builder =>
{
    builder
    .WithRemoting(_options.Hostname, _options.Port)
    .WithClustering(new ClusterOptions
    {
        Roles = new[] { "backend" },
        SeedNodes = new[] { new Address($"akka.{_options.ProtocolType}",
                                                _options.ActorSystemName, 
                                                _options.Hostname, 
                                                _options.Port) 
        }
    })
    .AddPetabridgeCmd(cmd =>
    {
        cmd.RegisterCommandPalette(new RemoteCommands());
        cmd.RegisterCommandPalette(ClusterCommands.Instance);
        cmd.RegisterCommandPalette(ClusterShardingCommands.Instance);
    })
    .WithShardRegion<IUserId>(_options.CreationUser.ShardTypename,
                p => UserActor.CreateProps(),
                new MesssageExtractor(_options.CreationUser.MaxNumberOfShards),
                new ShardOptions())
    .WithShardRegion<ConversationMessage>(_options.Conversation.ShardTypename,
                p => ConversationActor.CreateProps(),
                new ConversationMessageExtractor(_options.Conversation.MaxNumberOfShards),
                new ShardOptions())
    .WithShardRegion<IGroupMessage>(_options.Group.ShardTypename,
                p => GroupActor.CreateProps(),
                new GroupMessageExtractor(_options.Group.MaxNumberOfShards),
                new ShardOptions())
    .StartActors((actorSystem, actorRegistry) =>
    {
        var userShardRegion = actorRegistry.Get<IUserId>();
        actorRegistry.Register<UserActorProxy>(actorSystem.ActorOf(Props.Create(() => new UserActorProxy(userShardRegion))));

        var conversationSharedRegion = actorRegistry.Get<ConversationMessage>();
        actorRegistry.Register<ConversationActorProxy>(actorSystem.ActorOf(Props.Create(() => new ConversationActorProxy(conversationSharedRegion, userShardRegion))));

        var groupShardRegion = actorRegistry.Get<IGroupMessage>();
        actorRegistry.Register<GroupActorProxy>(actorSystem.ActorOf(Props.Create(() => new GroupActorProxy(groupShardRegion, userShardRegion))));
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

// ------------------------------------------------------------Conversation
app.MapPost("/Conversation", (ConversationMessage conversation, IActorRegistry reg) =>
{
    var actor = reg.Get<ConversationActorProxy>();
    actor.Tell(conversation);

    return Task.CompletedTask;
}).WithName("Conversation");
// ------------------------------------------------------------Conversation
// ------------------------------------------------------------Group
app.MapPost("/Group/Create", (CreateGroup createGroup, IActorRegistry reg) =>
{
    var actor = reg.Get<GroupActorProxy>();
    actor.Tell(new CreateGroupMessage { GroupId = createGroup.Name, Sender= createGroup.Sender , Message = string.Empty});
    
    return Task.CompletedTask;
}).WithName("CreateGroup");

app.MapPost("/Group/{groupName}/Join", ([FromQuery] string groupName, JoinGroup joinGroup, IActorRegistry reg) =>
{
    var actor = reg.Get<GroupActorProxy>();
    actor.Tell(new JoinGroupMessage { GroupId = groupName, Sender = joinGroup.Sender, Message = string.Empty });
    
    return Task.CompletedTask;
}).WithName("JoinGroup");

app.MapPost("/Group/{groupName}", ([FromQuery] string groupName, ConversationMessage conversation, IActorRegistry reg) =>
{

    return Task.CompletedTask;
}).WithName("SentToGroup");
// ------------------------------------------------------------Group
// ------------------------------------------------------------Channel
app.MapPost("/Channel/Create", (ConversationMessage conversation, IActorRegistry reg) =>
{

    return Task.CompletedTask;
}).WithName("CreateChannel");

app.MapPost("/Channel/{channelName}/Join", ([FromQuery] string channelName, ConversationMessage conversation, IActorRegistry reg) =>
{

    return Task.CompletedTask;
}).WithName("JoinChannel");

app.MapPost("/Channel/{channelName}", ([FromQuery] string channelName, ConversationMessage conversation, IActorRegistry reg) =>
{

    return Task.CompletedTask;
}).WithName("SentToChannel");
// ------------------------------------------------------------Channel


app.Run();