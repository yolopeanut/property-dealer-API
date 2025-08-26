using property_dealer_API.Application.Enums;
using property_dealer_API.Application.Exceptions;
using property_dealer_API.Application.MethodReturns;
using property_dealer_API.Core.Entities;
using property_dealer_API.Core.Logic.PlayerHandsManager;
using property_dealer_API.Core.Logic.PlayersManager;

namespace property_dealer_API.Core.Logic.ActionExecution.ActionHandlers.ActionSteps.OwnHandSelectStep
{
    public class OwnHandSelectionStep : IActionStep
    {
        private readonly IPlayerManager _playerManager;
        private readonly IPlayerHandManager _playerHandManager;

        public OwnHandSelectionStep(
            IPlayerManager playerManager,
            IPlayerHandManager playerHandManager
        )
        {
            this._playerManager = playerManager;
            this._playerHandManager = playerHandManager;
        }

        public ActionResult? ProcessStep(
            Player responder,
            ActionContext currentContext,
            IActionStepService stepService,
            DialogTypeEnum? nextDialog = null
        )
        {
            // Validate that a rent card was selected
            if (currentContext.OwnTargetCardId == null || !currentContext.OwnTargetCardId.Any())
            {
                throw new ActionContextParameterNullException(
                    currentContext,
                    "A rent card must be selected from your hand."
                );
            }

            // Validate that only the action initiator can select from their own hand
            if (responder.UserId != currentContext.ActionInitiatingPlayerId)
            {
                throw new InvalidOperationException(
                    "Only the action initiator can select cards from their own hand."
                );
            }

            // Get the selected rent card to validate it exists in the player's hand
            var selectedCardId = currentContext.OwnTargetCardId.First();
            try
            {
                var selectedCard = this._playerHandManager.GetCardFromPlayerHandById(
                    currentContext.ActionInitiatingPlayerId,
                    selectedCardId
                );

                // Store the card for later removal (TradeEmbargo removes it after payment)
                currentContext.SupportingCardIdToRemove = new List<string> { selectedCardId };
            }
            catch (Exception)
            {
                throw new ActionContextParameterNullException(
                    currentContext,
                    $"Selected card {selectedCardId} was not found in player's hand."
                );
            }

            // Process the rent card selection (this sets up the context for property set selection)
            stepService.ProcessRentCardSelection(currentContext);

            // Move to the next dialog (typically PropertySetSelection for rent actions)
            if (nextDialog.HasValue)
            {
                var initiator = this._playerManager.GetPlayerByUserId(
                    currentContext.ActionInitiatingPlayerId
                );
                stepService.SetNextDialog(currentContext, nextDialog.Value, initiator, null);
            }

            return null;
        }
    }
}