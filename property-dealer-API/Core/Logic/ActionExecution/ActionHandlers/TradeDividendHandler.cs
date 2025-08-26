using property_dealer_API.Application.Enums;
using property_dealer_API.Application.Exceptions;
using property_dealer_API.Application.MethodReturns;
using property_dealer_API.Core.Entities;
using property_dealer_API.Core.Logic.ActionExecution.ActionHandlers.ActionSteps;
using property_dealer_API.Core.Logic.ActionExecution.ActionHandlers.ActionSteps.PaymentStep;
using property_dealer_API.Core.Logic.GameRulesManager;
using property_dealer_API.Core.Logic.PendingActionsManager;
using property_dealer_API.Core.Logic.PlayerHandsManager;
using property_dealer_API.Core.Logic.PlayersManager;
using property_dealer_API.Models.Cards;
using property_dealer_API.Models.Enums.Cards;

namespace property_dealer_API.Core.Logic.ActionExecution.ActionHandlers
{
    public class TradeDividendHandler : ActionHandlerBase, IActionHandler
    {
        private readonly PaymentActionStep _paymentStep;
        private readonly IActionStepService _stepService;
        public ActionTypes ActionType => ActionTypes.TradeDividend;

        public TradeDividendHandler(
            IPlayerManager playerManager,
            IPlayerHandManager playerHandManager,
            IGameRuleManager rulesManager,
            IPendingActionManager pendingActionManager,
            IActionExecutor actionExecutor,
            PaymentActionStep paymentStep,
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
            this._paymentStep = paymentStep;
            this._stepService = stepService;
        }

        public ActionContext? Initialize(Player initiator, Card card, List<Player> allPlayers)
        {
            if (
                card is not CommandCard commandCard
                || commandCard.Command != ActionTypes.TradeDividend
            )
            {
                throw new CardMismatchException(initiator.UserId, card.CardGuid.ToString());
            }

            var pendingAction = new PendingAction
            {
                InitiatorUserId = initiator.UserId,
                ActionType = commandCard.Command,
            };
            // The context is created with a PayValue dialog. The RulesManager will identify
            // all other players as targets.
            var newActionContext = base.CreateActionContext(
                card.CardGuid.ToString(),
                DialogTypeEnum.PayValue,
                initiator,
                null,
                allPlayers,
                pendingAction
            );

            base.SetNextDialog(newActionContext, DialogTypeEnum.PayValue, initiator, null);
            return newActionContext;
        }

        public ActionResult? ProcessResponse(Player responder, ActionContext currentContext)
        {
            // For this action, the responder MUST be one of the targets, not the initiator.
            if (responder.UserId == currentContext.ActionInitiatingPlayerId)
                throw new InvalidOperationException(
                    "The action initiator cannot respond to their own Trade Dividend."
                );

            switch (currentContext.DialogToOpen)
            {
                case DialogTypeEnum.PayValue:
                    return this._paymentStep.ProcessStep(
                        responder,
                        currentContext,
                        this._stepService
                    );
                default:
                    throw new InvalidOperationException(
                        $"Invalid state for TradeDividend action: {currentContext.DialogToOpen}"
                    );
            }
        }
    }
}
