using property_dealer_API.Application.Enums;
using property_dealer_API.Core.Entities;
using property_dealer_API.Models.Cards;
using property_dealer_API.Models.Enums.Cards;

namespace property_dealer_API.Core.Logic.ActionExecution.ActionHandlers
{
    public interface IActionHandler
    {
        ActionTypes ActionType { get; }
        ActionContext? Initialize(Player initiator, Card card, List<Player> allPlayers);
        void ProcessResponse(Player responder, ActionContext currentContext);
    }
}
