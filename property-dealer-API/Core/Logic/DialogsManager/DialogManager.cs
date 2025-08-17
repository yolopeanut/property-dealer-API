using property_dealer_API.Application.Enums;
using property_dealer_API.Application.MethodReturns;
using property_dealer_API.Core.Entities;
using property_dealer_API.Core.Logic.ActionExecution;
using property_dealer_API.Core.Logic.PendingActionsManager;

namespace property_dealer_API.Core.Logic.DialogsManager
{

    public class DialogManager : IDialogManager
    {
        private readonly IActionExecutionManager _actionExecutionManager;
        private readonly IPendingActionManager _pendingActionManager;

        public DialogManager(
            IActionExecutionManager actionExecutionManager,
            IPendingActionManager pendingActionManager)
        {
            this._actionExecutionManager = actionExecutionManager;
            this._pendingActionManager = pendingActionManager;
        }

        public DialogProcessingResult ProcessPendingResponses(ActionContext actionContext)
        {
            DialogProcessingResult dialogProcessingResult = new() { ShouldClearPendingAction = false, ActionContext = actionContext };

            var newActionContexts = new List<ActionContext>();
            var pendingAction = this._pendingActionManager.CurrPendingAction;
            var allResponses = new List<(Player Player, ActionContext Context)>();

            // Drain the entire queue first
            while (pendingAction.ResponseQueue.TryDequeue(out var response))
            {
                allResponses.Add(response);
            }

            foreach (var (player, context) in allResponses)
            {
                var newActionContext = context.Clone();
                this._actionExecutionManager.HandleDialogResponse(player, newActionContext);
                newActionContexts.Add(newActionContext);
            }

            // Clear pending action after finishing processing all responses
            if (this._pendingActionManager.CanClearPendingAction)
            {
                dialogProcessingResult.ShouldClearPendingAction = true;
            }
            dialogProcessingResult.NewActionContexts = newActionContexts;

            return dialogProcessingResult;
        }

        public DialogProcessingResult RegisterActionResponse(Player player, ActionContext actionContext)
        {
            var shouldProcess = this._pendingActionManager.AddResponseToQueue(player, actionContext);

            if (shouldProcess)
            {
                var result = this.ProcessPendingResponses(actionContext);

                if (result.ShouldClearPendingAction)
                {
                    this._pendingActionManager.ClearPendingAction();
                }

                return result;
            }
            return new DialogProcessingResult { ShouldClearPendingAction = false, ActionContext = actionContext };
        }
    }


}
