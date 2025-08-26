using property_dealer_API.Application.Enums;
using property_dealer_API.Application.Exceptions;
using property_dealer_API.Application.MethodReturns;
using property_dealer_API.Core.Entities;
using property_dealer_API.Core.Logic.ActionExecution.ActionHandlers.ActionSteps;
using property_dealer_API.Core.Logic.ActionExecution.ActionHandlers.ActionSteps.OwnHandSelectStep;
using property_dealer_API.Core.Logic.ActionExecution.ActionHandlers.ActionSteps.PaymentStep;
using property_dealer_API.Core.Logic.ActionExecution.ActionHandlers.ActionSteps.PlayerSelectStep;
using property_dealer_API.Core.Logic.ActionExecution.ActionHandlers.ActionSteps.PropertySelectStep;
using property_dealer_API.Core.Logic.GameRulesManager;
using property_dealer_API.Core.Logic.PendingActionsManager;
using property_dealer_API.Core.Logic.PlayerHandsManager;
using property_dealer_API.Core.Logic.PlayersManager;
using property_dealer_API.Models.Cards;
using property_dealer_API.Models.Enums.Cards;

namespace property_dealer_API.Core.Logic.ActionExecution.ActionHandlers
{
    public class TradeEmbargoHandler : ActionHandlerBase, IActionHandler
    {
        private readonly PaymentActionStep _paymentStep;
        private readonly PlayerSelectionStep _playerSelectionStep;
        private readonly RentChargePropertySetStep _propertySetStep;
        private readonly OwnHandSelectionStep _ownHandStep;
        private readonly IActionStepService _stepService;

        public ActionTypes ActionType => ActionTypes.TradeEmbargo;

        public TradeEmbargoHandler(
            IPlayerManager playerManager,
            IPlayerHandManager playerHandManager,
            IGameRuleManager rulesManager,
            IPendingActionManager pendingActionManager,
            IActionExecutor actionExecutor,
            PaymentActionStep paymentStep,
            PlayerSelectionStep playerSelectionStep,
            RentChargePropertySetStep propertySetStep,
            OwnHandSelectionStep ownHandStep,
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
            this._propertySetStep = propertySetStep;
            this._ownHandStep = ownHandStep;
            this._stepService = stepService;
        }

        public ActionContext? Initialize(Player initiator, Card card, List<Player> allPlayers)
        {
            if (
                card is not CommandCard commandCard
                || commandCard.Command != ActionTypes.TradeEmbargo
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
                DialogTypeEnum.OwnHandSelection,
                initiator,
                null,
                allPlayers,
                pendingAction
            );

            base.SetNextDialog(newActionContext, DialogTypeEnum.OwnHandSelection, initiator, null);
            return newActionContext;
        }

        public ActionResult? ProcessResponse(Player responder, ActionContext currentContext)
        {
            switch (currentContext.DialogToOpen)
            {
                case DialogTypeEnum.OwnHandSelection:
                    if (responder.UserId != currentContext.ActionInitiatingPlayerId)
                        throw new InvalidOperationException(
                            "Only the action initiator can select a rent card."
                        );
                    return this._ownHandStep.ProcessStep(
                        responder,
                        currentContext,
                        this._stepService,
                        DialogTypeEnum.PropertySetSelection
                    );

                case DialogTypeEnum.PropertySetSelection:
                    if (responder.UserId != currentContext.ActionInitiatingPlayerId)
                        throw new InvalidOperationException(
                            "Only the action initiator can select a property set."
                        );
                    return this._propertySetStep.ProcessStep(
                        responder,
                        currentContext,
                        this._stepService,
                        DialogTypeEnum.PlayerSelection
                    );

                case DialogTypeEnum.PlayerSelection:
                    this._playerSelectionStep.ProcessStep(
                        responder,
                        currentContext,
                        this._stepService,
                        DialogTypeEnum.PayValue
                    );
                    this._stepService.CalculateTributeAmount(currentContext);
                    return null;

                case DialogTypeEnum.PayValue:
                    var processResult = this._paymentStep.ProcessStep(
                        responder,
                        currentContext,
                        this._stepService
                    );
                    this.RemoveTributeCardFromPlayerHand(currentContext);
                    return processResult;

                default:
                    throw new InvalidOperationException(
                        $"Invalid state for TradeEmbargo action: {currentContext.DialogToOpen}"
                    );
            }
        }

        private void RemoveTributeCardFromPlayerHand(ActionContext currentContext)
        {
            if (
                currentContext.SupportingCardIdToRemove == null
                || currentContext.SupportingCardIdToRemove.Count <= 0
            )
            {
                throw new ActionContextParameterNullException(
                    currentContext,
                    "Cannot remove tribute card from action initiating player when OwnTargetCardId is null"
                );
            }

            base.PlayerHandManager.RemoveFromPlayerHand(
                currentContext.ActionInitiatingPlayerId,
                currentContext.SupportingCardIdToRemove.First()
            );
        }
    }
}
