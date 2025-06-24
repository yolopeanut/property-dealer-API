using property_dealer_API.Models;
using property_dealer_API.Models.DTOs;
using property_dealer_API.Models.Enums;
using property_dealer_API.SharedServices;

namespace property_dealer_API.Hubs.GameLobby
{
    public class GameLobbyHubService : IGameLobbyHubService
    {
        private readonly IGameManagerService _gameManagerService;

        public GameLobbyHubService(IGameManagerService gameManagerService)
        {
            this._gameManagerService = gameManagerService;
        }

        // Geting game list summary
        public IEnumerable<GameListSummaryDTO> GetGameListSummary()
        {
            return this._gameManagerService.GetGameListSummary();
        }

        // Creating Room
        public string CreateRoom(string connectionId, string playerName, string roomName, CreateGameConfigDTO config)
        {
            // Ensure unique id
            var newRoomId = Guid.NewGuid().ToString();
            while (this._gameManagerService.IsGameIdExisting(newRoomId))
            {
                newRoomId = Guid.NewGuid().ToString();
            }

            var newPlayer = new Player { ConnectionId = connectionId, PlayerName = playerName };

            var newGameDetails = new GameDetails(newRoomId, roomName, config, newPlayer);
            this._gameManagerService.AddNewGameToDict(newRoomId, newGameDetails);
            this._gameManagerService.AddPlayerToDict(newRoomId, newPlayer);

            return newRoomId;
        }

        public JoinGameResponseEnum JoinRoom(string gameRoomId, string connectionId, string playerName)
        {
            var player = new Player { ConnectionId = connectionId, PlayerName = playerName };
            return this._gameManagerService.AddPlayerToDict(gameRoomId, player);
        }
    }
}
