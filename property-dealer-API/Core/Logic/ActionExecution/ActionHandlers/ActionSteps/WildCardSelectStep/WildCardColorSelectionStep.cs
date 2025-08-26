using property_dealer_API.Application.Enums;
using property_dealer_API.Application.Exceptions;
using property_dealer_API.Application.MethodReturns;
using property_dealer_API.Core.Entities;

namespace property_dealer_API.Core.Logic.ActionExecution.ActionHandlers.ActionSteps.WildCardSelectStep
{
    public class WildCardColorSelectionStep : IActionStep
    {
        private readonly IActionExecutor _actionExecutor;

        public WildCardColorSelectionStep(IActionExecutor actionExecutor)
        {
            this._actionExecutor = actionExecutor;
        }

        public ActionResult? ProcessStep(
            Player responder,
            ActionContext currentContext,
            IActionStepService stepService,
            DialogTypeEnum? nextDialog = null
        )
        {
            // Only the initiator of the card play can respond
            if (responder.UserId != currentContext.ActionInitiatingPlayerId)
                throw new InvalidOperationException(
                    "Only the player who played the SystemWildCard can choose its color."
                );

            if (currentContext.TargetSetColor == null)
            {
                throw new ActionContextParameterNullException(
                    currentContext,
                    "TargetSetColor must be provided when choosing a wildcard color."
                );
            }

            this._actionExecutor.ExecutePlayToTable(
                currentContext.ActionInitiatingPlayerId,
                currentContext.CardId,
                currentContext.TargetSetColor.Value
            );

            stepService.CompleteAction();
            return null;
        }
    }
}
