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

namespace property_dealer_API.Core.Logic.ActionExecution.ActionHandlers.ActionSteps.TableHandSelectStep
{
    public class ForcedTradeTableHandSelectionStep : IActionStep
    {
        private readonly IPlayerHandManager _playerHandManager;
        private readonly IPlayerManager _playerManager;
        private readonly IGameRuleManager _rulesManager;
        private readonly IActionExecutor _actionExecutor;
        private readonly IPendingActionManager _pendingActionManager;

        public ForcedTradeTableHandSelectionStep(
            IPlayerHandManager playerHandManager,
            IPlayerManager playerManager,
            IGameRuleManager rulesManager,
            IActionExecutor actionExecutor,
            IPendingActionManager pendingActionManager
        )
        {
            this._playerHandManager = playerHandManager;
            this._playerManager = playerManager;
            this._rulesManager = rulesManager;
            this._actionExecutor = actionExecutor;
            this._pendingActionManager = pendingActionManager;
        }

        public ActionResult? ProcessStep(
            Player responder,
            ActionContext currentContext,
            IActionStepService stepService,
            DialogTypeEnum? nextDialog = null
        )
        {
            var pendingAction = this._pendingActionManager.CurrPendingAction;
            if (pendingAction == null)
                throw new InvalidOperationException("No pending action found.");

            if (string.IsNullOrEmpty(currentContext.TargetPlayerId))
                throw new ActionContextParameterNullException(
                    currentContext,
                    "TargetPlayerId cannot be null for TableHandSelector."
                );
            if (currentContext.TargetCardId == null || !currentContext.TargetCardId.Any())
                throw new ActionContextParameterNullException(
                    currentContext,
                    $"TargetCardId was found null in {pendingAction.ActionType}!"
                );
            if (currentContext.OwnTargetCardId == null || !currentContext.OwnTargetCardId.Any())
                throw new ActionContextParameterNullException(
                    currentContext,
                    $"OwnTargetCardId was found null in {pendingAction.ActionType}!"
                );

            var initiatorId = currentContext.ActionInitiatingPlayerId;
            var initiator = this._playerManager.GetPlayerByUserId(initiatorId);
            string initiatorCardId = currentContext.OwnTargetCardId.First();
            var (cardFromInitiator, initiatorCardPropertyColorGroup) =
                this._playerHandManager.GetCardInTableHand(initiatorId, initiatorCardId);

            string targetId = currentContext.TargetPlayerId;
            string targetCardId = currentContext.TargetCardId;
            var targetPlayer = this._playerManager.GetPlayerByUserId(targetId);
            var (cardFromTarget, targetCardPropertyColorGroup) =
                this._playerHandManager.GetCardInTableHand(targetId, targetCardId);
            var targetPlayerHand = this._playerHandManager.GetPlayerHand(targetPlayer.UserId);

            this.ValidateActionPrerequisites(pendingAction, targetPlayer, cardFromTarget);
            bool specialConditionHandled = false;

            specialConditionHandled = this.TryHandleSpecialConditions(
                currentContext,
                responder,
                targetPlayer,
                cardFromTarget,
                targetPlayerHand,
                stepService
            );

            if (!specialConditionHandled)
            {
                this.ExecuteNormalAction(
                    currentContext,
                    targetPlayer,
                    targetCardPropertyColorGroup,
                    initiatorCardPropertyColorGroup
                );
                stepService.CompleteAction();

                if (currentContext.DialogResponse == CommandResponseEnum.RejectShieldsUp)
                {
                    return null;
                }

                return new ActionResult
                {
                    ActionInitiatingPlayerId = currentContext.ActionInitiatingPlayerId,
                    AffectedPlayerId = targetPlayer.UserId,
                    ActionType = currentContext.ActionType,
                    TakenCard = cardFromTarget.ToDto(),
                    GivenCard = cardFromInitiator.ToDto(),
                };
            }
            return null;
        }

        private void ValidateActionPrerequisites(
            PendingAction pendingAction,
            Player targetPlayer,
            Card targetCard
        )
        {
            if (targetCard is not StandardSystemCard systemCard)
                return;

            var targetPlayerTableHand = this._playerHandManager.GetPropertyGroupInPlayerTableHand(
                targetPlayer.UserId,
                systemCard.CardColoursEnum
            );

            if (pendingAction.ActionType == ActionTypes.ForcedTrade)
            {
                this._rulesManager.ValidateForcedTradeTarget(
                    targetPlayerTableHand,
                    systemCard.CardColoursEnum
                );
            }
        }

        private bool TryHandleSpecialConditions(
            ActionContext currentContext,
            Player responder,
            Player targetPlayer,
            Card targetCard,
            List<Card> targetPlayerHand,
            IActionStepService stepService
        )
        {
            var hasShieldsUpCard = this._rulesManager.DoesPlayerHaveShieldsUp(
                targetPlayer,
                targetPlayerHand
            );
            var hasNotRejectedShieldsUp = !this._rulesManager.IsShieldsUpRejectedFromVictim(
                currentContext
            );
            // Check for shields up
            if (hasShieldsUpCard && hasNotRejectedShieldsUp)
            {
                stepService.BuildShieldsUpContext(currentContext, responder, targetPlayer);
                return true;
            }

            if (this._rulesManager.IsCardSystemWildCard(targetCard))
            {
                this.BuildWildCardMovementContext(currentContext, responder, stepService); // Initiator benefits
                return true;
            }

            if (currentContext.OwnTargetCardId == null || !currentContext.OwnTargetCardId.Any())
            {
                throw new ActionContextParameterNullException(
                    currentContext,
                    "OwnTargetCardId null when handling forced trade!"
                );
            }

            var (ownTargetCard, _) = this._playerHandManager.GetCardInTableHand(
                currentContext.ActionInitiatingPlayerId,
                currentContext.OwnTargetCardId.First()
            );
            if (this._rulesManager.IsCardSystemWildCard(ownTargetCard))
            {
                this.BuildWildCardMovementContext(currentContext, targetPlayer, stepService); // Target player benefits
                return true;
            }

            return false; // No special conditions were met
        }

        private void ExecuteNormalAction(
            ActionContext currentContext,
            Player targetPlayer,
            PropertyCardColoursEnum colorForTakenCard,
            PropertyCardColoursEnum colorForGivenCard
        )
        {
            if (currentContext.TargetCardId == null || !currentContext.TargetCardId.Any())
            {
                throw new ActionContextParameterNullException(
                    currentContext,
                    "TargetCardId is null for normal execution."
                );
            }

            if (currentContext.OwnTargetCardId == null || !currentContext.OwnTargetCardId.Any())
            {
                throw new ActionContextParameterNullException(
                    currentContext,
                    "OwnTargetCardId is null for ForcedTrade execution."
                );
            }

            this._actionExecutor.MovePropertyBetweenTableHands(
                initiatorId: currentContext.ActionInitiatingPlayerId,
                targetId: targetPlayer.UserId,
                cardIdToTake: currentContext.TargetCardId,
                colorForTakenCard: colorForTakenCard,
                cardIdToGive: currentContext.OwnTargetCardId.First(),
                colorForGivenCard: colorForGivenCard
            );
        }

        private void BuildWildCardMovementContext(
            ActionContext context,
            Player beneficiary,
            IActionStepService stepService
        )
        {
            stepService.SetNextDialog(context, DialogTypeEnum.WildcardColor, beneficiary, null); // Wildcard choice is seen by the beneficiary
        }
    }
}
