using property_dealer_API.Application.Enums;
using property_dealer_API.Application.Exceptions;
using property_dealer_API.Application.MethodReturns;
using property_dealer_API.Core.Entities;
using property_dealer_API.Core.Logic.GameRulesManager;
using property_dealer_API.Core.Logic.PendingActionsManager;
using property_dealer_API.Core.Logic.PlayerHandsManager;
using property_dealer_API.Core.Logic.PlayersManager;
using property_dealer_API.Models.Cards;

namespace property_dealer_API.Core.Logic.ActionExecution.ActionHandlers.ActionSteps
{
    public class PirateRaidTableHandStep : IActionStep
    {
        private readonly IPlayerHandManager _playerHandManager;
        private readonly IPlayerManager _playerManager;
        private readonly IGameRuleManager _rulesManager;
        private readonly IActionExecutor _actionExecutor;
        private readonly IPendingActionManager _pendingActionManager;

        public PirateRaidTableHandStep(
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

            var initiatorId = currentContext.ActionInitiatingPlayerId;
            var initiator = this._playerManager.GetPlayerByUserId(initiatorId);

            var targetPlayer = this._playerManager.GetPlayerByUserId(currentContext.TargetPlayerId);
            var targetPlayerHand = this._playerHandManager.GetPlayerHand(targetPlayer.UserId);
            var (targetCard, targetCardPropertyGroup) = this._playerHandManager.GetCardInTableHand(
                targetPlayer.UserId,
                currentContext.TargetCardId
            );

            this.ValidateActionPrerequisites(targetPlayer, targetCard);

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
                return null;
            }

            // Check for wildcard - only target card can be wildcard in PirateRaid
            if (this._rulesManager.IsCardSystemWildCard(targetCard))
            {
                stepService.SetNextDialog(
                    currentContext,
                    DialogTypeEnum.WildcardColor,
                    initiator,
                    null
                );
                return null;
            }

            // Execute normal pirate raid
            this._actionExecutor.MovePropertyBetweenTableHands(
                initiatorId: currentContext.ActionInitiatingPlayerId,
                targetId: currentContext.TargetPlayerId,
                cardIdToTake: currentContext.TargetCardId,
                colorForTakenCard: targetCardPropertyGroup
            );

            stepService.CompleteAction();

            return new ActionResult
            {
                ActionInitiatingPlayerId = currentContext.ActionInitiatingPlayerId,
                AffectedPlayerId = targetPlayer.UserId,
                ActionType = currentContext.ActionType,
                TakenCard = targetCard.ToDto(),
            };
        }

        private void ValidateActionPrerequisites(Player targetPlayer, Card targetCard)
        {
            if (targetCard is not StandardSystemCard systemCard)
                return;

            var targetPlayerTableHand = this._playerHandManager.GetPropertyGroupInPlayerTableHand(
                targetPlayer.UserId,
                systemCard.CardColoursEnum
            );

            this._rulesManager.ValidatePirateRaidTarget(
                targetPlayerTableHand,
                systemCard.CardColoursEnum
            );
        }
    }
}
