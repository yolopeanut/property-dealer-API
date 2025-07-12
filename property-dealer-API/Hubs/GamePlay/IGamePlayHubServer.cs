using property_dealer_API.Application.DTOs.Requests;
using property_dealer_API.Application.Enums;
using property_dealer_API.Models.Enums.Cards;
using TypedSignalR.Client;

namespace property_dealer_API.Hubs.GameLobby
{
    [Hub]
    public interface IGamePlayHubServer
    {
        Task LeaveGameRoom(string gameRoomId, string userId);
        Task GetAllPlayerList(string gameRoomId);
        Task GetPlayerHand(string gameRoomId, string userId);
        Task GetAllTableCard(string gameRoomId);
        Task PlayCard(string gameRoomId, string userId, string cardId, CardDestinationEnum cardDestination, PropertyCardColoursEnum? cardColorDestinationEnum);
        Task GetLatestDiscardPileCard(string gameRoomId);
    }
}
