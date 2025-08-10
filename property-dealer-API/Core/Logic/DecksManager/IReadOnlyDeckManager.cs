using property_dealer_API.Models.Cards;

namespace property_dealer_API.Core.Logic.DecksManager
{
    public interface IReadOnlyDeckManager
    {
        List<Card> ViewAllCardsInDeck();
        Card? GetMostRecentDiscardedCard();
        Card GetDiscardedCardById(string cardId);
    }
}