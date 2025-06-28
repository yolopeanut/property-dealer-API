using Microsoft.AspNetCore.Razor.Runtime.TagHelpers;
using property_dealer_API.Models;
using property_dealer_API.Models.DTOs;
using property_dealer_API.SharedServices;

namespace property_dealer_API.Hubs.GameWaitingRoom.Service
{
    public class WaitingRoomService : IWaitingRoomService
    {
        IGameManagerService _gameManagerService;

        public WaitingRoomService(IGameManagerService gameManagerService)
        {
            this._gameManagerService = gameManagerService;
        }

        public List<Player> GetAllPlayers(string gameRoomLobbyId)
        {
            return this._gameManagerService.GetAllPlayers(gameRoomLobbyId);
        }

        public GameConfig? GetRoomConfig(string gameRoomLobbyId)
        {
            return this._gameManagerService.GetGameRoomConfig(gameRoomLobbyId);
        }
    }
}
