using property_dealer_API.Core.Entities;

namespace property_dealer_API.Core.Logic.PendingActionsManager
{
    public interface IPendingActionManager
    {
        Boolean CanClearPendingAction { get; set; }
        PendingAction CurrPendingAction { get; set; }
        void ClearPendingAction();
        Boolean AddResponseToQueue(Player player, ActionContext actionContext);
        void IncrementCurrentStep();
    }
}