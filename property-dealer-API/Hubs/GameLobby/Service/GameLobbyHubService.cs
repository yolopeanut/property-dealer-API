using property_dealer_API.Models;
using property_dealer_API.Models.DTOs;
using property_dealer_API.Models.Enums;
using property_dealer_API.SharedServices;

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
        public IEnumerable<GameListSummaryDTO> GetGameListSummary()
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

            var newGameDetails = new GameDetails(newRoomId, roomName, config, newPlayer);
            _gameManagerService.AddNewGameToDict(newRoomId, newGameDetails);
            _gameManagerService.AddPlayerToDict(newRoomId, newPlayer);

            return newRoomId;
        }

        public JoinGameResponseEnum JoinRoom(string gameRoomId, string connectionId, string userId, string playerName)
        {
            var player = new Player { UserId = userId, PlayerName = playerName };
            return _gameManagerService.AddPlayerToDict(gameRoomId, player);
        }
    }
}
