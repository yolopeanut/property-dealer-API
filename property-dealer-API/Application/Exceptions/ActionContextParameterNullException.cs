using property_dealer_API.Core;

namespace property_dealer_API.Application.Exceptions
{
    public class ActionContextParameterNullException : Exception
    {
        public ActionContextParameterNullException(ActionContext actionContext, string msg) : base($"{msg}:{actionContext}") { }
    }
}
