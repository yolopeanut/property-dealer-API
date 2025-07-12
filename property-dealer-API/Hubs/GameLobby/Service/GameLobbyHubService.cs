
using property_dealer_API.Application.DTOs.Responses;
using property_dealer_API.Application.Services.GameManagement;
using property_dealer_API.Core;
using property_dealer_API.Core.Entities;
using property_dealer_API.Models.Enums;

namespace property_dealer_API.Hubs.GameLobby.Service
{
    public class GameLobbyHubService : IGameLobbyHubService
    {
        private readonly IGameManagerService _gameManagerService;

        public GameLobbyHubService(IGameManagerService gameManagerService)
        {
            _gameManagerService = gameManagerService;
        }

        // Geting game list summary
        public IEnumerable<GameListSummaryResponse> GetGameListSummary()
        {
            return _gameManagerService.GetGameListSummary();
        }

        // Creating Room
        public string CreateRoom(string userId, string playerName, string roomName, GameConfig config)
        {
            // Ensure unique id
            var newRoomId = Guid.NewGuid().ToString();
            while (_gameManagerService.IsGameIdExisting(newRoomId))
            {
                newRoomId = Guid.NewGuid().ToString();
            }

            var newPlayer = new Player { UserId = userId, PlayerName = playerName };

            var newGameDetails = new GameDetails(newRoomId, roomName, config);
            newGameDetails.AddPlayer(newPlayer);
            _gameManagerService.AddNewGameToDict(newRoomId, newGameDetails);

            return newRoomId;
        }

        public JoinGameResponseEnum JoinRoom(string gameRoomId, string userId, string playerName)
        {
            var player = new Player { UserId = userId, PlayerName = playerName };
            var gameInstance = this._gameManagerService.GetGameDetails(gameRoomId);

            var response = gameInstance.AddPlayer(player);
            return response;
        }
    }
}
