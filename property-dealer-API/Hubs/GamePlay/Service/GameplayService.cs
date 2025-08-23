using property_dealer_API.Application.DTOs.Responses;
using property_dealer_API.Application.Enums;
using property_dealer_API.Application.Exceptions;
using property_dealer_API.Application.MethodReturns;
using property_dealer_API.Application.Services.CardManagement;
using property_dealer_API.Application.Services.GameManagement;
using property_dealer_API.Core;
using property_dealer_API.Core.Entities;
using property_dealer_API.Models.Enums.Cards;

namespace property_dealer_API.Hubs.GamePlay.Service
{
    public class GameplayService : IGameplayService
    {
        private readonly IGameManagerService _gameManagerService;

        public GameplayService(
            IGameManagerService gameManagerService,
            ICardFactoryService cardManagerService
        )
        {
            this._gameManagerService = gameManagerService;
        }

        public bool DoesPlayerExist(string userId, string gameRoomId)
        {
            try
            {
                this._gameManagerService.GetGameDetails(gameRoomId)
                    .PublicPlayerManager.GetPlayerByUserId(userId);
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
                this._gameManagerService.GetGameDetails(gameRoomId);
                return true;
            }
            catch (GameNotFoundException)
            {
                return false;
            }
        }

        public List<Player> GetAllPlayers(string gameRoomId)
        {
            return this
                ._gameManagerService.GetGameDetails(gameRoomId)
                .PublicPlayerManager.GetAllPlayers();
        }

        public Player GetPlayerByUserId(string gameRoomId, string userId)
        {
            return this
                ._gameManagerService.GetGameDetails(gameRoomId)
                .PublicPlayerManager.GetPlayerByUserId(userId);
        }

        public string RemovePlayerFromGame(string gameRoomId, string userId)
        {
            var gameInstance = this._gameManagerService.GetGameDetails(gameRoomId);

            var removalStatus = gameInstance.RemovePlayerByUserId(userId);

            //// If no players are left (response by game instance), remove the game from game manager.
            //if (removalStatus?.Response == RemovePlayerResponse.NoPlayersRemaining)
            //{
            //    this._gameManagerService.RemoveGame(gameRoomId);
            //}

            return removalStatus?.PlayerName ?? "Server: Cannot find player";
        }

        public List<CardDto> GetPlayerHand(string gameRoomId, string userId)
        {
            var cards = this
                ._gameManagerService.GetGameDetails(gameRoomId)
                .PublicPlayerHandManager.GetPlayerHand(userId);

            return [.. cards.Select(card => card.ToDto())];
        }

        public List<TableHands> GetAllPlayerTableHands(string gameRoomId)
        {
            return this._gameManagerService.GetGameDetails(gameRoomId).GetAllPlayerHands();
        }

        public TurnResult PlayCard(
            string gameRoomId,
            string userId,
            string cardId,
            CardDestinationEnum cardDestination,
            PropertyCardColoursEnum? cardColorDestinationEnum
        )
        {
            var gameInstance = this._gameManagerService.GetGameDetails(gameRoomId);
            return gameInstance.PlayTurn(userId, cardId, cardDestination, cardColorDestinationEnum);
        }

        public CardDto GetCardByIdFromPlayerHand(string gameRoomId, string userId, string cardId)
        {
            var gameInstance = this._gameManagerService.GetGameDetails(gameRoomId);
            var card = gameInstance.GetPlayerHandByCardId(userId, cardId);

            return card;
        }

        public CardDto? GetMostRecentDiscardedCard(string gameRoomId)
        {
            var gameInstance = this._gameManagerService.GetGameDetails(gameRoomId);
            var card = gameInstance.GetMostRecentDiscardedCard();

            return card;
        }

        public Player GetCurrentPlayerTurn(string gameRoomId)
        {
            var gameInstance = this._gameManagerService.GetGameDetails(gameRoomId);
            return gameInstance.GetCurrentPlayerTurn();
        }

        public TurnResult SendActionResponse(
            string gameRoomId,
            string userId,
            ActionContext actionContext
        )
        {
            var gameInstance = this._gameManagerService.GetGameDetails(gameRoomId);

            return gameInstance.RegisterActionResponse(userId, actionContext);
        }

        public void SendDebugCommand(
            string gameRoomId,
            DebugOptionsEnum debugCommand,
            DebugContext debugContext
        )
        {
            var gameInstance = this._gameManagerService.GetGameDetails(gameRoomId);

            gameInstance.ExecuteDebugCommand(debugCommand, debugContext);
        }

        public Player? CheckIfAnyPlayersWon(string gameRoomId)
        {
            var gameInstance = this._gameManagerService.GetGameDetails(gameRoomId);
            return gameInstance.CheckIfAnyPlayersWon();
        }

        public TurnResult EndPlayerTurnEarlier(string gameRoomId, string userId)
        {
            var gameInstance = this._gameManagerService.GetGameDetails(gameRoomId);

            return gameInstance.EndPlayerTurnEarlier(userId);
        }

        public void DisposeExtraCards(
            string gameRoomId,
            string userId,
            List<string> cardIdsToDispose
        )
        {
            var gameInstance = this._gameManagerService.GetGameDetails(gameRoomId);

            gameInstance.DisposeExtraCards(userId, cardIdsToDispose);
        }

        public void MovePropertySetModifierBetweenSets(
            string gameRoomId,
            string userId,
            string selectedCardId,
            PropertyCardColoursEnum destinationColor
        )
        {
            var gameInstance = this._gameManagerService.GetGameDetails(gameRoomId);

            gameInstance.MovePropertySetModifierBetweenSets(
                userId,
                selectedCardId,
                destinationColor
            );
        }

        public List<Player> GetPendingActionPlayers(string gameRoomId)
        {
            var gameInstance = this._gameManagerService.GetGameDetails(gameRoomId);
            return gameInstance.GetPendingActionPlayers();
        }

        public TurnResult GetCurrentPendingAction(string gameRoomId)
        {
            var gameInstance = this._gameManagerService.GetGameDetails(gameRoomId);
            return gameInstance.GetCurrentPendingAction();
        }
    }
}
