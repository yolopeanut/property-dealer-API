using property_dealer_API.Application.DTOs.Responses;
using property_dealer_API.Application.Enums;
using property_dealer_API.Application.Exceptions;
using property_dealer_API.Application.Services.CardManagement;
using property_dealer_API.Application.Services.GameManagement;
using property_dealer_API.Core.Entities;

namespace property_dealer_API.Hubs.GameWaitingRoom.Service
{
    public class WaitingRoomService : IWaitingRoomService
    {
        private readonly IGameManagerService _gameManagerService;
        private readonly ICardFactoryService _cardFactoryService;

        public WaitingRoomService(IGameManagerService gameManagerService, ICardFactoryService cardFactoryService)
        {
            this._gameManagerService = gameManagerService;
            this._cardFactoryService = cardFactoryService;
        }

        public List<Player> GetAllPlayers(string gameRoomId)
        {
            return this._gameManagerService.GetGameDetails(gameRoomId).PublicPlayerManager.GetAllPlayers();
        }

        public Player GetPlayerByUserId(string gameRoomId, string userId)
        {
            return this._gameManagerService.GetGameDetails(gameRoomId).PublicPlayerManager.GetPlayerByUserId(userId);
        }

        public string RemovePlayerFromGame(string gameRoomId, string userId)
        {
            var gameInstance = this._gameManagerService.GetGameDetails(gameRoomId);

            var removalStatus = gameInstance.RemovePlayerByUserId(userId);

            // If no players are left (response by game instance), remove the game from game manager.
            if (removalStatus.Response == RemovePlayerResponse.NoPlayersRemaining)
            {
                this._gameManagerService.RemoveGame(gameRoomId);
            }

            return removalStatus.PlayerName;
        }

        public GameConfig GetRoomConfig(string gameRoomId)
        {
            return this._gameManagerService.GetGameDetails(gameRoomId).Config;
        }

        public bool DoesRoomExist(string gameRoomId)
        {
            try
            {
                this._gameManagerService.GetGameDetails(gameRoomId);
                return true;
            }
            catch (GameNotFoundException)
            {
                return false;
            }
        }

        public bool DoesPlayerExist(string userId, string gameRoomId)
        {
            try
            {
                this._gameManagerService.GetGameDetails(gameRoomId).PublicPlayerManager.GetPlayerByUserId(userId);
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

        public void StartGame(string gameRoomId)
        {
            var gameInstance = this._gameManagerService.GetGameDetails(gameRoomId);

            // Call game instance to start game and pass over the initial deck
            var initialDeck = this._cardFactoryService.StartCardFactory();
            gameInstance.StartGame(initialDeck);
        }

        public IEnumerable<GameListSummaryResponse> GetAllExistingRoomIds()
        {
            return this._gameManagerService.GetGameListSummary(); // We'll need to add this to GameManagerService too
        }


    }
}