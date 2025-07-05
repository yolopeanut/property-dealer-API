using Microsoft.AspNetCore.SignalR;
using property_dealer_API.Application.DTOs.Responses;
using property_dealer_API.Application.Exceptions;
using property_dealer_API.Core.Entities;
using property_dealer_API.Models.Enums;

namespace property_dealer_API.Hubs.GameLobby
{

    public class GameLobbyHub : Hub<IGameLobbyHubClient>, IGameLobbyHubServer
    {
        private readonly IGameLobbyHubService _gameLobbyHubService;

        public GameLobbyHub(IGameLobbyHubService gameLobbyHubService)
        {
            this._gameLobbyHubService = gameLobbyHubService;
        }

        public async Task GetAllGameLobbySummaries()
        {
            var summaries = this._gameLobbyHubService.GetGameListSummary();

            await Clients.Caller.GetAllLobbySummary(summaries);
        }

        public async Task CreateGameRoom(string userId, string playerName, string roomName, GameConfig createGameConfig)
        {
            try
            {
                // Create Room
                Console.WriteLine("About to call CreateRoom");
                var roomIdCreated = this._gameLobbyHubService.CreateRoom(userId, playerName, roomName, createGameConfig);

                Console.WriteLine($"=== ROOM CREATED ===");
                Console.WriteLine($"Room ID: '{roomIdCreated}'");
                Console.WriteLine($"User ID: '{userId}'");
                Console.WriteLine($"Player Name: '{playerName}'");

                await Clients.Caller.CreateGameRoomId(roomIdCreated);

                Console.WriteLine("CreateRoom completed");

                // Broadcast summaries
                Console.WriteLine("About to get summaries");
                var summaries = this._gameLobbyHubService.GetGameListSummary();
                Console.WriteLine($"Got {summaries.Count()} summaries");
                await Clients.All.GetAllLobbySummary(summaries);

                Console.WriteLine("Broadcast completed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== EXCEPTION IN CreateGameRoom ===");
                Console.WriteLine($"Message: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                Console.WriteLine($"InnerException: {ex.InnerException?.Message}");
                throw; // Important: re-throw so client gets the error
            }
        }

        public async Task JoinGameRoom(string userId, string playerName, string gameRoomId)
        {
            try
            {
                // Joining room
                var response = this._gameLobbyHubService.JoinRoom(gameRoomId, userId, playerName);
                await Clients.Caller.JoinGameRoomStatus(new JoinGameResponse(response, gameRoomId));

                // Broadcasting to all lobby status
                var summaries = this._gameLobbyHubService.GetGameListSummary();
                await Clients.All.GetAllLobbySummary(summaries);
            }
            catch (GameNotFoundException)
            {
                await Clients.Caller.JoinGameRoomStatus(new JoinGameResponse(JoinGameResponseEnum.FailedToJoin, gameRoomId));
            }
        }


    }
}
