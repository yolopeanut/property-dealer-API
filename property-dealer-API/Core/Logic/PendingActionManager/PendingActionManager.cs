using property_dealer_API.Core.Entities;

namespace property_dealer_API.Core.Logic.PendingActionManager
{
    public class PendingActionManager
    {
        private PendingAction? _currPendingAction { get; set; }

        public PendingAction? CurrPendingAction
        {
            get => _currPendingAction;
            set
            {
                if (_currPendingAction != null)
                {
                    throw new InvalidOperationException("Cannot set new pending action when current one has not ended");
                }

                _currPendingAction = value;
            }
        }

        public void ClearPendingAction()
        {
            this._currPendingAction = null;
        }
    }
}
