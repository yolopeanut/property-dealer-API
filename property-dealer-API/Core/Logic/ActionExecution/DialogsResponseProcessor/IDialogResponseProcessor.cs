using property_dealer_API.Core.Entities;

namespace property_dealer_API.Core.Logic.ActionExecution
{
    public interface IDialogResponseProcessor
    {
        void HandlePayValueResponse(Player player, ActionContext actionContext);
        void HandlePlayerSelectionResponse(Player player, ActionContext actionContext);
        void HandlePropertySetSelectionResponse(Player player, ActionContext actionContext);
        void HandleTableHandSelectorResponse(Player player, ActionContext actionContext);
        void HandleWildCardResponse(Player player, ActionContext actionContext);
        void HandleShieldsUpResponse(Player player, ActionContext actionContext);
    }
}