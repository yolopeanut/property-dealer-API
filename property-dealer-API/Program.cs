using Microsoft.AspNetCore.Server.Kestrel.Core;
using property_dealer_API.Application.Services.CardManagement;
using property_dealer_API.Application.Services.GameManagement;
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

builder.Services.AddCors((o) =>
{
    o.AddPolicy("property-dealer-policy",
        policy => policy
            .WithOrigins("http://localhost:4200", "https://localhost:4200")
            .AllowAnyHeader()
            .AllowCredentials()
    );
});


var app = builder.Build();

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

app.Run();
