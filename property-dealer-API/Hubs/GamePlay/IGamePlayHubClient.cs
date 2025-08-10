
using property_dealer_API.Application.DTOs.Responses;
using property_dealer_API.Core;
using property_dealer_API.Core.Entities;
using TypedSignalR.Client;

namespace property_dealer_API.Hubs.GamePlay
{

    [Receiver]
    public interface IGamePlayHubClient
    {
        Task AllGameRoomPlayerList(object allPlayers);
        Task ErrorMsg(string message);
        Task PlayerLeft(string playerName);
        Task AllTableHands(List<TableHands> allTableHands);
        Task PlayerHand(List<CardDto> playerHand);
        Task LatestDiscardPileCard(CardDto discardedCard);
        Task OpenCommandDialog(ActionContext actionContext);
        Task CurrentPlayerTurn(Player player);
        Task PlayerWon(Player? player);
    }
}
