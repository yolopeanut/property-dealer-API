

using Microsoft.AspNetCore.Mvc;
using property_dealer_API.Application.Enums;
using property_dealer_API.Application.Exceptions;
using property_dealer_API.Core.Entities;
using property_dealer_API.Core.Logic.ActionExecution.ActionsContextBuilder;
using property_dealer_API.Core.Logic.DecksManager;
using property_dealer_API.Core.Logic.GameRulesManager;
using property_dealer_API.Core.Logic.PendingActionsManager;
using property_dealer_API.Core.Logic.PlayerHandsManager;
using property_dealer_API.Core.Logic.PlayersManager;
using property_dealer_API.Models.Cards;
using property_dealer_API.Models.Enums.Cards;
using System.Collections.Concurrent;
using System.Numerics;

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
            _contextBuilder = contextBuilder;
            _dialogProcessor = dialogProcessor;
        }

        public ActionContext? ExecuteAction(string userId, Card card, Player currentUser, List<Player> allPlayers)
            => _contextBuilder.BuildActionContext(userId, card, currentUser, allPlayers);

        public void HandleShieldsUpResponse(Player player, ActionContext actionContext)
            => _dialogProcessor.HandleShieldsUpResponse(player, actionContext);

        public void HandlePayValueResponse(Player player, ActionContext actionContext)
            => _dialogProcessor.HandlePayValueResponse(player, actionContext);

        public void HandleWildCardResponse(Player player, ActionContext actionContext)
            => _dialogProcessor.HandleWildCardResponse(player, actionContext);

        public void HandlePropertySetSelectionResponse(Player player, ActionContext actionContext)
            => _dialogProcessor.HandlePropertySetSelectionResponse(player, actionContext);

        public void HandleTableHandSelectorResponse(Player player, ActionContext actionContext)
            => _dialogProcessor.HandleTableHandSelectorResponse(player, actionContext);

        public void HandlePlayerSelectionResponse(Player player, ActionContext actionContext)
            => _dialogProcessor.HandlePlayerSelectionResponse(player, actionContext);
    }
}
