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
    public class SystemWildCardHandler : ActionHandlerBase, IActionHandler
    {
        // This handler is triggered by the card type, not a command.
        // We'll give it a unique ActionType for context, even if the card itself doesn't have a 'command'.
        public ActionTypes ActionType => ActionTypes.SystemWildCard;

        public SystemWildCardHandler(
            IPlayerManager playerManager,
            IPlayerHandManager playerHandManager,
            IGameRuleManager rulesManager,
            IPendingActionManager pendingActionManager,
            IActionExecutor actionExecutor)
            : base(playerManager, playerHandManager, rulesManager, pendingActionManager, actionExecutor)
        { }

        public ActionContext? Initialize(Player initiator, Card card, List<Player> allPlayers)
        {
            if (card is not SystemWildCard)
            {
                throw new CardMismatchException(initiator.UserId, card.CardGuid.ToString());
            }

            // The pending action type helps identify what's happening.
            var pendingAction = new PendingAction { InitiatorUserId = initiator.UserId, ActionType = ActionTypes.SystemWildCard };
            var newActionContext = base.CreateActionContext(card.CardGuid.ToString(), DialogTypeEnum.WildcardColor, initiator, null, allPlayers, pendingAction);

            // The first and only dialog needed is for the initiator to choose a color.
            base.SetNextDialog(newActionContext, DialogTypeEnum.WildcardColor, initiator, null);
            return newActionContext;
        }

        public void ProcessResponse(Player responder, ActionContext currentContext)
        {
            // Only the initiator of the card play can respond.
            if (responder.UserId != currentContext.ActionInitiatingPlayerId)
                throw new InvalidOperationException("Only the player who played the SystemWildCard can choose its color.");

            switch (currentContext.DialogToOpen)
            {
                case DialogTypeEnum.WildcardColor:
                    this.ProcessColorSelection(currentContext);
                    break;

                default:
                    throw new InvalidOperationException($"Invalid state for SystemWildCard action: {currentContext.DialogToOpen}");
            }
        }

        private void ProcessColorSelection(ActionContext currentContext)
        {
            if (currentContext.TargetSetColor == null)
            {
                throw new ActionContextParameterNullException(currentContext, "TargetSetColor must be provided when choosing a wildcard color.");
            }

            // We assume the card being played is the one in the ActionContext's CardId.
            base.ActionExecutor.ExecutePlayToTable(
                currentContext.ActionInitiatingPlayerId,
                currentContext.CardId,
                currentContext.TargetSetColor.Value // The color chosen by the player.
            );

            base.CompleteAction();
        }
    }
}