


using property_dealer_API.Core.Entities;
using property_dealer_API.Models.Cards;
using property_dealer_API.Models.Enums.Cards;

namespace property_dealer_API.Core.Logic.ActionExecution
{
    public class ActionExecutionManager : IActionExecutionManager
    {
        private readonly IActionHandlerResolver _actionHandlerResolver;

        public ActionExecutionManager(
            IActionHandlerResolver actionHandlerResolver)
        {
            this._actionHandlerResolver = actionHandlerResolver;
        }

        public ActionContext? ExecuteAction(string userId, Card card, Player currentUser, List<Player> allPlayers)
        {
            IActionHandler actionHandler;
            switch (card)
            {
                case CommandCard commandCard:
                    actionHandler = _actionHandlerResolver.GetHandler(commandCard.Command);
                    break;
                case SystemWildCard wildCard:
                    actionHandler = _actionHandlerResolver.GetHandler(ActionTypes.SystemWildCard);
                    break;
                case TributeCard tributeCard:
                    actionHandler = _actionHandlerResolver.GetHandler(ActionTypes.Tribute);
                    break;
                //case TributeWildCard tributeWildCard:
                //    actionHandler = _actionHandlerResolver.GetHandler(ActionTypes.TributeWildCard);
                //    break;
                default:
                    throw new InvalidOperationException($"Unsupported card type: {card.GetType().Name}");
            }
            return actionHandler.Initialize(currentUser, card, allPlayers);
        }

        public void HandleDialogResponse(Player responder, ActionContext actionContext)
        {
            IActionHandler actionHandler;
            actionHandler = _actionHandlerResolver.GetHandler(actionContext.ActionType);
            actionHandler.ProcessResponse(responder, actionContext);
        }
    }
}
