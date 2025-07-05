using property_dealer_API.Application.DTOs.Responses;
using property_dealer_API.Models.Cards;

namespace property_dealer_API.Core.Logic.PlayerHandsManager
{
    public interface IReadOnlyPlayerHandManager
    {
        List<Card> GetPlayerHand(string userId);
        List<TableHands> GetAllTableHands();
    }
}
