using property_dealer_API.Application.DTOs.Responses;
using property_dealer_API.Application.Enums;
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
        ActionContext? PlayCard(string gameRoomId, string userId, string cardId, CardDestinationEnum cardDestination, PropertyCardColoursEnum? cardColorDestinationEnum);
        string RemovePlayerFromGame(string gameRoomId, string userId);
        CardDto GetCardByIdFromPlayerHand(string gameRoomId, string userId, string cardId);
        CardDto GetMostRecentDiscardedCard(string gameRoomId);
        Player GetCurrentPlayerTurn(string gameRoomId);
        List<ActionContext>? SendActionResponse(string gameRoomId, string userId, ActionContext actionContext);
        void SendDebugCommand(string gameRoomId, string userId, DebugOptionsEnum debugOption);
    }
}
