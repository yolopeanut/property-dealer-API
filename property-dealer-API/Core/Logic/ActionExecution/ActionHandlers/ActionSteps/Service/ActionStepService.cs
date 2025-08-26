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

namespace property_dealer_API.Core.Logic.ActionExecution.ActionHandlers.ActionSteps.Service
{
    public class ActionStepService : IActionStepService
    {
        private readonly IPlayerManager _playerManager;
        private readonly IPlayerHandManager _playerHandManager;
        private readonly IGameRuleManager _rulesManager;
        private readonly IPendingActionManager _pendingActionManager;
        private readonly IActionExecutor _actionExecutor;

        public ActionStepService(
            IPlayerManager playerManager,
            IPlayerHandManager playerHandManager,
            IGameRuleManager rulesManager,
            IPendingActionManager pendingActionManager,
            IActionExecutor actionExecutor
        )
        {
            this._playerManager = playerManager;
            this._playerHandManager = playerHandManager;
            this._rulesManager = rulesManager;
            this._pendingActionManager = pendingActionManager;
            this._actionExecutor = actionExecutor;
        }

        public ActionResult? HandleShieldsUp(
            Player responder,
            ActionContext currentContext,
            Func<ActionContext, Player, bool, ActionResult?>? callbackIfShieldsUpRejected
        )
        {
            if (currentContext.DialogResponse == CommandResponseEnum.ShieldsUp)
            {
                var targetPlayer = this._playerManager.GetPlayerByUserId(responder.UserId);
                var targetPlayerHand = this._playerHandManager.GetPlayerHand(targetPlayer.UserId);
                var hasShieldsUpCard = this._rulesManager.DoesPlayerHaveShieldsUp(
                    targetPlayer,
                    targetPlayerHand
                );

                if (!hasShieldsUpCard)
                {
                    throw new CardNotFoundException("Shields up was not found in players deck!");
                }
                var playerHand = this._playerHandManager.GetPlayerHand(responder.UserId);

                var shieldsUpCard = playerHand.FirstOrDefault(card =>
                    card is CommandCard commandCard && commandCard.Command == ActionTypes.ShieldsUp
                );
                if (shieldsUpCard != null)
                {
                    this._actionExecutor.HandleRemoveFromHand(
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
                return callbackIfShieldsUpRejected(currentContext, responder, false);
            }
            return null;
        }

        public void CompleteAction()
        {
            this._pendingActionManager.IncrementProcessedActions();
        }

        public void BuildShieldsUpContext(ActionContext context, Player initiator, Player target)
        {
            this.SetNextDialog(context, DialogTypeEnum.ShieldsUp, initiator, target);
        }

        public void SetNextDialog(
            ActionContext currentContext,
            DialogTypeEnum nextDialog,
            Player initiator,
            Player? target
        )
        {
            var allPlayers = this._playerManager.GetAllPlayers();
            currentContext.DialogToOpen = nextDialog;
            currentContext.DialogTargetList = this._rulesManager.IdentifyWhoSeesDialog(
                initiator,
                target,
                allPlayers,
                nextDialog
            );

            var pendingAction = this._pendingActionManager.CurrPendingAction;
            pendingAction.CurrentActionContext = currentContext;
            if (pendingAction != null)
            {
                pendingAction.RequiredResponders = new ConcurrentBag<Player>(
                    currentContext.DialogTargetList
                );
            }
        }

        public void CalculateTributeAmount(ActionContext currentContext)
        {
            if (currentContext.ActionType == ActionTypes.TradeEmbargo)
            {
                if (currentContext.OwnTargetCardId?.Count < 0)
                {
                    return;
                }
            }

            if (!currentContext.TargetSetColor.HasValue)
            {
                return;
            }

            var playerTableHand = this._playerHandManager.GetPropertyGroupInPlayerTableHand(
                currentContext.ActionInitiatingPlayerId,
                currentContext.TargetSetColor.Value
            );

            // Calculate the base rent amount and then double it
            var baseRentAmount = this._rulesManager.CalculateRentAmount(
                currentContext.TargetSetColor.Value,
                playerTableHand
            );

            currentContext.PaymentAmount = baseRentAmount;

            if (currentContext.ActionType == ActionTypes.TradeEmbargo)
            {
                currentContext.PaymentAmount *= 2; // Double the rent for Trade Embargo
            }
        }

        public void ProcessRentCardSelection(ActionContext currentContext)
        {
            if (currentContext.OwnTargetCardId == null || !currentContext.OwnTargetCardId.Any())
                throw new ActionContextParameterNullException(
                    currentContext,
                    "A rent card must be selected."
                );

            var rentCardId = currentContext.OwnTargetCardId.First();
            var rentCard = this._playerHandManager.GetCardFromPlayerHandById(
                currentContext.ActionInitiatingPlayerId,
                rentCardId
            );
            if (rentCard is not TributeCard)
            {
                throw new CardMismatchException(
                    currentContext.ActionInitiatingPlayerId,
                    rentCardId
                );
            }

            var initiator = this._playerManager.GetPlayerByUserId(
                currentContext.ActionInitiatingPlayerId
            );
            this.SetNextDialog(
                currentContext,
                DialogTypeEnum.PropertySetSelection,
                initiator,
                null
            );
        }
    }
}
