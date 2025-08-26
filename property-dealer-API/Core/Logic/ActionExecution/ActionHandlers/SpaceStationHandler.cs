using property_dealer_API.Application.Enums;
using property_dealer_API.Application.Exceptions;
using property_dealer_API.Application.MethodReturns;
using property_dealer_API.Core.Entities;
using property_dealer_API.Core.Logic.ActionExecution.ActionHandlers.ActionSteps;
using property_dealer_API.Core.Logic.ActionExecution.ActionHandlers.ActionSteps.PropertySelectStep;
using property_dealer_API.Core.Logic.GameRulesManager;
using property_dealer_API.Core.Logic.PendingActionsManager;
using property_dealer_API.Core.Logic.PlayerHandsManager;
using property_dealer_API.Core.Logic.PlayersManager;
using property_dealer_API.Models.Cards;
using property_dealer_API.Models.Enums.Cards;

namespace property_dealer_API.Core.Logic.ActionExecution.ActionHandlers
{
    public class SpaceStationHandler : ActionHandlerBase, IActionHandler
    {
        private readonly BuildingPlacementStep _buildingPlacementStep;
        private readonly IActionStepService _stepService;

        public ActionTypes ActionType => ActionTypes.SpaceStation;

        public SpaceStationHandler(
            IPlayerManager playerManager,
            IPlayerHandManager playerHandManager,
            IGameRuleManager rulesManager,
            IPendingActionManager pendingActionManager,
            IActionExecutor actionExecutor,
            BuildingPlacementStep buildingPlacementStep,
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
            this._buildingPlacementStep = buildingPlacementStep;
            this._stepService = stepService;
        }

        public ActionContext? Initialize(Player initiator, Card card, List<Player> allPlayers)
        {
            if (
                card is not CommandCard commandCard
                || commandCard.Command != ActionTypes.SpaceStation
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
            // Only the player who played the card can choose where to build
            if (responder.UserId != currentContext.ActionInitiatingPlayerId)
                throw new InvalidOperationException(
                    "Only the action initiator can choose which set to build on."
                );

            switch (currentContext.DialogToOpen)
            {
                case DialogTypeEnum.PropertySetSelection:
                    return this._buildingPlacementStep.ProcessStep(
                        responder,
                        currentContext,
                        this._stepService
                    );

                default:
                    throw new InvalidOperationException(
                        $"Invalid state for SpaceStation action: {currentContext.DialogToOpen}"
                    );
            }
        }
    }
}
