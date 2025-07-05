
using property_dealer_API.Application.Enums;
using property_dealer_API.Application.Exceptions;
using property_dealer_API.Application.Services.CardManagement;
using property_dealer_API.Application.Services.GameManagement;
using property_dealer_API.Core.Entities;

namespace property_dealer_API.Hubs.GamePlay.Service
{
    public class GameplayService : IGameplayService
    {
        private readonly IGameManagerService _gameManagerService;
        private readonly ICardFactoryService _cardManagerService;
        public GameplayService(IGameManagerService gameManagerService, ICardFactoryService cardManagerService)
        {
            this._gameManagerService = gameManagerService;
            this._cardManagerService = cardManagerService;
        }

        public bool DoesPlayerExist(string userId, string gameRoomId)
        {
            try
            {
                _gameManagerService.GetGameDetails(gameRoomId).GetPlayerByUserId(userId);
                return true;
            }
            catch (GameNotFoundException)
            {
                return false;
            }
            catch (PlayerNotFoundException)
            {
                return false;
            }
        }

        public bool DoesRoomExist(string gameRoomId)
        {
            try
            {
                _gameManagerService.GetGameDetails(gameRoomId);
                return true;
            }
            catch (GameNotFoundException)
            {
                return false;
            }
        }

        public List<Player> GetAllPlayers(string gameRoomId)
        {
            return _gameManagerService.GetGameDetails(gameRoomId).GetPlayers();
        }

        public Player GetPlayerByUserId(string gameRoomId, string userId)
        {
            return _gameManagerService.GetGameDetails(gameRoomId).GetPlayerByUserId(userId);
        }

        public string RemovePlayerFromGame(string gameRoomId, string userId)
        {
            var gameInstance = this._gameManagerService.GetGameDetails(gameRoomId);

            var removalStatus = gameInstance.RemovePlayerByUserId(userId);

            // If no players are left (response by game instance), remove the game from game manager.
            if (removalStatus?.Response == RemovePlayerResponse.NoPlayersRemaining)
            {
                this._gameManagerService.RemoveGame(gameRoomId);
            }

            return removalStatus?.PlayerName ?? "Server: Cannot find player";
        }
    }
}
