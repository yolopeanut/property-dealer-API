using property_dealer_API.Application.MethodReturns;
using property_dealer_API.Core.Entities;

namespace property_dealer_API.Core.Logic.DialogsManager
{
    public interface IDialogManager
    {
        DialogProcessingResult ProcessPendingResponses(ActionContext actionContext);
        DialogProcessingResult RegisterActionResponse(Player player, ActionContext actionContext);
    }
}