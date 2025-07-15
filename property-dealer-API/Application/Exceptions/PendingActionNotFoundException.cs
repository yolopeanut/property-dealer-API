using property_dealer_API.Core;

namespace property_dealer_API.Application.Exceptions
{
    public class PendingActionNotFoundException : Exception
    {
        public PendingActionNotFoundException(ActionContext? actionContext) : base($"{actionContext}: Tried accessing pending action but it was not found.") { }
    }
}
