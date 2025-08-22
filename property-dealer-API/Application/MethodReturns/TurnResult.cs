using Microsoft.AspNetCore.Mvc.RazorPages;
using property_dealer_API.Core;
using property_dealer_API.Core.Entities;

namespace property_dealer_API.Application.MethodReturns
{
    public class TurnResult
    {
        public ActionContext? ActionContext { get; set; }
        public Player? WinningPlayer { get; set; }
        public Player? NeedToRemoveCardPlayer { get; set; }
        public ActionResult? ActionResults { get; set; }
        public bool GameEnded => this.WinningPlayer != null;

        public TurnResult(
            ActionContext? actionContext = null,
            Player? winningPlayer = null,
            Player? needToRemoveCardPlayer = null,
            ActionResult? actionResult = null
        )
        {
            this.ActionContext = actionContext;
            this.WinningPlayer = winningPlayer;
            this.NeedToRemoveCardPlayer = needToRemoveCardPlayer;
            this.ActionResults = actionResult;
        }
    }
}
