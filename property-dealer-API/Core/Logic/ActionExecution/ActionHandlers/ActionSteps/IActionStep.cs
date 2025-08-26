using property_dealer_API.Application.Enums;
using property_dealer_API.Application.MethodReturns;
using property_dealer_API.Core.Entities;

namespace property_dealer_API.Core.Logic.ActionExecution.ActionHandlers.ActionSteps
{
    public interface IActionStep
    {
        ActionResult? ProcessStep(
            Player responder,
            ActionContext currentContext,
            IActionStepService stepService,
            DialogTypeEnum? nextDialog = null
        );
    }
}
