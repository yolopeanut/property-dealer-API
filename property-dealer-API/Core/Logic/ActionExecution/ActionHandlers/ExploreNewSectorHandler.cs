using property_dealer_API.Application.Enums;
using property_dealer_API.Core.Entities;
using property_dealer_API.Core.Logic.GameRulesManager;
using property_dealer_API.Core.Logic.PendingActionsManager;
using property_dealer_API.Core.Logic.PlayerHandsManager;
using property_dealer_API.Core.Logic.PlayersManager;
using property_dealer_API.Models.Cards;
using property_dealer_API.Models.Enums.Cards;
using property_dealer_API.Application.Exceptions;

namespace property_dealer_API.Core.Logic.ActionExecution.ActionHandlers
{
    public class ExploreNewSectorHandler : ActionHandlerBase, IActionHandler
    {
        public ActionTypes ActionType => ActionTypes.ExploreNewSector;

        public ExploreNewSectorHandler(
            IPlayerManager playerManager,
            IPlayerHandManager playerHandManager,
            IGameRuleManager rulesManager,
            IPendingActionManager pendingActionManager,
            IActionExecutor actionExecutor)
            : base(
                  playerManager,
                  playerHandManager,
                  rulesManager,
                  pendingActionManager,
                  actionExecutor
                  )
        { }

        public ActionContext? Initialize(Player initiator, Card card, List<Player> allPlayers)
        {
            if (card is not CommandCard commandCard)
            {
                throw new CardMismatchException(initiator.UserId, card.CardGuid.ToString());
            }
            if (commandCard.Command != this.ActionType)
            {
                throw new InvalidOperationException($"Wrong command card found for Explore New Sector! Expected {ActionTypes.ExploreNewSector} but got {commandCard.Command}");
            }

            var pendingAction = new PendingAction { InitiatorUserId = initiator.UserId, ActionType = commandCard.Command };
            base.PendingActionManager.CurrPendingAction = pendingAction;

            // The core logic of the action is delegated to the ActionExecutor.
            // This handler acts as an orchestrator.
            const int cardsToDraw = 2;
            this.ActionExecutor.ExecuteDrawCards(initiator.UserId, cardsToDraw);

            // This is an immediate action that is now complete.
            // CompleteAction() correctly signals that no further steps are needed and returns null.
            return base.CompleteAction();
        }
        public void ProcessResponse(Player responder, ActionContext currentContext)
        {
            throw new InvalidOperationException("ExploreNewSector is an immediate action and does not have response steps to process.");
        }
    }
}