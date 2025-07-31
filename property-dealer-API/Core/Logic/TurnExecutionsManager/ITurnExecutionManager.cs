using property_dealer_API.Application.Enums;
using property_dealer_API.Models.Cards;
using property_dealer_API.Models.Enums.Cards;

namespace property_dealer_API.Core.Logic.TurnExecutionsManager
{
    public interface ITurnExecutionManager
    {
        ActionContext? ExecuteTurnAction(string userId, Card playerHandCard, CardDestinationEnum cardDestination, PropertyCardColoursEnum? colorDestination);
        void RecoverFromFailedTurn(string userId, Card card);
    }
}