using property_dealer_API.Application.Enums;
using property_dealer_API.Application.Exceptions;
using property_dealer_API.Application.MethodReturns;
using property_dealer_API.Core.Entities;
using property_dealer_API.Core.Logic.GameRulesManager;
using property_dealer_API.Core.Logic.PendingActionsManager;
using property_dealer_API.Core.Logic.PlayerHandsManager;
using property_dealer_API.Core.Logic.PlayersManager;
using property_dealer_API.Models.Cards;
using property_dealer_API.Models.Enums.Cards;

namespace property_dealer_API.Core.Logic.ActionExecution.ActionHandlers
{
    public class TradeEmbargoHandler : ActionHandlerBase, IActionHandler
    {
        public ActionTypes ActionType => ActionTypes.TradeEmbargo;

        public TradeEmbargoHandler(
            IPlayerManager playerManager,
            IPlayerHandManager playerHandManager,
            IGameRuleManager rulesManager,
            IPendingActionManager pendingActionManager,
            IActionExecutor actionExecutor
        )
            : base(
                playerManager,
                playerHandManager,
                rulesManager,
                pendingActionManager,
                actionExecutor
            ) { }

        public ActionContext? Initialize(Player initiator, Card card, List<Player> allPlayers)
        {
            if (
                card is not CommandCard commandCard
                || commandCard.Command != ActionTypes.TradeEmbargo
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
                DialogTypeEnum.OwnHandSelection,
                initiator,
                null,
                allPlayers,
                pendingAction
            );

            base.SetNextDialog(newActionContext, DialogTypeEnum.OwnHandSelection, initiator, null);
            return newActionContext;
        }

        public ActionResult? ProcessResponse(Player responder, ActionContext currentContext)
        {
            // The flow you specified
            switch (currentContext.DialogToOpen)
            {
                case DialogTypeEnum.OwnHandSelection:
                    if (responder.UserId != currentContext.ActionInitiatingPlayerId)
                        throw new InvalidOperationException(
                            "Only the action initiator can select a rent card."
                        );
                    this.ProcessRentCardSelection(currentContext);
                    break;
                case DialogTypeEnum.PropertySetSelection:
                    if (responder.UserId != currentContext.ActionInitiatingPlayerId)
                        throw new InvalidOperationException(
                            "Only the action initiator can select a property set."
                        );

                    this.ValidateRentTarget(
                        currentContext.ActionInitiatingPlayerId,
                        currentContext
                    );
                    this.ProcessPropertySetSelection(currentContext);
                    break;
                case DialogTypeEnum.PlayerSelection:
                    if (responder.UserId != currentContext.ActionInitiatingPlayerId)
                        throw new InvalidOperationException(
                            "Only the action initiator can select a player."
                        );
                    this.ProcessPlayerSelection(currentContext);
                    break;
                case DialogTypeEnum.PayValue:
                    if (responder.UserId == currentContext.ActionInitiatingPlayerId)
                        throw new InvalidOperationException(
                            "The action initiator cannot pay themselves rent."
                        );
                    this.ProcessPaymentAndCompleteAction(currentContext, responder);
                    this.RemoveTributeCardFromPlayerHand(currentContext);
                    break;
                // Shields up is already part of pay value dialog on the UI
                default:
                    throw new InvalidOperationException(
                        $"Invalid state for TradeEmbargo action: {currentContext.DialogToOpen}"
                    );
            }

            return null;
        }

        private ActionResult? ProcessPaymentAndCompleteAction(
            ActionContext currentContext,
            Player responder,
            Boolean _ = true
        )
        {
            this.ProcessPaymentResponse(currentContext, responder);
            base.CompleteAction();
            return null;
        }

        private void RemoveTributeCardFromPlayerHand(ActionContext currentContext)
        {
            if (
                currentContext.SupportingCardIdToRemove == null
                || currentContext.SupportingCardIdToRemove.Count <= 0
            )
            {
                throw new ActionContextParameterNullException(
                    currentContext,
                    "Cannot remove tribute card from action initiating player when OwnTargetCardId is null"
                );
            }

            base.PlayerHandManager.RemoveFromPlayerHand(
                currentContext.ActionInitiatingPlayerId,
                currentContext.SupportingCardIdToRemove.First()
            );
        }

        private void ProcessRentCardSelection(ActionContext currentContext)
        {
            if (currentContext.OwnTargetCardId == null || !currentContext.OwnTargetCardId.Any())
                throw new ActionContextParameterNullException(
                    currentContext,
                    "A rent card must be selected."
                );

            var rentCardId = currentContext.OwnTargetCardId.First();
            var rentCard = base.PlayerHandManager.GetCardFromPlayerHandById(
                currentContext.ActionInitiatingPlayerId,
                rentCardId
            );
            if (rentCard is not TributeCard)
            {
                throw new CardMismatchException(
                    currentContext.ActionInitiatingPlayerId,
                    rentCardId
                );
            }

            var initiator = base.PlayerManager.GetPlayerByUserId(
                currentContext.ActionInitiatingPlayerId
            );
            base.SetNextDialog(
                currentContext,
                DialogTypeEnum.PropertySetSelection,
                initiator,
                null
            );
        }

        private void ValidateRentTarget(string actionInitiatingPlayer, ActionContext currentContext)
        {
            if (currentContext.OwnTargetCardId == null || !currentContext.OwnTargetCardId.Any())
                throw new ActionContextParameterNullException(
                    currentContext,
                    "A rent card must be selected."
                );

            if (!currentContext.TargetSetColor.HasValue)
                throw new ActionContextParameterNullException(
                    currentContext,
                    "Cannot have null target set color during tribute action!"
                );

            var tributeCardId = currentContext.OwnTargetCardId.First();
            var targetColor = currentContext.TargetSetColor.Value;

            try
            {
                var tributeCard = base.PlayerHandManager.GetCardFromPlayerHandById(
                    currentContext.ActionInitiatingPlayerId,
                    tributeCardId
                );
                base.RulesManager.ValidateTributeCardTarget(targetColor, tributeCard);
                var targetPlayerHand = base.PlayerHandManager.GetPropertyGroupInPlayerTableHand(
                    actionInitiatingPlayer,
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

        private void ProcessPropertySetSelection(ActionContext currentContext)
        {
            if (!currentContext.TargetSetColor.HasValue)
                throw new ActionContextParameterNullException(
                    currentContext,
                    "A property set must be selected."
                );

            var initiator = base.PlayerManager.GetPlayerByUserId(
                currentContext.ActionInitiatingPlayerId
            );
            base.SetNextDialog(currentContext, DialogTypeEnum.PlayerSelection, initiator, null);
        }

        private void ProcessPlayerSelection(ActionContext currentContext)
        {
            if (string.IsNullOrEmpty(currentContext.TargetPlayerId))
                throw new ActionContextParameterNullException(
                    currentContext,
                    "A target player must be selected."
                );

            var initiator = base.PlayerManager.GetPlayerByUserId(
                currentContext.ActionInitiatingPlayerId
            );
            var targetPlayer = base.PlayerManager.GetPlayerByUserId(currentContext.TargetPlayerId);
            var targetPlayerHand = base.PlayerHandManager.GetPlayerHand(targetPlayer.UserId);

            this.CalculateTributeAmount(currentContext);
            base.SetNextDialog(currentContext, DialogTypeEnum.PayValue, initiator, targetPlayer);
        }

        private ActionResult? ProcessPaymentResponse(
            ActionContext currentContext,
            Player responder,
            Boolean _ = true
        )
        {
            // Check if the response was a "Shields Up" card.
            // This assumes a shield play consists of submitting just the single shield card.
            if (currentContext.DialogResponse == CommandResponseEnum.ShieldsUp)
            {
                var targetPlayer = base.PlayerManager.GetPlayerByUserId(
                    currentContext.TargetPlayerId!
                );
                var targetPlayerHand = base.PlayerHandManager.GetPlayerHand(targetPlayer.UserId);
                var initiator = base.PlayerManager.GetPlayerByUserId(
                    currentContext.ActionInitiatingPlayerId
                );

                if (base.RulesManager.DoesPlayerHaveShieldsUp(targetPlayer, targetPlayerHand))
                {
                    return base.HandleShieldsUp(
                        responder,
                        currentContext,
                        this.ProcessPaymentResponse
                    );
                }
                else
                {
                    throw new CardNotFoundException("Shields up was not found in players deck!");
                }
            }

            if (currentContext.OwnTargetCardId == null || !currentContext.OwnTargetCardId.Any())
            {
                var playerHand = base.PlayerHandManager.GetPlayerTableHand(responder.UserId);
                var moneyHand = base.PlayerHandManager.GetPlayerMoneyHand(responder.UserId);
                if (!base.RulesManager.IsPlayerBroke(playerHand, moneyHand))
                {
                    throw new ActionContextParameterNullException(
                        currentContext,
                        $"A response (payment or shield) must be provided for {currentContext.ActionType}!"
                    );
                }

                return null;
            }

            base.ActionExecutor.ExecutePayment(
                currentContext.ActionInitiatingPlayerId,
                responder.UserId,
                currentContext.OwnTargetCardId
            );

            return null;
        }

        private void CalculateTributeAmount(ActionContext currentContext)
        {
            if (currentContext.OwnTargetCardId?.Count > 0 && currentContext.TargetSetColor.HasValue)
            {
                var playerTableHand = base.PlayerHandManager.GetPropertyGroupInPlayerTableHand(
                    currentContext.ActionInitiatingPlayerId,
                    currentContext.TargetSetColor.Value
                );

                // Calculate the base rent amount and then double it
                var baseRentAmount = base.RulesManager.CalculateRentAmount(
                    currentContext.TargetSetColor.Value,
                    playerTableHand
                );
                currentContext.PaymentAmount = baseRentAmount * 2; // Double the rent for Trade Embargo
            }
        }
    }
}
