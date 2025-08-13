using property_dealer_API.Application.Enums;
using property_dealer_API.Core.Entities;
using property_dealer_API.Core.Logic.GameRulesManager;
using property_dealer_API.Core.Logic.PendingActionsManager;
using property_dealer_API.Core.Logic.PlayerHandsManager;
using property_dealer_API.Core.Logic.PlayersManager;
using property_dealer_API.Models.Cards;
using property_dealer_API.Models.Enums.Cards;
using System.Collections.Concurrent;

namespace property_dealer_API.Core.Logic.ActionExecution.ActionHandlers
{
    public abstract class ActionHandlerBase
    {
        protected readonly IPlayerManager PlayerManager;
        protected readonly IPlayerHandManager PlayerHandManager;
        protected readonly IGameRuleManager RulesManager;
        protected readonly IPendingActionManager PendingActionManager;
        protected readonly IActionExecutor ActionExecutor;

        protected ActionHandlerBase(
            IPlayerManager playerManager,
            IPlayerHandManager playerHandManager,
            IGameRuleManager rulesManager,
            IPendingActionManager pendingActionManager,
            IActionExecutor actionExecutor)
        {
            PlayerManager = playerManager;
            PlayerHandManager = playerHandManager;
            RulesManager = rulesManager;
            PendingActionManager = pendingActionManager;
            ActionExecutor = actionExecutor;
        }
        protected ActionContext CreateActionContext(string cardId, DialogTypeEnum dialogType, Player currentUser, Player? targetUser, List<Player> allPlayers, PendingAction pendingAction)
        {
            var dialogTargetList = this.RulesManager.IdentifyWhoSeesDialog(currentUser, targetUser, allPlayers, dialogType);
            var amountToPay = this.RulesManager.GetPaymentAmount(pendingAction.ActionType);
            pendingAction.RequiredResponders = new ConcurrentBag<Player>(dialogTargetList);

            // Set the pending action
            this.PendingActionManager.CurrPendingAction = pendingAction;

            return new ActionContext
            {
                CardId = cardId,
                ActionInitiatingPlayerId = currentUser.UserId,
                ActionType = pendingAction.ActionType,
                DialogTargetList = dialogTargetList,
                DialogToOpen = dialogType,
                PaymentAmount = amountToPay
            };
        }

        protected void SetNextDialog(ActionContext currentContext, DialogTypeEnum nextDialog, Player initiator, Player? target)
        {
            var allPlayers = PlayerManager.GetAllPlayers();
            currentContext.DialogToOpen = nextDialog;
            currentContext.DialogTargetList = RulesManager.IdentifyWhoSeesDialog(initiator, target, allPlayers, nextDialog);

            var pendingAction = PendingActionManager.CurrPendingAction;
            if (pendingAction != null)
            {
                pendingAction.RequiredResponders = new ConcurrentBag<Player>(currentContext.DialogTargetList);
            }
        }

        protected ActionContext? CompleteAction()
        {
            PendingActionManager.IncrementProcessedActions();
            return null;
        }

        protected void HandleShieldsUp(Player responder)
        {
            var playerHand = this.PlayerHandManager.GetPlayerHand(responder.UserId);

            var shieldsUpCard = playerHand.FirstOrDefault(card => card is CommandCard commandCard && commandCard.Command == ActionTypes.ShieldsUp);
            if (shieldsUpCard != null)
            {
                this.ActionExecutor.HandleRemoveFromHand(responder.UserId, shieldsUpCard.CardGuid.ToString());
            }

            this.CompleteAction();
        }
        protected void BuildShieldsUpContext(ActionContext context, Player initiator, Player target)
        {
            this.SetNextDialog(context, DialogTypeEnum.ShieldsUp, initiator, target);
        }
    }
}