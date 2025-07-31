using property_dealer_API.Core.Entities;
using property_dealer_API.Models.Cards;

namespace property_dealer_API.Core.Logic.ActionExecution.ActionsContextBuilder
{
    public interface IActionContextBuilder
    {
        ActionContext? BuildActionContext(string userId, Card card, Player currentUser, List<Player> allPlayers);
    }
}