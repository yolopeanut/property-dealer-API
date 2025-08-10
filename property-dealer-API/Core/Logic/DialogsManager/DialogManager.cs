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
                // For the scenario that the user does shields up (can come from pays value-button or shields up dialog)
                if (context.DialogResponse == CommandResponseEnum.ShieldsUp)
                {
                    this._actionExecutionManager.HandleShieldsUpResponse(player, newActionContext);
                    continue;
                }

                // Handle all the responses
                switch (context.DialogToOpen)
                {
                    #region No new dialogs
                    case DialogTypeEnum.PayValue:
                        //This can only have ok or shields up, since shields up handled, no further dialog.
                        this._actionExecutionManager.HandlePayValueResponse(player, newActionContext);
                        break;
                    case DialogTypeEnum.WildcardColor:
                        // just do processing on wildcard color
                        this._actionExecutionManager.HandleWildCardResponse(player, newActionContext);
                        break;
                    #endregion

                    #region Might have new dialog
                    case DialogTypeEnum.PropertySetSelection:
                        this._actionExecutionManager.HandlePropertySetSelectionResponse(player, newActionContext);
                        newActionContexts.Add(newActionContext);
                        break;
                    case DialogTypeEnum.TableHandSelector:
                        this._actionExecutionManager.HandleTableHandSelectorResponse(player, newActionContext);
                        newActionContexts.Add(newActionContext);
                        // prompt shields up next dialog or wildcard
                        break;
                    #endregion

                    #region Only new target dialogs
                    case DialogTypeEnum.PlayerSelection:
                        this._actionExecutionManager.HandlePlayerSelectionResponse(player, newActionContext);
                        newActionContexts.Add(newActionContext);
                        break;
                    case DialogTypeEnum.OwnHandSelection:
                        this._actionExecutionManager.HandleOwnHandSelectionResponse(player, newActionContext);
                        newActionContexts.Add(newActionContext);
                        break;

                        #endregion
                }
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
