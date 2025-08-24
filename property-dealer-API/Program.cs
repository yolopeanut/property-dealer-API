using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using property_dealer_API.Application.Services.CardManagement;
using property_dealer_API.Application.Services.GameManagement;
using property_dealer_API.Core.Factories;
using property_dealer_API.Core.Logic.ActionExecution;
using property_dealer_API.Core.Logic.ActionExecution.ActionHandlerResolvers;
using property_dealer_API.Core.Logic.ActionExecution.ActionHandlers;
using property_dealer_API.Core.Logic.DebuggingManager;
using property_dealer_API.Core.Logic.DecksManager;
using property_dealer_API.Core.Logic.DialogsManager;
using property_dealer_API.Core.Logic.GameRulesManager;
using property_dealer_API.Core.Logic.GameStateMapper;
using property_dealer_API.Core.Logic.PendingActionsManager;
using property_dealer_API.Core.Logic.PlayerHandsManager;
using property_dealer_API.Core.Logic.PlayersManager;
using property_dealer_API.Core.Logic.TurnExecutionsManager;
using property_dealer_API.Core.Logic.TurnManager;
using property_dealer_API.Hubs.GameLobby;
using property_dealer_API.Hubs.GameLobby.Service;
using property_dealer_API.Hubs.GamePlay;
using property_dealer_API.Hubs.GamePlay.Service;
using property_dealer_API.Hubs.GameWaitingRoom;
using property_dealer_API.Hubs.GameWaitingRoom.Service;
using TypedSignalR.Client.DevTools;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.Configure<IISServerOptions>(options =>
{
    options.AllowSynchronousIO = true;
});
builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.AllowSynchronousIO = true;
});

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();

//Hub Services (all hubs have services)
builder.Services.AddSingleton<IGameLobbyHubService, GameLobbyHubService>();
builder.Services.AddSingleton<IWaitingRoomService, WaitingRoomService>();
builder.Services.AddSingleton<IGameplayService, GameplayService>();

// Application level service
builder.Services.AddSingleton<IGameManagerService, GameManagerService>();

//Manager services which are stateless
builder.Services.AddSingleton<ICardFactoryService, CardFactoryService>();
builder.Services.AddSingleton<IGameDetailsFactory, GameDetailsFactory>();

// Scoped manager services for gameplay
builder.Services.AddScoped<IDeckManager, DeckManager>();
builder.Services.AddScoped<IReadOnlyDeckManager>(provider =>
    provider.GetRequiredService<IDeckManager>()
);

builder.Services.AddScoped<IPlayerManager, PlayerManager>();
builder.Services.AddScoped<IReadOnlyPlayerManager>(provider =>
    provider.GetRequiredService<IPlayerManager>()
);

builder.Services.AddScoped<IPlayerHandManager, PlayersHandManager>();
builder.Services.AddScoped<IReadOnlyPlayerHandManager>(provider =>
    provider.GetRequiredService<IPlayerHandManager>()
);

builder.Services.AddScoped<IGameStateMapper, GameStateMapper>();
builder.Services.AddScoped<IGameRuleManager, GameRuleManager>();
builder.Services.AddScoped<ITurnManager, TurnManager>();
builder.Services.AddScoped<ITurnExecutionManager, TurnExecutionManager>();
builder.Services.AddScoped<IPendingActionManager, PendingActionManager>();
builder.Services.AddScoped<IDialogManager, DialogManager>();
builder.Services.AddScoped<IActionExecutor, ActionExecutor>();
builder.Services.AddScoped<IActionExecutionManager, ActionExecutionManager>();
builder.Services.AddScoped<IDebugManager, DebugManager>();

builder.Services.AddScoped<IActionHandlerResolver, ActionHandlerResolver>();

builder.Services.AddTransient<HostileTakeoverHandler>();
builder.Services.AddTransient<ForcedTradeHandler>();
builder.Services.AddTransient<PirateRaidHandler>();
builder.Services.AddTransient<BountyHunterHandler>();
builder.Services.AddTransient<TradeDividendHandler>();
builder.Services.AddTransient<ExploreNewSectorHandler>();

builder.Services.AddTransient<SpaceStationHandler>();
builder.Services.AddTransient<StarbaseHandler>();

builder.Services.AddTransient<TradeEmbargoHandler>();

//builder.Services.AddTransient<ShieldsUpHandler>();
builder.Services.AddTransient<SystemWildCardHandler>();
builder.Services.AddTransient<TributeCardHandler>();
builder.Services.AddTransient<WildCardTributeHandler>();

builder.Services.AddCors(
    (o) =>
    {
        o.AddPolicy(
            "property-dealer-policy",
            policy =>
                policy
                    .WithOrigins(
                        "http://localhost:4200",
                        "https://localhost:4200",
                        "http://192.168.192.19:4200",
                        "https://galaxy-monopolizer.brandon-tan.work"
                    )
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials()
        );
    }
);

var app = builder.Build();

app.UseForwardedHeaders(
    new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    }
);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseSignalRHubSpecification();
    app.UseSignalRHubDevelopmentUI();
}

app.UseCors("property-dealer-policy");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapHub<GameLobbyHub>("/gamelobby");
app.MapHub<WaitingRoomHub>("/waiting-room");
app.MapHub<GamePlayHub>("/gameplay");

app.Urls.Add("http://*:5210");

//app.Urls.Add("http://*:80");
//app.Urls.Add("https://*:7200");

app.Run();

public partial class Program { }
