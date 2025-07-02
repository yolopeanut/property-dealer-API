using property_dealer_API.Core.Entities;
using TypedSignalR.Client;

[Hub]
public interface IGameLobbyHubServer
{
    Task GetAllGameLobbySummaries();
    Task CreateGameRoom(string userId, string playerName, string roomName, GameConfig createGameConfig);
    Task JoinGameRoom(string userId, string playerName, string gameRoomId);
}