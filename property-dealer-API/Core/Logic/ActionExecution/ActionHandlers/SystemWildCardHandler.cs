using property_dealer_API.Application.Enums;
using property_dealer_API.Application.Exceptions;
using property_dealer_API.Application.MethodReturns;
using property_dealer_API.Core.Entities;
using property_dealer_API.Core.Logic.ActionExecution.ActionHandlers.ActionSteps;
using property_dealer_API.Core.Logic.ActionExecution.ActionHandlers.ActionSteps.WildCardSelectStep;
using property_dealer_API.Core.Logic.GameRulesManager;
using property_dealer_API.Core.Logic.PendingActionsManager;
using property_dealer_API.Core.Logic.PlayerHandsManager;
using property_dealer_API.Core.Logic.PlayersManager;
using property_dealer_API.Models.Cards;
using property_dealer_API.Models.Enums.Cards;

namespace property_dealer_API.Core.Logic.ActionExecution.ActionHandlers
{
    public class SystemWildCardHandler : ActionHandlerBase, IActionHandler
    {
        private readonly WildCardColorSelectionStep _wildCardColorSelectionStep;
        private readonly IActionStepService _stepService;

        public ActionTypes ActionType => ActionTypes.SystemWildCard;

        public SystemWildCardHandler(
            IPlayerManager playerManager,
            IPlayerHandManager playerHandManager,
            IGameRuleManager rulesManager,
            IPendingActionManager pendingActionManager,
            IActionExecutor actionExecutor,
            WildCardColorSelectionStep wildCardColorSelectionStep,
            IActionStepService stepService
        )
            : base(
                playerManager,
                playerHandManager,
                rulesManager,
                pendingActionManager,
                actionExecutor
            )
        {
            this._wildCardColorSelectionStep = wildCardColorSelectionStep;
            this._stepService = stepService;
        }

        public ActionContext? Initialize(Player initiator, Card card, List<Player> allPlayers)
        {
            if (card is not SystemWildCard)
            {
                throw new CardMismatchException(initiator.UserId, card.CardGuid.ToString());
            }

            var pendingAction = new PendingAction
            {
                InitiatorUserId = initiator.UserId,
                ActionType = ActionTypes.SystemWildCard,
            };
            var newActionContext = base.CreateActionContext(
                card.CardGuid.ToString(),
                DialogTypeEnum.WildcardColor,
                initiator,
                null,
                allPlayers,
                pendingAction
            );

            base.SetNextDialog(newActionContext, DialogTypeEnum.WildcardColor, initiator, null);
            return newActionContext;
        }

        public ActionResult? ProcessResponse(Player responder, ActionContext currentContext)
        {
            // Only the initiator of the card play can respond
            if (responder.UserId != currentContext.ActionInitiatingPlayerId)
                throw new InvalidOperationException(
                    "Only the player who played the SystemWildCard can choose its color."
                );

            switch (currentContext.DialogToOpen)
            {
                case DialogTypeEnum.WildcardColor:
                    return this._wildCardColorSelectionStep.ProcessStep(
                        responder,
                        currentContext,
                        this._stepService
                    );

                default:
                    throw new InvalidOperationException(
                        $"Invalid state for SystemWildCard action: {currentContext.DialogToOpen}"
                    );
            }
        }
    }
}
