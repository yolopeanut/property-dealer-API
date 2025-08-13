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
using System.Numerics;

namespace property_dealer_API.Core.Logic.ActionExecution.ActionHandlers
{
    public class PirateRaidHandler : ActionHandlerBase, IActionHandler
    {
        public ActionTypes ActionType => ActionTypes.PirateRaid;

        public PirateRaidHandler(
            IPlayerManager playerManager,
            IPlayerHandManager playerHandManager,
            IGameRuleManager rulesManager,
            IPendingActionManager pendingActionManager,
            IActionExecutor actionExecutor)
            : base(playerManager, playerHandManager, rulesManager, pendingActionManager, actionExecutor)
        { }

        public ActionContext? Initialize(Player initiator, Card card, List<Player> allPlayers)
        {
            if (card is not CommandCard commandCard || commandCard.Command != ActionTypes.PirateRaid)
            {
                throw new CardMismatchException(initiator.UserId, card.CardGuid.ToString());
            }

            var pendingAction = new PendingAction { InitiatorUserId = initiator.UserId, ActionType = commandCard.Command };
            var newActionContext = base.CreateActionContext(card.CardGuid.ToString(), DialogTypeEnum.PlayerSelection, initiator, null, allPlayers, pendingAction);

            base.SetNextDialog(newActionContext, DialogTypeEnum.PlayerSelection, initiator, null);
            return newActionContext;
        }

        public void ProcessResponse(Player responder, ActionContext currentContext)
        {
            switch (currentContext.DialogToOpen)
            {
                case DialogTypeEnum.PlayerSelection:
                    this.ProcessPlayerSelection(currentContext);
                    break;

                case DialogTypeEnum.TableHandSelector:
                    this.ProcessTableHandSelection(currentContext, responder);
                    break;

                case DialogTypeEnum.WildcardColor:
                    base.CompleteAction();
                    break;
                case DialogTypeEnum.ShieldsUp:
                    base.HandleShieldsUp(responder);
                    break;

                default:
                    throw new InvalidOperationException($"Invalid state for PirateRaid action: {currentContext.DialogToOpen}");
            }
        }

        private void ProcessPlayerSelection(ActionContext currentContext)
        {
            var initiator = base.PlayerManager.GetPlayerByUserId(currentContext.ActionInitiatingPlayerId);
            var targetPlayer = base.PlayerManager.GetPlayerByUserId(currentContext.TargetPlayerId!);
            base.SetNextDialog(currentContext, DialogTypeEnum.TableHandSelector, initiator, targetPlayer);
        }

        private void ProcessTableHandSelection(ActionContext currentContext, Player responder)
        {
            var pendingAction = base.PendingActionManager.CurrPendingAction;
            if (pendingAction == null) throw new InvalidOperationException("No pending action found.");

            if (string.IsNullOrEmpty(currentContext.TargetPlayerId))
                throw new ActionContextParameterNullException(currentContext, "TargetPlayerId cannot be null for TableHandSelector.");
            if (currentContext.TargetCardId == null || !currentContext.TargetCardId.Any())
                throw new ActionContextParameterNullException(currentContext, $"TargetCardId was found null in {pendingAction.ActionType}!");

            var targetPlayer = base.PlayerManager.GetPlayerByUserId(currentContext.TargetPlayerId);
            var targetPlayerHand = base.PlayerHandManager.GetPlayerHand(targetPlayer.UserId);
            var (targetCard, _) = base.PlayerHandManager.GetCardInTableHand(targetPlayer.UserId, currentContext.TargetCardId);

            this.ValidateActionPrerequisites(pendingAction, targetPlayer, targetCard);

            bool specialConditionHandled = this.TryHandleSpecialConditions(currentContext, responder, targetPlayer, targetCard, targetPlayerHand);

            if (!specialConditionHandled)
            {
                this.ExecuteNormalAction(currentContext, targetPlayer, pendingAction);
                base.CompleteAction();
            }
        }

        private void ValidateActionPrerequisites(PendingAction pendingAction, Player targetPlayer, Card targetCard)
        {
            if (targetCard is not StandardSystemCard systemCard) return;

            var targetPlayerTableHand = base.PlayerHandManager.GetPropertyGroupInPlayerTableHand(targetPlayer.UserId, systemCard.CardColoursEnum);

            base.RulesManager.ValidatePirateRaidTarget(targetPlayerTableHand, systemCard.CardColoursEnum);
        }

        private bool TryHandleSpecialConditions(ActionContext currentContext, Player initiator, Player targetPlayer, Card targetCard, List<Card> targetPlayerHand)
        {
            if (base.RulesManager.DoesPlayerHaveShieldsUp(targetPlayer, targetPlayerHand))
            {
                base.BuildShieldsUpContext(currentContext, initiator, targetPlayer);
                return true;
            }

            if (base.RulesManager.IsCardSystemWildCard(targetCard))
            {
                this.BuildWildCardMovementContext(currentContext, initiator); // Initiator benefits
                return true;
            }
            return false; // No special conditions were met
        }

        private void ExecuteNormalAction(ActionContext currentContext, Player targetPlayer, PendingAction pendingAction)
        {
            if (currentContext.TargetCardId == null || !currentContext.TargetCardId.Any())
            {
                throw new ActionContextParameterNullException(currentContext, "TargetCardId is null for normal execution.");
            }

            base.ActionExecutor.ExecutePirateRaid(
                currentContext.ActionInitiatingPlayerId,
                targetPlayer.UserId,
                currentContext.TargetCardId // Card to take
            );
        }

        private void BuildWildCardMovementContext(ActionContext context, Player beneficiary)
        {
            base.SetNextDialog(context, DialogTypeEnum.WildcardColor, beneficiary, null); // Wildcard choice is seen by the beneficiary
        }
    }
}