using Microsoft.AspNetCore.SignalR;
using property_dealer_API.Models.DTOs;
using property_dealer_API.Models.Enums;
using System.Linq.Expressions;

namespace property_dealer_API.Hubs.GameLobby
{
    public class GameLobbyHub : Hub<IGameLobbyHub>
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

        public async Task CreateGameRoom(string playerName, string roomName, CreateGameConfigDTO createGameConfig)
        {
            try
            {
                // Create Room
                Console.WriteLine("About to call CreateRoom");
                var roomIdCreated = this._gameLobbyHubService.CreateRoom(Context.ConnectionId, playerName, roomName, createGameConfig);
                await Clients.Caller.CreateGameRoomId(roomIdCreated);

                //TODO add response status for create room status
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

        public async Task JoinGameRoom(string gameRoomId, string playerName)
        {
            // Joining room
            var response = this._gameLobbyHubService.JoinRoom(gameRoomId, Context.ConnectionId, playerName);
            await Clients.Caller.JoinGameRoomStatus(new JoinGameResponseDTO(response, gameRoomId));

            // Broadcasting to all lobby status
            var summaries = this._gameLobbyHubService.GetGameListSummary();
            await Clients.All.GetAllLobbySummary(summaries);
        }


    }
}
