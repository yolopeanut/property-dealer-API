using property_dealer_API.Application.Enums;
using property_dealer_API.Application.Exceptions;
using property_dealer_API.Application.MethodReturns;
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
            IActionExecutor actionExecutor
        )
            : base(
                playerManager,
                playerHandManager,
                rulesManager,
                pendingActionManager,
                actionExecutor
            ) { }

        public ActionContext? Initialize(Player initiator, Card card, List<Player> allPlayers)
        {
            if (
                card is not CommandCard commandCard
                || commandCard.Command != ActionTypes.BountyHunter
            )
            {
                throw new CardMismatchException(initiator.UserId, card.CardGuid.ToString());
            }

            var pendingAction = new PendingAction
            {
                InitiatorUserId = initiator.UserId,
                ActionType = commandCard.Command,
            };
            var newActionContext = base.CreateActionContext(
                card.CardGuid.ToString(),
                DialogTypeEnum.PlayerSelection,
                initiator,
                null,
                allPlayers,
                pendingAction
            );

            base.SetNextDialog(newActionContext, DialogTypeEnum.PlayerSelection, initiator, null);
            return newActionContext;
        }

        public ActionResult? ProcessResponse(Player responder, ActionContext currentContext)
        {
            switch (currentContext.DialogToOpen)
            {
                case DialogTypeEnum.PlayerSelection:
                    // Only the initiator can select a player.
                    if (responder.UserId != currentContext.ActionInitiatingPlayerId)
                        throw new InvalidOperationException(
                            "Only the action initiator can select a player."
                        );

                    this.ProcessPlayerSelection(currentContext);
                    break;

                case DialogTypeEnum.PayValue:
                    // Only the target can respond with payment.
                    if (responder.UserId != currentContext.TargetPlayerId)
                        throw new InvalidOperationException(
                            "Only the target player can respond with payment."
                        );

                    this.ProcessPayment(currentContext, responder);
                    break;

                default:
                    throw new InvalidOperationException(
                        $"Invalid state for BountyHunter action: {currentContext.DialogToOpen}"
                    );
            }

            return null;
        }

        private void ProcessPlayerSelection(ActionContext currentContext)
        {
            var initiator = base.PlayerManager.GetPlayerByUserId(
                currentContext.ActionInitiatingPlayerId
            );
            var targetPlayer = base.PlayerManager.GetPlayerByUserId(currentContext.TargetPlayerId!);
            var targetPlayerHand = base.PlayerHandManager.GetPlayerHand(targetPlayer.UserId);

            // If they cannot block, proceed to the payment step.
            base.SetNextDialog(currentContext, DialogTypeEnum.PayValue, initiator, targetPlayer);
        }

        private ActionResult? ProcessPayment(
            ActionContext currentContext,
            Player responder,
            Boolean _ = true
        )
        {
            // Check if the response was a "Shields Up" card.
            // This assumes a shield play consists of submitting just the single shield card.
            if (currentContext.DialogResponse == CommandResponseEnum.ShieldsUp)
            {
                var targetPlayer = base.PlayerManager.GetPlayerByUserId(
                    currentContext.TargetPlayerId!
                );
                var targetPlayerHand = base.PlayerHandManager.GetPlayerHand(targetPlayer.UserId);
                var initiator = base.PlayerManager.GetPlayerByUserId(
                    currentContext.ActionInitiatingPlayerId
                );

                if (base.RulesManager.DoesPlayerHaveShieldsUp(targetPlayer, targetPlayerHand))
                {
                    return base.HandleShieldsUp(responder, currentContext, this.ProcessPayment);
                }
                else
                {
                    throw new CardNotFoundException("Shields up was not found in players deck!");
                }
            }

            if (currentContext.OwnTargetCardId == null || !currentContext.OwnTargetCardId.Any())
            {
                var playerHand = base.PlayerHandManager.GetPlayerTableHand(responder.UserId);
                var moneyHand = base.PlayerHandManager.GetPlayerMoneyHand(responder.UserId);
                if (!base.RulesManager.IsPlayerBroke(playerHand, moneyHand))
                {
                    throw new ActionContextParameterNullException(
                        currentContext,
                        $"A response (payment or shield) must be provided for {currentContext.ActionType}!"
                    );
                }

                return null;
            }

            // The 'responder' is the player paying. The initiator is receiving the payment.
            base.ActionExecutor.ExecutePayment(
                currentContext.ActionInitiatingPlayerId,
                responder.UserId,
                currentContext.OwnTargetCardId
            );

            base.CompleteAction();

            return null;
        }
    }
}
