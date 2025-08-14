using property_dealer_API.Models.Cards;
using property_dealer_API.Models.Enums.Cards;

namespace property_dealer_API.Core.Logic.ActionExecution
{
    public interface IActionExecutor
    {
        void ExecuteHostileTakeover(string initiatorUserId, string targetUserId, PropertyCardColoursEnum targetSetColor);
        void MovePropertyBetweenTableHands(string initiatorId, string targetId, string cardIdToTake, PropertyCardColoursEnum colorForTakenCard, string? cardIdToGive = null, PropertyCardColoursEnum? colorForGivenCard = null);
        void ExecutePropertyTrade(string initiatorId, string initiatorCardId, PropertyCardColoursEnum colorForCardFromTarget, string targetId, string targetCardId, PropertyCardColoursEnum colorForCardFromInitiator);
        void ExecutePirateRaid(string initiatorUserId, string targetUserId, string targetCardId);
        Card HandleRemoveFromHand(string userId, string cardId);
        void ExecuteDrawCards(string userId, int numCardsToDraw);
        void ExecutePayment(string receivingPlayerId, string payingPlayerId, List<string> targetsChosenCards);
        void ExecutePlayToTable(string actionInitiatingPlayerId, string cardId, PropertyCardColoursEnum targetSetColor);
        void ExecuteBuildOnSet(string actionInitiatingPlayerId, string cardId, PropertyCardColoursEnum targetSetColor);
    }
}