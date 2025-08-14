using Microsoft.AspNetCore.Mvc;
using property_dealer_API.Application.Enums;
using property_dealer_API.Application.Exceptions;
using property_dealer_API.Core.Entities;
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
        public ActionTypes ActionType => ActionTypes.Tribute;

        public TributeCardHandler(
            IPlayerManager playerManager,
            IPlayerHandManager playerHandManager,
            IGameRuleManager rulesManager,
            IPendingActionManager pendingActionManager,
            IActionExecutor actionExecutor)
            : base(playerManager, playerHandManager, rulesManager, pendingActionManager, actionExecutor)
        { }

        public ActionContext? Initialize(Player initiator, Card card, List<Player> allPlayers)
        {
            if (card is not TributeCard)
            {
                throw new CardMismatchException(initiator.UserId, card.CardGuid.ToString());
            }

            var pendingAction = new PendingAction { InitiatorUserId = initiator.UserId, ActionType = ActionTypes.Tribute };
            var newActionContext = base.CreateActionContext(card.CardGuid.ToString(), DialogTypeEnum.PropertySetSelection, initiator, null, allPlayers, pendingAction);

            base.SetNextDialog(newActionContext, DialogTypeEnum.PropertySetSelection, initiator, null);
            return newActionContext;
        }

        public void ProcessResponse(Player responder, ActionContext currentContext)
        {
            switch (currentContext.DialogToOpen)
            {
                case DialogTypeEnum.PropertySetSelection:
                    // Only the initiator can select the property set.
                    if (responder.UserId != currentContext.ActionInitiatingPlayerId)
                        throw new InvalidOperationException("Only the action initiator can select a property set.");
                    if (!currentContext.TargetSetColor.HasValue)
                        throw new ActionContextParameterNullException(currentContext, "Cannot have null target set color during tribute action!");

                    this.ValidateRentTarget(currentContext.ActionInitiatingPlayerId, currentContext.TargetSetColor.Value);
                    this.ProcessPropertySetSelection(currentContext);
                    // DO NOT complete the action here, as it transitions to the payment step for others.
                    break;

                case DialogTypeEnum.PayValue:
                    // Only the targets can respond with payment.
                    if (responder.UserId == currentContext.ActionInitiatingPlayerId)
                        throw new InvalidOperationException("The action initiator cannot pay themselves rent.");

                    this.ProcessPaymentResponse(currentContext, responder);
                    // Each payment increments the counter. The manager will handle final completion.
                    base.CompleteAction();
                    break;

                case DialogTypeEnum.ShieldsUp:
                    // This case is needed if a shield is played, as it's a separate dialog.
                    base.HandleShieldsUp(responder);
                    base.CompleteAction();
                    break;

                default:
                    throw new InvalidOperationException($"Invalid state for Tribute action: {currentContext.DialogToOpen}");
            }
        }

        private void ValidateRentTarget(string actionInitiatingPlayer, PropertyCardColoursEnum targetColor)
        {
            try
            {
                var targetPlayerHand = base.PlayerHandManager.GetPropertyGroupInPlayerTableHand(actionInitiatingPlayer, targetColor);

            }
            catch (Exception)
            {

                throw new InvalidOperationException($"Cannot charge rent for {targetColor} properties because the target player doesn't own any {targetColor} properties.");
            }
        }

        private void ProcessPropertySetSelection(ActionContext currentContext)
        {
            if (!currentContext.TargetSetColor.HasValue)
                throw new ActionContextParameterNullException(currentContext, "A property set must be selected.");

            // Calculate the rent amount now that the set is known.
            this.CalculateTributeAmount(currentContext);

            var initiator = base.PlayerManager.GetPlayerByUserId(currentContext.ActionInitiatingPlayerId);

            // Set the next dialog to PayValue and target ALL OTHER players by passing a null target.
            base.SetNextDialog(currentContext, DialogTypeEnum.PayValue, initiator, null);
        }

        private void ProcessPaymentResponse(ActionContext currentContext, Player responder)
        {
            if (currentContext.OwnTargetCardId == null || !currentContext.OwnTargetCardId.Any())
                throw new ActionContextParameterNullException(currentContext, "A response (payment or shield) must be provided.");

            if (currentContext.DialogResponse == CommandResponseEnum.ShieldsUp)
            {
                var targetPlayer = base.PlayerManager.GetPlayerByUserId(responder.UserId);
                var targetPlayerHand = base.PlayerHandManager.GetPlayerHand(targetPlayer.UserId);
                if (base.RulesManager.DoesPlayerHaveShieldsUp(targetPlayer, targetPlayerHand))
                {
                    base.HandleShieldsUp(responder);
                }
                else
                {
                    throw new CardNotFoundException("Shields up was not found in players deck!");
                }
                return; // Exit after handling the shield.
            }

            base.ActionExecutor.ExecutePayment(
                currentContext.ActionInitiatingPlayerId,
                responder.UserId,
                currentContext.OwnTargetCardId
            );
        }

        private void CalculateTributeAmount(ActionContext currentContext)
        {
            if (currentContext.TargetSetColor.HasValue)
            {
                var playerTableHand = base.PlayerHandManager.GetPropertyGroupInPlayerTableHand(currentContext.ActionInitiatingPlayerId, currentContext.TargetSetColor.Value);
                currentContext.PaymentAmount = base.RulesManager.CalculateRentAmount(currentContext.TargetSetColor.Value, playerTableHand);
            }
        }
    }
}