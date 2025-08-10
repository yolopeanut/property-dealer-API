using property_dealer_API.Models.Cards;
using property_dealer_API.Models.Enums.Cards;

namespace property_dealer_API.Core.Logic.PlayerHandsManager
{
    public interface IPlayerHandManager : IReadOnlyPlayerHandManager
    {
        void AddPlayerHand(string userId);
        void RemovePlayerByUserId(string userId);
        Card RemoveFromPlayerHand(string userId, string cardId);
        void AssignPlayerHand(string userId, List<Card> cards);
        void AddCardToPlayerHand(string userId, Card card);
        void AddCardToPlayerMoneyHand(string userId, Card card);
        void AddCardToPlayerTableHand(string userId, Card card, PropertyCardColoursEnum targetColor);
        Card RemoveCardFromPlayerMoneyHand(string userId, string cardId);
        Card RemoveCardFromPlayerTableHand(string userId, string cardId);
        (PropertyCardColoursEnum propertyGroup, List<Card> cardsInPropertyGroup) RemovePropertyGroupFromPlayerTableHand(string userId, PropertyCardColoursEnum targetColor);
    }
}