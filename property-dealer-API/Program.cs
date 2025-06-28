using property_dealer_API.Hubs.GameLobby;
using property_dealer_API.Hubs.GameLobby.Service;
using property_dealer_API.Hubs.GameWaitingRoom;
using property_dealer_API.Hubs.GameWaitingRoom.Service;
using property_dealer_API.SharedServices;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();
builder.Services.AddSingleton<IGameLobbyHubService, GameLobbyHubService>();
builder.Services.AddSingleton<IGameManagerService, GameManagerService>();
builder.Services.AddSingleton<IWaitingRoomService, WaitingRoomService>();

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
}

app.UseCors("property-dealer-policy");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapHub<GameLobbyHub>("/gamelobby");
app.MapHub<WaitingRoomHub>("/waiting-room");

app.Run();
