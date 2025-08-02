


using property_dealer_API.Core.Entities;
using property_dealer_API.Core.Logic.ActionExecution.ActionsContextBuilder;
using property_dealer_API.Models.Cards;

namespace property_dealer_API.Core.Logic.ActionExecution
{
    public class ActionExecutionManager : IActionExecutionManager
    {

        private readonly IActionContextBuilder _contextBuilder;
        private readonly IDialogResponseProcessor _dialogProcessor;

        public ActionExecutionManager(
            IActionContextBuilder contextBuilder,
            IDialogResponseProcessor dialogProcessor)
        {
            this._contextBuilder = contextBuilder;
            this._dialogProcessor = dialogProcessor;
        }

        public ActionContext? ExecuteAction(string userId, Card card, Player currentUser, List<Player> allPlayers)
            => this._contextBuilder.BuildActionContext(userId, card, currentUser, allPlayers);

        public void HandleShieldsUpResponse(Player player, ActionContext actionContext)
            => this._dialogProcessor.HandleShieldsUpResponse(player, actionContext);

        public void HandlePayValueResponse(Player player, ActionContext actionContext)
            => this._dialogProcessor.HandlePayValueResponse(player, actionContext);

        public void HandleWildCardResponse(Player player, ActionContext actionContext)
            => this._dialogProcessor.HandleWildCardResponse(player, actionContext);

        public void HandlePropertySetSelectionResponse(Player player, ActionContext actionContext)
            => this._dialogProcessor.HandlePropertySetSelectionResponse(player, actionContext);

        public void HandleTableHandSelectorResponse(Player player, ActionContext actionContext)
            => this._dialogProcessor.HandleTableHandSelectorResponse(player, actionContext);

        public void HandlePlayerSelectionResponse(Player player, ActionContext actionContext)
            => this._dialogProcessor.HandlePlayerSelectionResponse(player, actionContext);

        public void HandleOwnHandSelectionResponse(Player player, ActionContext actionContext)
            => this._dialogProcessor.HandleOwnHandSelectionResponse(player, actionContext);
    }
}
