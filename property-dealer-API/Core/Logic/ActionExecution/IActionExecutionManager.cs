using property_dealer_API.Core.Entities;
using property_dealer_API.Models.Cards;

namespace property_dealer_API.Core.Logic.ActionExecution
{
    public interface IActionExecutionManager
    {
        ActionContext? ExecuteAction(string userId, Card card, Player currentUser, List<Player> allPlayers);
        void HandleShieldsUpResponse(Player player, ActionContext actionContext);
        void HandlePayValueResponse(Player player, ActionContext actionContext);
        void HandleWildCardResponse(Player player, ActionContext actionContext);
        void HandlePropertySetSelectionResponse(Player player, ActionContext actionContext);
        void HandleTableHandSelectorResponse(Player player, ActionContext actionContext);
        void HandlePlayerSelectionResponse(Player player, ActionContext actionContext);
    }
}