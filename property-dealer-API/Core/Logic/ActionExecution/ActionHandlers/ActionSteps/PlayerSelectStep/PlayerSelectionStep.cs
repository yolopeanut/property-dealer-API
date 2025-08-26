using property_dealer_API.Application.Enums;
using property_dealer_API.Application.Exceptions;
using property_dealer_API.Application.MethodReturns;
using property_dealer_API.Core.Entities;
using property_dealer_API.Core.Logic.PlayersManager;

namespace property_dealer_API.Core.Logic.ActionExecution.ActionHandlers.ActionSteps.PlayerSelectStep
{
    public class PlayerSelectionStep : IActionStep
    {
        private readonly IPlayerManager _playerManager;

        public PlayerSelectionStep(IPlayerManager playerManager)
        {
            this._playerManager = playerManager;
        }

        public ActionResult? ProcessStep(
            Player responder,
            ActionContext currentContext,
            IActionStepService stepService,
            DialogTypeEnum? nextDialog = null
        )
        {
            if (!nextDialog.HasValue)
            {
                throw new InvalidOperationException(
                    "Cannot have null nextDialog for player selection"
                );
            }

            if (string.IsNullOrEmpty(currentContext.TargetPlayerId))
                throw new ActionContextParameterNullException(
                    currentContext,
                    "A target player must be selected."
                );

            if (responder.UserId != currentContext.ActionInitiatingPlayerId)
                throw new InvalidOperationException(
                    "Only the action initiator can select a player."
                );

            var initiator = this._playerManager.GetPlayerByUserId(
                currentContext.ActionInitiatingPlayerId
            );
            var targetPlayer = this._playerManager.GetPlayerByUserId(
                currentContext.TargetPlayerId!
            );

            stepService.SetNextDialog(currentContext, nextDialog.Value, initiator, targetPlayer);

            return null;
        }
    }
}
