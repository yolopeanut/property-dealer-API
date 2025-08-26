using property_dealer_API.Application.Enums;
using property_dealer_API.Application.Exceptions;
using property_dealer_API.Application.MethodReturns;
using property_dealer_API.Core.Entities;
using property_dealer_API.Core.Logic.PlayerHandsManager;
using property_dealer_API.Models.Cards;
using property_dealer_API.Models.Enums.Cards;

namespace property_dealer_API.Core.Logic.ActionExecution.ActionHandlers.ActionSteps
{
    public class ForcedTradeWildcardColorSelectionStep : IActionStep
    {
        private readonly IPlayerHandManager _playerHandManager;
        private readonly IActionExecutor _actionExecutor;

        public ForcedTradeWildcardColorSelectionStep(
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
            if (String.IsNullOrEmpty(currentContext.TargetPlayerId))
                throw new ActionContextParameterNullException(
                    currentContext,
                    "TargetPlayerId is null for wildcard property selection."
                );
            if (currentContext.OwnTargetCardId == null)
                throw new ActionContextParameterNullException(
                    currentContext,
                    "OwnTargetCardId is null for wildcard property."
                );
            if (String.IsNullOrEmpty(currentContext.TargetCardId))
                throw new ActionContextParameterNullException(
                    currentContext,
                    "TargetCardId must be selected for the wildcard property."
                );

            string initiatorId = currentContext.ActionInitiatingPlayerId;
            string initiatorCardId = currentContext.OwnTargetCardId.First();
            var (cardFromInitiator, _) = this._playerHandManager.GetCardInTableHand(
                initiatorId,
                initiatorCardId
            );

            string targetId = currentContext.TargetPlayerId;
            string targetCardId = currentContext.TargetCardId;
            var (cardFromTarget, _) = this._playerHandManager.GetCardInTableHand(
                targetId,
                targetCardId
            );

            PropertyCardColoursEnum propertyGroupFromInitiator;
            PropertyCardColoursEnum propertyGroupFromTarget;

            // Determine destination colors based on which card is the wildcard and who is receiving it.
            if (cardFromTarget is SystemWildCard && responder.UserId == initiatorId)
            {
                propertyGroupFromTarget = currentContext.TargetSetColor.Value;
                propertyGroupFromInitiator = (
                    (StandardSystemCard)cardFromInitiator
                ).CardColoursEnum;
            }
            else if (cardFromInitiator is SystemWildCard && responder.UserId == targetId)
            {
                propertyGroupFromInitiator = currentContext.TargetSetColor.Value;
                propertyGroupFromTarget = ((StandardSystemCard)cardFromTarget).CardColoursEnum;
            }
            else
            {
                throw new InvalidOperationException(
                    "Could not resolve wildcard trade logic. The responder may not match the wildcard recipient."
                );
            }

            this._actionExecutor.MovePropertyBetweenTableHands(
                initiatorId: currentContext.ActionInitiatingPlayerId,
                targetId: currentContext.TargetPlayerId,
                cardIdToTake: currentContext.TargetCardId,
                colorForTakenCard: propertyGroupFromTarget,
                cardIdToGive: currentContext.OwnTargetCardId.First(),
                colorForGivenCard: propertyGroupFromInitiator
            );
            stepService.CompleteAction();

            if (currentContext.DialogResponse == CommandResponseEnum.RejectShieldsUp)
            {
                return null;
            }

            return new ActionResult
            {
                ActionInitiatingPlayerId = currentContext.ActionInitiatingPlayerId,
                AffectedPlayerId = targetId,
                ActionType = currentContext.ActionType,
                TakenCard = cardFromTarget.ToDto(),
                GivenCard = cardFromInitiator.ToDto(),
            };
        }
    }
}
