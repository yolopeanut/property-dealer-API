using property_dealer_API.Models.Enums.Cards;

namespace property_dealer_API.Core.Logic.DebuggingManager
{
    public interface IDebugManager
    {
        void GiveAllCardsInDeck();
        void GivePlayerCard(string userId, string cardType);
        void SetPlayerMoney(string userId, int amount);
        void CompletePlayerPropertySet(string userId, PropertyCardColoursEnum color);
        void ForcePlayerWin(string userId);
        void SkipToNextPlayer();
        void ResetGame();
        void SetPlayerHandSize(string userId, int size);
    }
}