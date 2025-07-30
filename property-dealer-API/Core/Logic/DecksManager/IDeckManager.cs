using property_dealer_API.Models.Cards;

namespace property_dealer_API.Core.Logic.DecksManager
{
    public interface IDeckManager : IReadOnlyDeckManager
    {
        void PopulateInitialDeck(List<Card> initialDeck);
        List<Card> DrawCard(int numToDraw);
        void Discard(Card card);
    }
}