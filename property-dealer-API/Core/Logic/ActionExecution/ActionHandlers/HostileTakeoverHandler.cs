using property_dealer_API.Application.Enums;
using property_dealer_API.Application.Exceptions;
using property_dealer_API.Application.MethodReturns;
using property_dealer_API.Core.Entities;
using property_dealer_API.Core.Logic.ActionExecution.ActionHandlers.ActionSteps;
using property_dealer_API.Core.Logic.ActionExecution.ActionHandlers.ActionSteps.PlayerSelectStep;
using property_dealer_API.Core.Logic.GameRulesManager;
using property_dealer_API.Core.Logic.PendingActionsManager;
using property_dealer_API.Core.Logic.PlayerHandsManager;
using property_dealer_API.Core.Logic.PlayersManager;
using property_dealer_API.Models.Cards;
using property_dealer_API.Models.Enums.Cards;

namespace property_dealer_API.Core.Logic.ActionExecution.ActionHandlers
{
    public class HostileTakeoverHandler : ActionHandlerBase, IActionHandler
    {
        private readonly PlayerSelectionStep _playerSelectionStep;
        private readonly HostileTakeoverPropertySetStep _propertySetStep;
        private readonly IActionStepService _stepService;

        public ActionTypes ActionType => ActionTypes.HostileTakeover;

        public HostileTakeoverHandler(
            IPlayerManager playerManager,
            IPlayerHandManager playerHandManager,
            IGameRuleManager rulesManager,
            IPendingActionManager pendingActionManager,
            IActionExecutor actionExecutor,
            PlayerSelectionStep playerSelectionStep,
            HostileTakeoverPropertySetStep propertySetStep,
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
            this._playerSelectionStep = playerSelectionStep;
            this._propertySetStep = propertySetStep;
            this._stepService = stepService;
        }

        public ActionContext? Initialize(Player initiator, Card card, List<Player> allPlayers)
        {
            if (
                card is not CommandCard commandCard
                || commandCard.Command != ActionTypes.HostileTakeover
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
                    return this._playerSelectionStep.ProcessStep(
                        responder,
                        currentContext,
                        this._stepService,
                        DialogTypeEnum.PropertySetSelection
                    );

                case DialogTypeEnum.PropertySetSelection:
                    return this._propertySetStep.ProcessStep(
                        responder,
                        currentContext,
                        this._stepService
                    );

                case DialogTypeEnum.ShieldsUp:
                    return this._stepService.HandleShieldsUp(
                        responder,
                        currentContext,
                        (context, player, _) =>
                            this._propertySetStep.ProcessStep(player, context, this._stepService)
                    );

                default:
                    throw new InvalidOperationException(
                        $"Invalid state for HostileTakeover action: {currentContext.DialogToOpen}"
                    );
            }
        }
    }
}
