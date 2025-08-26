using property_dealer_API.Application.Enums;
using property_dealer_API.Application.Exceptions;
using property_dealer_API.Application.MethodReturns;
using property_dealer_API.Core.Entities;
using property_dealer_API.Core.Logic.ActionExecution.ActionHandlers.ActionSteps;
using property_dealer_API.Core.Logic.ActionExecution.ActionHandlers.ActionSteps.PaymentStep;
using property_dealer_API.Core.Logic.ActionExecution.ActionHandlers.ActionSteps.PropertySelectStep;
using property_dealer_API.Core.Logic.GameRulesManager;
using property_dealer_API.Core.Logic.PendingActionsManager;
using property_dealer_API.Core.Logic.PlayerHandsManager;
using property_dealer_API.Core.Logic.PlayersManager;
using property_dealer_API.Models.Cards;
using property_dealer_API.Models.Enums.Cards;

namespace property_dealer_API.Core.Logic.ActionExecution.ActionHandlers
{
    public class TributeCardHandler : ActionHandlerBase, IActionHandler
    {
        private readonly PaymentActionStep _paymentStep;
        private readonly RentChargePropertySetStep _propertySetStep;
        private readonly IActionStepService _stepService;

        public ActionTypes ActionType => ActionTypes.Tribute;

        public TributeCardHandler(
            IPlayerManager playerManager,
            IPlayerHandManager playerHandManager,
            IGameRuleManager rulesManager,
            IPendingActionManager pendingActionManager,
            IActionExecutor actionExecutor,
            PaymentActionStep paymentStep,
            RentChargePropertySetStep propertySetStep,
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
            this._propertySetStep = propertySetStep;
            this._stepService = stepService;
        }

        public ActionContext? Initialize(Player initiator, Card card, List<Player> allPlayers)
        {
            if (card is not TributeCard)
            {
                throw new CardMismatchException(initiator.UserId, card.CardGuid.ToString());
            }

            var pendingAction = new PendingAction
            {
                InitiatorUserId = initiator.UserId,
                ActionType = ActionTypes.Tribute,
            };
            var newActionContext = base.CreateActionContext(
                card.CardGuid.ToString(),
                DialogTypeEnum.PropertySetSelection,
                initiator,
                null,
                allPlayers,
                pendingAction
            );

            base.SetNextDialog(
                newActionContext,
                DialogTypeEnum.PropertySetSelection,
                initiator,
                null
            );
            return newActionContext;
        }

        public ActionResult? ProcessResponse(Player responder, ActionContext currentContext)
        {
            switch (currentContext.DialogToOpen)
            {
                case DialogTypeEnum.PropertySetSelection:
                    // Only the initiator can select the property set
                    if (responder.UserId != currentContext.ActionInitiatingPlayerId)
                        throw new InvalidOperationException(
                            "Only the action initiator can select a property set."
                        );

                    return this._propertySetStep.ProcessStep(
                        responder,
                        currentContext,
                        this._stepService
                    );

                case DialogTypeEnum.PayValue:
                    return this._paymentStep.ProcessStep(
                        responder,
                        currentContext,
                        this._stepService
                    );

                default:
                    throw new InvalidOperationException(
                        $"Invalid state for Tribute action: {currentContext.DialogToOpen}"
                    );
            }
        }
    }
}
