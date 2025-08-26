using property_dealer_API.Application.Enums;
using property_dealer_API.Application.Exceptions;
using property_dealer_API.Application.MethodReturns;
using property_dealer_API.Core.Entities;
using property_dealer_API.Core.Logic.PlayerHandsManager;

namespace property_dealer_API.Core.Logic.ActionExecution.ActionHandlers.ActionSteps
{
    public class PirateRaidWildcardStep : IActionStep
    {
        private readonly IPlayerHandManager _playerHandManager;
        private readonly IActionExecutor _actionExecutor;

        public PirateRaidWildcardStep(
            IPlayerHandManager playerHandManager,
            IActionExecutor actionExecutor
        )
        {
            this._playerHandManager = playerHandManager;
            this._actionExecutor = actionExecutor;
        }

        public ActionResult? ProcessStep(
            Player responder,
            ActionContext currentContext,
            IActionStepService stepService,
            DialogTypeEnum? nextDialog = null
        )
        {
            if (!currentContext.TargetSetColor.HasValue)
                throw new ActionContextParameterNullException(
                    currentContext,
                    "A color must be selected for the wildcard property."
                );
            if (currentContext.TargetPlayerId == null)
                throw new ActionContextParameterNullException(
                    currentContext,
                    "TargetPlayerId cannot be null when doing a pirate raid on a wildcard step."
                );
            if (currentContext.TargetCardId == null)
                throw new ActionContextParameterNullException(
                    currentContext,
                    "TargetCardId cannot be null when doing a pirate raid on a wildcard step"
                );

            var (cardFromTarget, _) = this._playerHandManager.GetCardInTableHand(
                currentContext.TargetPlayerId!,
                currentContext.TargetCardId!
            );

            this._actionExecutor.MovePropertyBetweenTableHands(
                initiatorId: currentContext.ActionInitiatingPlayerId,
                targetId: currentContext.TargetPlayerId,
                cardIdToTake: currentContext.TargetCardId,
                colorForTakenCard: currentContext.TargetSetColor.Value
            );

            stepService.CompleteAction();

            return new ActionResult
            {
                ActionInitiatingPlayerId = currentContext.ActionInitiatingPlayerId,
                AffectedPlayerId = currentContext.TargetPlayerId,
                ActionType = currentContext.ActionType,
                TakenCard = cardFromTarget.ToDto(),
            };
        }
    }
}
