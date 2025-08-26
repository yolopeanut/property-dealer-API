using property_dealer_API.Application.Enums;
using property_dealer_API.Application.Exceptions;
using property_dealer_API.Application.MethodReturns;
using property_dealer_API.Core.Entities;
using property_dealer_API.Core.Logic.GameRulesManager;
using property_dealer_API.Core.Logic.PlayerHandsManager;
using property_dealer_API.Models.Cards;
using property_dealer_API.Models.Enums.Cards;

namespace property_dealer_API.Core.Logic.ActionExecution.ActionHandlers.ActionSteps.PropertySelectStep
{
    public class BuildingPlacementStep : IActionStep
    {
        private readonly IPlayerHandManager _playerHandManager;
        private readonly IGameRuleManager _rulesManager;
        private readonly IActionExecutor _actionExecutor;

        public BuildingPlacementStep(
            IPlayerHandManager playerHandManager,
            IGameRuleManager rulesManager,
            IActionExecutor actionExecutor
        )
        {
            this._playerHandManager = playerHandManager;
            this._rulesManager = rulesManager;
            this._actionExecutor = actionExecutor;
        }

        public ActionResult? ProcessStep(
            Player responder,
            ActionContext currentContext,
            IActionStepService stepService,
            DialogTypeEnum? nextDialog = null
        )
        {
            // Only the player who played the card can choose where to build
            if (responder.UserId != currentContext.ActionInitiatingPlayerId)
                throw new InvalidOperationException(
                    "Only the action initiator can choose which set to build on."
                );

            if (!currentContext.TargetSetColor.HasValue)
            {
                throw new ActionContextParameterNullException(
                    currentContext,
                    "TargetSetColor must be provided when building."
                );
            }

            var playerTableHand = this._playerHandManager.GetPropertyGroupInPlayerTableHand(
                currentContext.ActionInitiatingPlayerId,
                currentContext.TargetSetColor.Value
            );

            // Validate placement based on action type
            this.ValidatePlacement(currentContext, playerTableHand);

            this._actionExecutor.ExecuteBuildOnSet(
                currentContext.ActionInitiatingPlayerId,
                currentContext.CardId,
                currentContext.TargetSetColor.Value
            );

            stepService.CompleteAction();
            return null;
        }

        private void ValidatePlacement(ActionContext currentContext, List<Card> playerTableHand)
        {
            switch (currentContext.ActionType)
            {
                case ActionTypes.SpaceStation:
                    this._rulesManager.ValidateSpaceStationPlacement(
                        playerTableHand,
                        currentContext.TargetSetColor!.Value
                    );
                    break;

                case ActionTypes.Starbase:
                    this._rulesManager.ValidateStarbasePlacement(
                        playerTableHand,
                        currentContext.TargetSetColor!.Value
                    );
                    break;

                default:
                    throw new InvalidOperationException(
                        $"BuildingPlacementStep does not support action type: {currentContext.ActionType}"
                    );
            }
        }
    }
}
