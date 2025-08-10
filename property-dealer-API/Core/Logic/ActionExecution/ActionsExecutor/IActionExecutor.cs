using property_dealer_API.Models.Cards;
using property_dealer_API.Models.Enums.Cards;

namespace property_dealer_API.Core.Logic.ActionExecution
{
    public interface IActionExecutor
    {
        void ExecuteHostileTakeover(string initiatorUserId, string targetUserId, PropertyCardColoursEnum targetSetColor);
        void ExecuteForcedTrade(string initiatorUserId, string targetUserId, string targetCardId, string ownCardId);
        void ExecutePirateRaid(string initiatorUserId, string targetUserId, string targetCardId);
        Card HandleRemoveFromHand(string userId, string cardId);
        void AssignCardToPlayer(string userId, int numCardsToDraw);
    }
}