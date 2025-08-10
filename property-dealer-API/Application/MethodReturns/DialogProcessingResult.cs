using property_dealer_API.Core;

namespace property_dealer_API.Application.MethodReturns
{
    public class DialogProcessingResult
    {
        public List<ActionContext>? NewActionContexts { get; set; }
        public bool ShouldClearPendingAction { get; set; }
        public ActionContext? ActionContext { get; set; }
    }

}
