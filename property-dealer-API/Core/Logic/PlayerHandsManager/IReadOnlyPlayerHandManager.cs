using property_dealer_API.Models.Cards;
using property_dealer_API.Models.Enums.Cards;

namespace property_dealer_API.Core.Logic.PlayerHandsManager
{
    public interface IReadOnlyPlayerHandManager
    {
        List<Card> GetPlayerHand(string userId);
        Card GetCardFromPlayerHandById(string userId, string cardId);
        (Card card, PropertyCardColoursEnum propertyGroup) GetCardInTableHand(string userId, string cardId);
        Card GetCardInMoneyHand(string userId, string cardId);
        (int handGroup, PropertyCardColoursEnum? propertyGroup) FindCardInWhichHand(string userId, string cardId);
        List<Card> GetPropertyGroupInPlayerTableHand(string userId, PropertyCardColoursEnum targetColor);
        void ProcessAllTableHandsSafely(Action<string, IReadOnlyDictionary<PropertyCardColoursEnum, IReadOnlyList<Card>>, IReadOnlyList<Card>> processAction);
    }
}