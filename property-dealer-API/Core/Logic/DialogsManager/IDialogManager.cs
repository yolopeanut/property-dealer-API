using property_dealer_API.Application.MethodReturns;
using property_dealer_API.Core.Entities;
using property_dealer_API.Models.Cards;

namespace property_dealer_API.Core.Logic.DialogsManager
{
    public interface IDialogManager
    {
        ActionContext? ExecuteAction(string userId, string cardId, Card card, Player currentUser, List<Player> allPlayers);
        DialogProcessingResult ProcessPendingResponses(ActionContext actionContext);
        DialogProcessingResult RegisterActionResponse(Player player, ActionContext actionContext);
    }
}