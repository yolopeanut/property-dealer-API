using property_dealer_API.Application.Enums;
using property_dealer_API.Application.Exceptions;
using property_dealer_API.Application.MethodReturns;
using property_dealer_API.Core.Entities;
using property_dealer_API.Core.Logic.GameRulesManager;
using property_dealer_API.Core.Logic.PlayerHandsManager;
using property_dealer_API.Core.Logic.PlayersManager;

namespace property_dealer_API.Core.Logic.ActionExecution.ActionHandlers.ActionSteps
{
    public class HostileTakeoverPropertySetStep : IActionStep
    {
        private readonly IPlayerManager _playerManager;
        private readonly IPlayerHandManager _playerHandManager;
        private readonly IGameRuleManager _rulesManager;
        private readonly IActionExecutor _actionExecutor;

        public HostileTakeoverPropertySetStep(
            IPlayerManager playerManager,
            IPlayerHandManager playerHandManager,
            IGameRuleManager rulesManager,
            IActionExecutor actionExecutor
        )
        {
            this._playerManager = playerManager;
            this._playerHandManager = playerHandManager;
            this._rulesManager = rulesManager;
            this._actionExecutor = actionExecutor;
        }

        public ActionResult? ProcessStep(
            Player responder,
            ActionContext currentContext,
            IActionStepService stepService,
            DialogTypeEnum? nextDialog = null
        )
        {
            if (string.IsNullOrEmpty(currentContext.TargetPlayerId))
            {
                throw new InvalidOperationException(
                    "Target player cannot be null when doing hostile takeover!"
                );
            }

            var targetPlayer = this._playerManager.GetPlayerByUserId(currentContext.TargetPlayerId);

            if (!currentContext.TargetSetColor.HasValue)
            {
                throw new ActionContextParameterNullException(
                    currentContext,
                    "TargetSetColor was found to be null for hostile takeover!"
                );
            }

            var targetPlayerSelectedPropertySet =
                this._playerHandManager.GetPropertyGroupInPlayerTableHand(
                    targetPlayer.UserId,
                    currentContext.TargetSetColor.Value
                );

            this._rulesManager.ValidateHostileTakeoverTarget(
                targetPlayerSelectedPropertySet,
                currentContext.TargetSetColor.Value
            );

            var targetPlayerHand = this._playerHandManager.GetPlayerHand(targetPlayer.UserId);
            var initiator = this._playerManager.GetPlayerByUserId(
                currentContext.ActionInitiatingPlayerId
            );

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
                stepService.BuildShieldsUpContext(currentContext, initiator, targetPlayer);
                return null;
            }

            // Execute the hostile takeover
            this._actionExecutor.ExecuteHostileTakeover(
                currentContext.ActionInitiatingPlayerId,
                targetPlayer.UserId,
                currentContext.TargetSetColor.Value
            );

            stepService.CompleteAction();

            return new ActionResult
            {
                ActionInitiatingPlayerId = currentContext.ActionInitiatingPlayerId,
                AffectedPlayerId = targetPlayer.UserId,
                ActionType = currentContext.ActionType,
                TakenPropertySet = currentContext.TargetSetColor.Value,
            };
        }
    }
}
