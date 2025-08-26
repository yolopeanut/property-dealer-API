using property_dealer_API.Application.Enums;
using property_dealer_API.Application.Exceptions;
using property_dealer_API.Application.MethodReturns;
using property_dealer_API.Core.Entities;
using property_dealer_API.Core.Logic.GameRulesManager;
using property_dealer_API.Core.Logic.PlayerHandsManager;

namespace property_dealer_API.Core.Logic.ActionExecution.ActionHandlers.ActionSteps.PaymentStep
{
    public class PaymentActionStep : IActionStep
    {
        private readonly IPlayerHandManager _playerHandManager;
        private readonly IGameRuleManager _rulesManager;
        private readonly IActionExecutor _actionExecutor;

        public PaymentActionStep(
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
            if (responder.UserId == currentContext.ActionInitiatingPlayerId)
                throw new InvalidOperationException(
                    "The action initiator cannot pay themselves rent."
                );

            // Check if the response was a "Shields Up" card.
            if (currentContext.DialogResponse == CommandResponseEnum.ShieldsUp)
            {
                var responderHand = this._playerHandManager.GetPlayerHand(responder.UserId);
                if (this._rulesManager.DoesPlayerHaveShieldsUp(responder, responderHand))
                {
                    return stepService.HandleShieldsUp(
                        responder,
                        currentContext,
                        (context, player, _) => this.ProcessStep(player, context, stepService)
                    );
                }
                else
                {
                    throw new CardNotFoundException("Shields up was not found in players deck!");
                }
            }

            // The player must have submitted cards, either as payment or as a 'Shields Up'.
            if (currentContext.OwnTargetCardId == null || !currentContext.OwnTargetCardId.Any())
            {
                var playerHand = this._playerHandManager.GetPlayerTableHand(responder.UserId);
                var moneyHand = this._playerHandManager.GetPlayerMoneyHand(responder.UserId);
                if (!this._rulesManager.IsPlayerBroke(playerHand, moneyHand))
                {
                    throw new ActionContextParameterNullException(
                        currentContext,
                        $"A response (payment or shield) must be provided for {currentContext.ActionType}!"
                    );
                }

                return null;
            }

            // If it wasn't a shield, it's a payment.
            // The initiator receives payment from the responder.
            this._actionExecutor.ExecutePayment(
                currentContext.ActionInitiatingPlayerId,
                responder.UserId,
                currentContext.OwnTargetCardId
            );

            stepService.CompleteAction();
            return null;
        }
    }
}
