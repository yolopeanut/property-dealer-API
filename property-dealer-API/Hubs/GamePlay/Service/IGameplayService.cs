using property_dealer_API.Application.DTOs.Responses;
using property_dealer_API.Application.Enums;
using property_dealer_API.Application.MethodReturns;
using property_dealer_API.Core;
using property_dealer_API.Core.Entities;
using property_dealer_API.Models.Enums.Cards;

namespace property_dealer_API.Hubs.GamePlay.Service
{
    public interface IGameplayService
    {
        bool DoesPlayerExist(string userId, string gameRoomId);
        bool DoesRoomExist(string gameRoomId);
        List<TableHands> GetAllPlayerTableHands(string gameRoomId);
        List<Player> GetAllPlayers(string gameRoomId);
        Player GetPlayerByUserId(string gameRoomId, string userId);
        List<CardDto> GetPlayerHand(string gameRoomId, string userId);
        TurnResult PlayCard(
            string gameRoomId,
            string userId,
            string cardId,
            CardDestinationEnum cardDestination,
            PropertyCardColoursEnum? cardColorDestinationEnum
        );
        string RemovePlayerFromGame(string gameRoomId, string userId);
        CardDto GetCardByIdFromPlayerHand(string gameRoomId, string userId, string cardId);
        CardDto? GetMostRecentDiscardedCard(string gameRoomId);
        Player GetCurrentPlayerTurn(string gameRoomId);
        TurnResult SendActionResponse(
            string gameRoomId,
            string userId,
            ActionContext actionContext
        );
        void SendDebugCommand(
            string gameRoomId,
            DebugOptionsEnum debugCommand,
            DebugContext debugContext
        );
        Player? CheckIfAnyPlayersWon(string gameRoomId);
        TurnResult EndPlayerTurnEarlier(string gameRoomId, string userId);
        void DisposeExtraCards(string gameRoomId, string userId, List<string> cardIdsToDispose);
        void MovePropertySetModifierBetweenSets(
            string gameRoomId,
            string userId,
            string selectedCardId,
            PropertyCardColoursEnum destinationColor
        );
        List<Player> GetPendingActionPlayers(string gameRoomId);
        TurnResult GetCurrentPendingAction(string gameRoomId);
    }
}
