using property_dealer_API.Application.Services.GameManagement;
using property_dealer_API.Core.Entities;

namespace property_dealer_API.Hubs.GameWaitingRoom.Service
{
    public class WaitingRoomService : IWaitingRoomService
    {
        IGameManagerService _gameManagerService;

        public WaitingRoomService(IGameManagerService gameManagerService)
        {
            this._gameManagerService = gameManagerService;
        }

        public List<Player>? GetAllPlayers(string gameRoomLobbyId)
        {
            var gameInstance = this._gameManagerService.GetGameDetails(gameRoomLobbyId);

            if (gameInstance != null) { return gameInstance.GetPlayers(); }

            return null;
        }

        public GameConfig? GetRoomConfig(string gameRoomLobbyId)
        {
            var gameInstance = this._gameManagerService.GetGameDetails(gameRoomLobbyId);

            if (gameInstance != null) { return gameInstance.Config; }

            return null;
        }
    }
}