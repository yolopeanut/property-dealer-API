using Microsoft.AspNetCore.Mvc;
using property_dealer_API.Application.Enums;
using property_dealer_API.Application.Exceptions;
using property_dealer_API.Core.Entities;
using property_dealer_API.Core.Logic.GameRulesManager;
using property_dealer_API.Core.Logic.PendingActionsManager;
using property_dealer_API.Core.Logic.PlayerHandsManager;
using property_dealer_API.Core.Logic.PlayersManager;
using property_dealer_API.Models.Cards;
using property_dealer_API.Models.Enums.Cards;

namespace property_dealer_API.Core.Logic.ActionExecution.ActionHandlers
{
    public class StarbaseHandler : ActionHandlerBase, IActionHandler
    {
        public ActionTypes ActionType => ActionTypes.Starbase;

        public StarbaseHandler(
            IPlayerManager playerManager,
            IPlayerHandManager playerHandManager,
            IGameRuleManager rulesManager,
            IPendingActionManager pendingActionManager,
            IActionExecutor actionExecutor)
            : base(playerManager, playerHandManager, rulesManager, pendingActionManager, actionExecutor)
        { }

        public ActionContext? Initialize(Player initiator, Card card, List<Player> allPlayers)
        {
            if (card is not CommandCard commandCard || commandCard.Command != ActionTypes.Starbase)
            {
                throw new CardMismatchException(initiator.UserId, card.CardGuid.ToString());
            }

            var pendingAction = new PendingAction { InitiatorUserId = initiator.UserId, ActionType = commandCard.Command };
            var newActionContext = base.CreateActionContext(card.CardGuid.ToString(), DialogTypeEnum.PropertySetSelection, initiator, null, allPlayers, pendingAction);

            // The first step is for the initiator to choose one of their own complete sets.
            base.SetNextDialog(newActionContext, DialogTypeEnum.PropertySetSelection, initiator, null);
            return newActionContext;
        }

        public void ProcessResponse(Player responder, ActionContext currentContext)
        {
            // Only the player who played the card can choose where to build.
            if (responder.UserId != currentContext.ActionInitiatingPlayerId)
                throw new InvalidOperationException("Only the action initiator can choose which set to build on.");

            switch (currentContext.DialogToOpen)
            {
                case DialogTypeEnum.PropertySetSelection:
                    this.ValidateProcess(currentContext);
                    this.ProcessPropertySetSelection(currentContext);
                    break;

                default:
                    throw new InvalidOperationException($"Invalid state for Starbase action: {currentContext.DialogToOpen}");
            }
        }

        private void ValidateProcess(ActionContext currentContext)
        {
            var pendingAction = base.PendingActionManager.CurrPendingAction;
            if (!currentContext.TargetSetColor.HasValue)
            {
                throw new ActionContextParameterNullException(currentContext, "TargetSetColor must be provided when building a Starbase.");
            }

            var playerTableHand = base.PlayerHandManager.GetPropertyGroupInPlayerTableHand(currentContext.ActionInitiatingPlayerId, currentContext.TargetSetColor.Value);

            base.RulesManager.ValidateStarbasePlacement(playerTableHand, currentContext.TargetSetColor.Value);
        }

        private void ProcessPropertySetSelection(ActionContext currentContext)
        {
            base.ActionExecutor.ExecuteBuildOnSet(
                currentContext.ActionInitiatingPlayerId,
                currentContext.CardId,
                currentContext.TargetSetColor!.Value
            );

            base.CompleteAction();
        }
    }
}