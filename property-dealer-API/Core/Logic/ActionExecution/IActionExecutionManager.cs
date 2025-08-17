using property_dealer_API.Core.Entities;
using property_dealer_API.Models.Cards;

namespace property_dealer_API.Core.Logic.ActionExecution
{
    public interface IActionExecutionManager
    {
        ActionContext? ExecuteAction(string userId, Card card, Player currentUser, List<Player> allPlayers);
        void HandleDialogResponse(Player responder, ActionContext actionContext);
    }
}