using property_dealer_API.Application.Enums;
using property_dealer_API.Application.Exceptions;
using property_dealer_API.Application.MethodReturns;
using property_dealer_API.Core.Entities;
using property_dealer_API.Core.Logic.GameRulesManager;
using property_dealer_API.Core.Logic.PlayerHandsManager;
using property_dealer_API.Core.Logic.PlayersManager;
using property_dealer_API.Models.Enums.Cards;

namespace property_dealer_API.Core.Logic.ActionExecution.ActionHandlers.ActionSteps.PropertySelectStep
{
    public class RentChargePropertySetStep : IActionStep
    {
        private readonly IPlayerManager _playerManager;
        private readonly IPlayerHandManager _playerHandManager;
        private readonly IGameRuleManager _rulesManager;

        public RentChargePropertySetStep(
            IPlayerManager playerManager,
            IPlayerHandManager playerHandManager,
            IGameRuleManager rulesManager
        )
        {
            this._playerManager = playerManager;
            this._playerHandManager = playerHandManager;
            this._rulesManager = rulesManager;
        }

        public ActionResult? ProcessStep(
            Player responder,
            ActionContext currentContext,
            IActionStepService stepService,
            DialogTypeEnum? nextDialog = null
        )
        {
            this.ValidateRentTarget(currentContext);

            if (!currentContext.TargetSetColor.HasValue)
                throw new ActionContextParameterNullException(
                    currentContext,
                    "A property set must be selected."
                );

            var initiator = this._playerManager.GetPlayerByUserId(
                currentContext.ActionInitiatingPlayerId
            );

            // Determine next dialog based on action type or explicit parameter
            var nextDialogType = nextDialog ?? this.GetDefaultNextDialog(currentContext.ActionType);

            stepService.CalculateTributeAmount(currentContext);

            stepService.SetNextDialog(currentContext, nextDialogType, initiator, null);
            return null;
        }

        private DialogTypeEnum GetDefaultNextDialog(ActionTypes actionType)
        {
            return actionType switch
            {
                ActionTypes.TradeEmbargo => DialogTypeEnum.PlayerSelection,
                ActionTypes.Tribute => DialogTypeEnum.PayValue,
                _ => DialogTypeEnum.PayValue, // Default to charging all players
            };
        }

        private void ValidateRentTarget(ActionContext currentContext)
        {
            if (!currentContext.TargetSetColor.HasValue)
                throw new ActionContextParameterNullException(
                    currentContext,
                    "Cannot have null target set color during rent action!"
                );

            var targetColor = currentContext.TargetSetColor.Value;

            // Determine which card to validate based on action type
            string cardIdToValidate = this.GetCardIdForValidation(currentContext);

            try
            {
                var rentCard = this._playerHandManager.GetCardFromPlayerHandById(
                    currentContext.ActionInitiatingPlayerId,
                    cardIdToValidate
                );
                this._rulesManager.ValidateTributeCardTarget(targetColor, rentCard);
                var targetPlayerHand = this._playerHandManager.GetPropertyGroupInPlayerTableHand(
                    currentContext.ActionInitiatingPlayerId,
                    targetColor
                );
            }
            catch (Exception)
            {
                throw new InvalidOperationException(
                    $"Cannot charge rent for {targetColor} properties because the target player doesn't own any {targetColor} properties."
                );
            }
        }

        private string GetCardIdForValidation(ActionContext currentContext)
        {
            return currentContext.ActionType switch
            {
                ActionTypes.TradeEmbargo => currentContext.OwnTargetCardId?.FirstOrDefault()
                    ?? throw new ActionContextParameterNullException(
                        currentContext,
                        "A rent card must be selected."
                    ),
                ActionTypes.Tribute => currentContext.CardId,
                _ => currentContext.CardId,
            };
        }
    }
}
