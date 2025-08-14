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
    // This handler manages a wildcard rent card that functions identically to a standard tribute/rent card,
    // but can be used on any property set.
    public class WildCardTributeHandler : ActionHandlerBase, IActionHandler
    {
        public ActionTypes ActionType => ActionTypes.TributeWildCard;

        public WildCardTributeHandler(
            IPlayerManager playerManager,
            IPlayerHandManager playerHandManager,
            IGameRuleManager rulesManager,
            IPendingActionManager pendingActionManager,
            IActionExecutor actionExecutor)
            : base(playerManager, playerHandManager, rulesManager, pendingActionManager, actionExecutor)
        { }

        public ActionContext? Initialize(Player initiator, Card card, List<Player> allPlayers)
        {
            if (card is not CommandCard commandCard || commandCard.Command != ActionTypes.TributeWildCard)
            {
                throw new CardMismatchException(initiator.UserId, card.CardGuid.ToString());
            }

            var pendingAction = new PendingAction { InitiatorUserId = initiator.UserId, ActionType = commandCard.Command };
            var newActionContext = base.CreateActionContext(card.CardGuid.ToString(), DialogTypeEnum.PropertySetSelection, initiator, null, allPlayers, pendingAction);

            base.SetNextDialog(newActionContext, DialogTypeEnum.PropertySetSelection, initiator, null);
            return newActionContext;
        }

        public void ProcessResponse(Player responder, ActionContext currentContext)
        {
            switch (currentContext.DialogToOpen)
            {
                case DialogTypeEnum.PropertySetSelection:
                    if (responder.UserId != currentContext.ActionInitiatingPlayerId)
                        throw new InvalidOperationException("Only the action initiator can select a property set.");
                    if (!currentContext.TargetSetColor.HasValue)
                        throw new ActionContextParameterNullException(currentContext, "Cannot have null target set color during wildcard tribute action!");

                    // No need to validate color as wildcard can be applied to any color
                    this.ProcessPropertySetSelection(currentContext);
                    break;

                case DialogTypeEnum.PayValue:
                    if (responder.UserId == currentContext.ActionInitiatingPlayerId)
                        throw new InvalidOperationException("The action initiator cannot pay themselves rent.");

                    this.ProcessPaymentResponse(currentContext, responder);
                    base.CompleteAction();
                    break;

                case DialogTypeEnum.ShieldsUp:
                    base.HandleShieldsUp(responder);
                    base.CompleteAction();
                    break;

                default:
                    throw new InvalidOperationException($"Invalid state for WildCardTribute action: {currentContext.DialogToOpen}");
            }
        }

        private void ProcessPropertySetSelection(ActionContext currentContext)
        {
            if (!currentContext.TargetSetColor.HasValue)
                throw new ActionContextParameterNullException(currentContext, "A property set must be selected.");

            this.CalculateTributeAmount(currentContext);

            var initiator = base.PlayerManager.GetPlayerByUserId(currentContext.ActionInitiatingPlayerId);

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
                return;
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