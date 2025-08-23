using System.Collections.Concurrent;
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
    public abstract class ActionHandlerBase
    {
        protected readonly IPlayerManager PlayerManager;
        protected readonly IPlayerHandManager PlayerHandManager;
        protected readonly IGameRuleManager RulesManager;
        protected readonly IPendingActionManager PendingActionManager;
        protected readonly IActionExecutor ActionExecutor;

        protected ActionHandlerBase(
            IPlayerManager playerManager,
            IPlayerHandManager playerHandManager,
            IGameRuleManager rulesManager,
            IPendingActionManager pendingActionManager,
            IActionExecutor actionExecutor
        )
        {
            this.PlayerManager = playerManager;
            this.PlayerHandManager = playerHandManager;
            this.RulesManager = rulesManager;
            this.PendingActionManager = pendingActionManager;
            this.ActionExecutor = actionExecutor;
        }

        protected ActionContext CreateActionContext(
            string cardId,
            DialogTypeEnum dialogType,
            Player currentUser,
            Player? targetUser,
            List<Player> allPlayers,
            PendingAction pendingAction
        )
        {
            var dialogTargetList = this.RulesManager.IdentifyWhoSeesDialog(
                currentUser,
                targetUser,
                allPlayers,
                dialogType
            );
            var amountToPay = this.RulesManager.GetPaymentAmount(pendingAction.ActionType);
            pendingAction.RequiredResponders = new ConcurrentBag<Player>(dialogTargetList);

            // Set the pending action
            var actionContext = new ActionContext
            {
                CardId = cardId,
                ActionInitiatingPlayerId = currentUser.UserId,
                ActionType = pendingAction.ActionType,
                DialogTargetList = dialogTargetList,
                DialogToOpen = dialogType,
                PaymentAmount = amountToPay,
            };

            pendingAction.CurrentActionContext = actionContext;
            this.PendingActionManager.CurrPendingAction = pendingAction;
            return actionContext;
        }

        protected void SetNextDialog(
            ActionContext currentContext,
            DialogTypeEnum nextDialog,
            Player initiator,
            Player? target
        )
        {
            var allPlayers = this.PlayerManager.GetAllPlayers();
            currentContext.DialogToOpen = nextDialog;
            currentContext.DialogTargetList = this.RulesManager.IdentifyWhoSeesDialog(
                initiator,
                target,
                allPlayers,
                nextDialog
            );

            var pendingAction = this.PendingActionManager.CurrPendingAction;
            pendingAction.CurrentActionContext = currentContext;
            if (pendingAction != null)
            {
                pendingAction.RequiredResponders = new ConcurrentBag<Player>(
                    currentContext.DialogTargetList
                );
            }
        }

        protected virtual void CompleteAction()
        {
            this.PendingActionManager.IncrementProcessedActions();
        }

        protected virtual ActionResult? HandleShieldsUp(
            Player responder,
            ActionContext currentContext,
            Func<ActionContext, Player, Boolean, ActionResult?>? callbackIfShieldsUpRejected
        )
        {
            if (currentContext.DialogResponse == CommandResponseEnum.ShieldsUp)
            {
                var targetPlayer = this.PlayerManager.GetPlayerByUserId(responder.UserId);
                var targetPlayerHand = this.PlayerHandManager.GetPlayerHand(targetPlayer.UserId);
                if (!this.RulesManager.DoesPlayerHaveShieldsUp(targetPlayer, targetPlayerHand))
                {
                    throw new CardNotFoundException("Shields up was not found in players deck!");
                }
                var playerHand = this.PlayerHandManager.GetPlayerHand(responder.UserId);

                var shieldsUpCard = playerHand.FirstOrDefault(card =>
                    card is CommandCard commandCard && commandCard.Command == ActionTypes.ShieldsUp
                );
                if (shieldsUpCard != null)
                {
                    this.ActionExecutor.HandleRemoveFromHand(
                        responder.UserId,
                        shieldsUpCard.CardGuid.ToString()
                    );
                }
                this.CompleteAction();

                // ActionInitiating player for shields up is the target player because they reject the action
                return new ActionResult
                {
                    ActionInitiatingPlayerId = targetPlayer.UserId,
                    AffectedPlayerId = currentContext.ActionInitiatingPlayerId,
                    ActionType = ActionTypes.ShieldsUp,
                };
            }
            else if (currentContext.DialogResponse == CommandResponseEnum.RejectShieldsUp)
            {
                if (callbackIfShieldsUpRejected == null)
                {
                    throw new InvalidOperationException(
                        "Cannot do accept response when callback action is null!"
                    );
                }
                return callbackIfShieldsUpRejected(currentContext, responder, false); // Will handle the complete action in the called function
            }
            return null;
        }

        protected void BuildShieldsUpContext(ActionContext context, Player initiator, Player target)
        {
            this.SetNextDialog(context, DialogTypeEnum.ShieldsUp, initiator, target);
        }
    }
}
