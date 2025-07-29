using property_dealer_API.Application.Exceptions;
using property_dealer_API.Core.Entities;

namespace property_dealer_API.Core.Logic.PendingActionsManager
{
    public class PendingActionManager
    {
        private PendingAction? _currPendingAction { get; set; }
        public Boolean CanClearPendingAction { get; set; }

        public PendingAction CurrPendingAction
        {
            get
            {
                if (this._currPendingAction == null)
                {
                    throw new PendingActionNotFoundException(null);
                }

                return _currPendingAction;
            }
            set
            {
                //if (this._currPendingAction != null)
                //{
                //    throw new InvalidOperationException("Cannot set new pending action when current one has not ended");
                //}

                this._currPendingAction = value;
            }
        }

        public void ClearPendingAction()
        {
            this._currPendingAction = null;
            this.CanClearPendingAction = false;
        }

        public Boolean AddResponseToQueue(Player player, ActionContext actionContext)
        {
            this.CurrPendingAction.ResponseQueue.Enqueue((player, actionContext));

            if (!this.CurrPendingAction.IsWaitingForResponses)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void IncrementCurrentStep()
        {
            this.CurrPendingAction.CurrentStep += 1;
        }
    }
}
