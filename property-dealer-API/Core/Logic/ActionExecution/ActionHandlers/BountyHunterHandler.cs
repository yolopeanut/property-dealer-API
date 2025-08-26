using property_dealer_API.Application.Enums;
using property_dealer_API.Application.Exceptions;
using property_dealer_API.Application.MethodReturns;
using property_dealer_API.Core.Entities;
using property_dealer_API.Core.Logic.ActionExecution.ActionHandlers.ActionSteps;
using property_dealer_API.Core.Logic.ActionExecution.ActionHandlers.ActionSteps.PaymentStep;
using property_dealer_API.Core.Logic.ActionExecution.ActionHandlers.ActionSteps.PlayerSelectStep;
using property_dealer_API.Core.Logic.GameRulesManager;
using property_dealer_API.Core.Logic.PendingActionsManager;
using property_dealer_API.Core.Logic.PlayerHandsManager;
using property_dealer_API.Core.Logic.PlayersManager;
using property_dealer_API.Models.Cards;
using property_dealer_API.Models.Enums.Cards;

namespace property_dealer_API.Core.Logic.ActionExecution.ActionHandlers
{
    public class BountyHunterHandler : ActionHandlerBase, IActionHandler
    {
        private readonly PaymentActionStep _paymentStep;
        private readonly PlayerSelectionStep _playerSelectionStep;
        private readonly IActionStepService _stepService;
        public ActionTypes ActionType => ActionTypes.BountyHunter;

        public BountyHunterHandler(
            IPlayerManager playerManager,
            IPlayerHandManager playerHandManager,
            IGameRuleManager rulesManager,
            IPendingActionManager pendingActionManager,
            IActionExecutor actionExecutor,
            PaymentActionStep paymentStep,
            PlayerSelectionStep playerSelectionStep,
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
            this._playerSelectionStep = playerSelectionStep;
            this._stepService = stepService;
        }

        public ActionContext? Initialize(Player initiator, Card card, List<Player> allPlayers)
        {
            if (
                card is not CommandCard commandCard
                || commandCard.Command != ActionTypes.BountyHunter
            )
            {
                throw new CardMismatchException(initiator.UserId, card.CardGuid.ToString());
            }

            var pendingAction = new PendingAction
            {
                InitiatorUserId = initiator.UserId,
                ActionType = commandCard.Command,
            };
            var newActionContext = base.CreateActionContext(
                card.CardGuid.ToString(),
                DialogTypeEnum.PlayerSelection,
                initiator,
                null,
                allPlayers,
                pendingAction
            );

            base.SetNextDialog(newActionContext, DialogTypeEnum.PlayerSelection, initiator, null);
            return newActionContext;
        }

        public ActionResult? ProcessResponse(Player responder, ActionContext currentContext)
        {
            switch (currentContext.DialogToOpen)
            {
                case DialogTypeEnum.PlayerSelection:
                    this._playerSelectionStep.ProcessStep(
                        responder,
                        currentContext,
                        _stepService,
                        DialogTypeEnum.PayValue
                    );
                    break;

                case DialogTypeEnum.PayValue:
                    return this._paymentStep.ProcessStep(
                        responder,
                        currentContext,
                        this._stepService
                    );

                default:
                    throw new InvalidOperationException(
                        $"Invalid state for BountyHunter action: {currentContext.DialogToOpen}"
                    );
            }

            return null;
        }
    }
}
