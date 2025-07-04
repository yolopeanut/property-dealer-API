using property_dealer_API.Application.Services.CardManagement;
using property_dealer_API.Application.Services.GameManagement;
using property_dealer_API.Core.Entities;
using property_dealer_API.Application.Enums;
using property_dealer_API.Application.DTOs.Responses;

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

        public List<Player>? GetAllPlayers(string gameRoomId)
        {
            return _gameManagerService.GetGameDetails(gameRoomId)?.GetPlayers();
        }

        public Player? GetPlayerByUserId(string gameRoomId, string userId)
        {
            return _gameManagerService.GetGameDetails(gameRoomId)?.GetPlayerByUserId(userId);
        }

        public string RemovePlayerFromGame(string gameRoomId, string userId)
        {
            var gameInstance = this._gameManagerService.GetGameDetails(gameRoomId);
            if (gameInstance == null)
            {
                return "Server: Game not found";
            }

            var removalStatus = gameInstance?.RemovePlayerByUserId(userId);

            // If no players are left (response by game instance), remove the game from game manager.
            if (removalStatus?.Response == RemovePlayerResponse.NoPlayersRemaining)
            {
                this._gameManagerService.RemoveGame(gameRoomId);
            }

            return removalStatus?.PlayerName ?? "Server: Cannot find player";
        }

        public GameConfig? GetRoomConfig(string gameRoomId)
        {
            return _gameManagerService.GetGameDetails(gameRoomId)?.Config;
        }

        public bool DoesRoomExist(string gameRoomLobbyId)
        {
            return _gameManagerService.GetGameDetails(gameRoomLobbyId) != null;
        }

        public bool DoesPlayerExist(string userId, string gameRoomLobbyId)
        {
            return _gameManagerService.GetGameDetails(gameRoomLobbyId)?.GetPlayerByUserId(userId) != null;
        }

        public void StartGame(string gameRoomId)
        {
            var gameInstance = this._gameManagerService.GetGameDetails(gameRoomId);

            if (gameInstance == null) { return; }

            // Call game instance to start game and pass over the initial deck
            var initialDeck = this._cardFactoryService.StartCardFactory();
            gameInstance.StartGame(initialDeck);
        }

        public IEnumerable<GameListSummaryResponse> GetAllExistingRoomIds()
        {
            return _gameManagerService.GetGameListSummary(); // We'll need to add this to GameManagerService too
        }


    }
}