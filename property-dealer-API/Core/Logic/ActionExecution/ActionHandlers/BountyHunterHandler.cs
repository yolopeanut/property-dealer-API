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
    public class BountyHunterHandler : ActionHandlerBase, IActionHandler
    {
        public ActionTypes ActionType => ActionTypes.BountyHunter;

        public BountyHunterHandler(
            IPlayerManager playerManager,
            IPlayerHandManager playerHandManager,
            IGameRuleManager rulesManager,
            IPendingActionManager pendingActionManager,
            IActionExecutor actionExecutor)
            : base(playerManager, playerHandManager, rulesManager, pendingActionManager, actionExecutor)
        { }

        public ActionContext? Initialize(Player initiator, Card card, List<Player> allPlayers)
        {
            if (card is not CommandCard commandCard || commandCard.Command != ActionTypes.BountyHunter)
            {
                throw new CardMismatchException(initiator.UserId, card.CardGuid.ToString());
            }

            var pendingAction = new PendingAction { InitiatorUserId = initiator.UserId, ActionType = commandCard.Command };
            var newActionContext = base.CreateActionContext(card.CardGuid.ToString(), DialogTypeEnum.PlayerSelection, initiator, null, allPlayers, pendingAction);

            base.SetNextDialog(newActionContext, DialogTypeEnum.PlayerSelection, initiator, null);
            return newActionContext;
        }

        public void ProcessResponse(Player responder, ActionContext currentContext)
        {
            switch (currentContext.DialogToOpen)
            {
                case DialogTypeEnum.PlayerSelection:
                    // Only the initiator can select a player.
                    if (responder.UserId != currentContext.ActionInitiatingPlayerId)
                        throw new InvalidOperationException("Only the action initiator can select a player.");

                    this.ProcessPlayerSelection(currentContext);
                    break;

                case DialogTypeEnum.PayValue:
                    // Only the target can respond with payment.
                    if (responder.UserId != currentContext.TargetPlayerId)
                        throw new InvalidOperationException("Only the target player can respond with payment.");

                    this.ProcessPayment(currentContext, responder);
                    break;

                case DialogTypeEnum.ShieldsUp:
                    base.HandleShieldsUp(responder);
                    break;

                default:
                    throw new InvalidOperationException($"Invalid state for BountyHunter action: {currentContext.DialogToOpen}");
            }
        }

        private void ProcessPlayerSelection(ActionContext currentContext)
        {
            var initiator = base.PlayerManager.GetPlayerByUserId(currentContext.ActionInitiatingPlayerId);
            var targetPlayer = base.PlayerManager.GetPlayerByUserId(currentContext.TargetPlayerId!);
            var targetPlayerHand = base.PlayerHandManager.GetPlayerHand(targetPlayer.UserId);

            // Before asking for payment, check if the target can block the action.
            if (base.RulesManager.DoesPlayerHaveShieldsUp(targetPlayer, targetPlayerHand))
            {
                // Present the target with the option to use their shield.
                base.BuildShieldsUpContext(currentContext, initiator, targetPlayer);
            }
            else
            {
                // If they cannot block, proceed to the payment step.
                base.SetNextDialog(currentContext, DialogTypeEnum.PayValue, initiator, targetPlayer);
            }
        }

        private void ProcessPayment(ActionContext currentContext, Player responder)
        {
            if (currentContext.OwnTargetCardId == null || currentContext.OwnTargetCardId.Count < 1)
            {
                throw new ActionContextParameterNullException(currentContext, $"Payment cards were not provided for {currentContext.ActionType}!");
            }

            // The 'responder' is the player paying. The initiator is receiving the payment.
            base.ActionExecutor.ExecutePayment(
                currentContext.ActionInitiatingPlayerId,
                responder.UserId,
                currentContext.OwnTargetCardId
            );

            base.CompleteAction();
        }
    }
}