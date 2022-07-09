using Akka.Actor;
using Akka.Cluster.Hosting;
using Akka.Hosting;
using Akka.Remote.Hosting;
using Microsoft.AspNetCore.Mvc;
using Petabridge.Cmd.Cluster;
using Petabridge.Cmd.Cluster.Sharding;
using Petabridge.Cmd.Host;
using Petabridge.Cmd.Remote;
using SocialNetwork.Api.Actors;
using SocialNetwork.Api.Messages;
using SocialNetwork.Api.Models;
using SocialNetwork.Api.Options;

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
    .WithShardRegion<IConversationMessage>(_options.Conversation.ShardTypename,
                p => ConversationActor.CreateProps(),
                new ConversationMessageExtractor(_options.Conversation.MaxNumberOfShards),
                new ShardOptions())
    .WithShardRegion<IGroupIdentifier>(_options.Group.ShardTypename,
                p => GroupActor.CreateProps(),
                new GroupMessageExtractor(_options.Group.MaxNumberOfShards),
                new ShardOptions())
    .WithShardRegion<IChannelIdentifier>(_options.Channel.ShardTypename,
                p => ChannelActor.CreateProps(),
                new ChannelMessageExtractor(_options.Channel.MaxNumberOfShards),
                new ShardOptions())
    .StartActors((actorSystem, actorRegistry) =>
    {
        var userShardRegion = actorRegistry.Get<IUserId>();
        actorRegistry.Register<UserActorProxy>(actorSystem.ActorOf(Props.Create(() => new UserActorProxy(userShardRegion))));

        var conversationSharedRegion = actorRegistry.Get<IConversationMessage>();
        actorRegistry.Register<ConversationActorProxy>(actorSystem.ActorOf(Props.Create(() => new ConversationActorProxy(conversationSharedRegion, userShardRegion))));

        var groupShardRegion = actorRegistry.Get<IGroupIdentifier>();
        actorRegistry.Register<GroupActorProxy>(actorSystem.ActorOf(Props.Create(() => new GroupActorProxy(groupShardRegion, userShardRegion))));

        var channelShardRegion = actorRegistry.Get<IChannelIdentifier>();
        actorRegistry.Register<ChannelActorProxy>(actorSystem.ActorOf(Props.Create(() => new ChannelActorProxy(channelShardRegion, userShardRegion))));
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
app.MapGet("/UserStatus/{userId}", async ([FromRoute] string userId, IActorRegistry reg) =>
{
    var actor = reg.Get<UserActorProxy>();
    var response = await actor.Ask<UserStatusResponseMessage>(new UserStatusRequestMessage { UserId = userId });

    return response;
}).WithName("UserStatus");
// ------------------------------------------------------------Conversation
app.MapPost("/Conversation", (SendConversationMessage conversation, IActorRegistry reg) =>
{
    var actor = reg.Get<ConversationActorProxy>();
    actor.Tell(new ConversationMessage
    {
        Message = conversation.Message,
        UserId1 = conversation.UserId1,
        UserId2 = conversation.UserId2
    });

    return Task.CompletedTask;
}).WithName("Sent Message To Conversation");
app.MapGet("/Conversation/{conversationId}", async ([FromRoute] string conversationId, IActorRegistry reg) =>
{
    var actor = reg.Get<ConversationActorProxy>();
    var messages = await actor.Ask<List<ConversationMessage>>(new GetConversationMessage { ConversationId = conversationId });

    return messages;
}).WithName("Get Messages To Conversation");
// ------------------------------------------------------------Conversation
// ------------------------------------------------------------Group
app.MapPost("/Group/Create", (CreateGroup createGroup, IActorRegistry reg) =>
{
    var actor = reg.Get<GroupActorProxy>();
    actor.Tell(new CreateGroupMessage { GroupId = createGroup.Name, Sender = createGroup.Sender });

    return Task.CompletedTask;
}).WithName("CreateGroup");

app.MapPost("/Group/{groupName}/Join", ([FromRoute] string groupName, JoinGroup joinGroup, IActorRegistry reg) =>
{
    var actor = reg.Get<GroupActorProxy>();
    actor.Tell(new JoinGroupMessage { GroupId = groupName, Sender = joinGroup.Sender});

    return Task.CompletedTask;
}).WithName("JoinGroup");

app.MapPost("/Group/{groupName}", ([FromRoute] string groupName, MessageToGroup groupMessage, IActorRegistry reg) =>
{
    var actor = reg.Get<GroupActorProxy>();
    actor.Tell(new GroupMessage { GroupId = groupName, Sender = groupMessage.Sender, Message = groupMessage.Message });
}).WithName("SentToGroup");

app.MapGet("/Group/{groupName}", async ([FromRoute] string groupName, IActorRegistry reg) =>
{
    var actor = reg.Get<GroupActorProxy>();
    var response = await actor.Ask<GroupStatusResponse>(new GroupStatusMessage { GroupId = groupName, Sender = string.Empty });
    return response;
}).WithName("Get Group Status");
// ------------------------------------------------------------Group
// ------------------------------------------------------------Channel
app.MapPost("/Channel/Create", (CreateChannel createChannel, IActorRegistry reg) =>
{
    var actor = reg.Get<ChannelActorProxy>();
    actor.Tell(new CreateChannelMessage { ChannelId = createChannel.Name, Sender = createChannel.Sender });
}).WithName("CreateChannel");

app.MapPost("/Channel/{channelName}/Join", ([FromRoute] string channelName, JoinChannel joinChannel, IActorRegistry reg) =>
{
    var actor = reg.Get<ChannelActorProxy>();
    actor.Tell(new JoinChannelMessage { ChannelId = channelName, Sender = joinChannel.Sender });
}).WithName("JoinChannel");

app.MapPost("/Channel/{channelName}", ([FromRoute] string channelName, MessageToChannel channelMessage, IActorRegistry reg) =>
{
    var actor = reg.Get<ChannelActorProxy>();
    actor.Tell(new ChannelMessage { ChannelId = channelName, Sender = channelMessage.Sender, Message = channelMessage.Message });
}).WithName("SentToChannel");
// ------------------------------------------------------------Channel


app.Run();