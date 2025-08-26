using property_dealer_API.Application.Enums;
using property_dealer_API.Application.MethodReturns;
using property_dealer_API.Core.Entities;

namespace property_dealer_API.Core.Logic.ActionExecution.ActionHandlers.ActionSteps
{
    public interface IActionStepService
    {
        ActionResult? HandleShieldsUp(
            Player responder,
            ActionContext currentContext,
            Func<ActionContext, Player, Boolean, ActionResult?>? callbackIfShieldsUpRejected
        );
        void CompleteAction();
        void BuildShieldsUpContext(ActionContext context, Player initiator, Player target);
        void SetNextDialog(
            ActionContext currentContext,
            DialogTypeEnum nextDialog,
            Player initiator,
            Player? target
        );
        void CalculateTributeAmount(ActionContext currentContext);
        void ProcessRentCardSelection(ActionContext currentContext);
    }
}
