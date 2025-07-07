using property_dealer_API.Application.DTOs.Responses;
using property_dealer_API.Models.Cards;
using property_dealer_API.Models.Enums.Cards;

namespace property_dealer_API.Core.Logic.PlayerHandsManager
{
    public interface IReadOnlyPlayerHandManager
    {
        List<Card> GetPlayerHand(string userId);
        void ProcessAllTableHandsSafely(Action<string, IReadOnlyDictionary<PropertyCardColoursEnum, IReadOnlyList<Card>>, IReadOnlyList<Card>> processAction);
    }
}
