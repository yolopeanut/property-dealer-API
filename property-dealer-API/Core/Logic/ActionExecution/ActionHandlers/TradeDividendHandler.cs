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
    public class TradeDividendHandler : ActionHandlerBase, IActionHandler
    {
        public ActionTypes ActionType => ActionTypes.TradeDividend;

        public TradeDividendHandler(
            IPlayerManager playerManager,
            IPlayerHandManager playerHandManager,
            IGameRuleManager rulesManager,
            IPendingActionManager pendingActionManager,
            IActionExecutor actionExecutor)
            : base(playerManager, playerHandManager, rulesManager, pendingActionManager, actionExecutor)
        { }

        public ActionContext? Initialize(Player initiator, Card card, List<Player> allPlayers)
        {
            if (card is not CommandCard commandCard || commandCard.Command != ActionTypes.TradeDividend)
            {
                throw new CardMismatchException(initiator.UserId, card.CardGuid.ToString());
            }

            var pendingAction = new PendingAction { InitiatorUserId = initiator.UserId, ActionType = commandCard.Command };
            // The context is created with a PayValue dialog. The RulesManager will identify
            // all other players as targets.
            var newActionContext = base.CreateActionContext(card.CardGuid.ToString(), DialogTypeEnum.PayValue, initiator, null, allPlayers, pendingAction);

            base.SetNextDialog(newActionContext, DialogTypeEnum.PayValue, initiator, null);
            return newActionContext;
        }

        public void ProcessResponse(Player responder, ActionContext currentContext)
        {
            // For this action, the responder MUST be one of the targets, not the initiator.
            if (responder.UserId == currentContext.ActionInitiatingPlayerId)
                throw new InvalidOperationException("The action initiator cannot respond to their own Trade Dividend.");

            switch (currentContext.DialogToOpen)
            {
                case DialogTypeEnum.PayValue:
                    this.ProcessPaymentResponse(currentContext, responder);
                    base.CompleteAction();
                    break;

                default:
                    throw new InvalidOperationException($"Invalid state for TradeDividend action: {currentContext.DialogToOpen}");
            }
        }

        private void ProcessPaymentResponse(ActionContext currentContext, Player responder, Boolean _ = true)
        {
            // The player must have submitted cards, either as payment or as a 'Shields Up'.
            if (currentContext.OwnTargetCardId == null || !currentContext.OwnTargetCardId.Any())
            {
                throw new ActionContextParameterNullException(currentContext, $"A response (payment or shield) must be provided for {currentContext.ActionType}!");
            }

            // Check if the response was a "Shields Up" card.
            // This assumes a shield play consists of submitting just the single shield card.
            if (currentContext.DialogResponse == CommandResponseEnum.ShieldsUp)
            {
                var targetPlayer = base.PlayerManager.GetPlayerByUserId(currentContext.TargetPlayerId!);
                var targetPlayerHand = base.PlayerHandManager.GetPlayerHand(targetPlayer.UserId);
                var initiator = base.PlayerManager.GetPlayerByUserId(currentContext.ActionInitiatingPlayerId);

                if (base.RulesManager.DoesPlayerHaveShieldsUp(targetPlayer, targetPlayerHand))
                {
                    base.HandleShieldsUp(responder, currentContext, this.ProcessPaymentResponse);
                }
                else
                {
                    throw new CardNotFoundException("Shields up was not found in players deck!");
                }
                return;
            }

            // If it wasn't a shield, it's a payment.
            // The initiator receives payment from the responder.
            base.ActionExecutor.ExecutePayment(
                currentContext.ActionInitiatingPlayerId,
                responder.UserId,
                currentContext.OwnTargetCardId
            );
        }
    }
}