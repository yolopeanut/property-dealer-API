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
    public class HostileTakeoverHandler : ActionHandlerBase, IActionHandler
    {
        public ActionTypes ActionType => ActionTypes.HostileTakeover;

        public HostileTakeoverHandler(
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
            if (card is not CommandCard commandCard)
            {
                throw new CardMismatchException(initiator.UserId, card.CardGuid.ToString());
            }

            if (commandCard.Command != ActionTypes.HostileTakeover)
            {
                throw new InvalidOperationException(
                    $"Wrong command card found for hostile takeover! {commandCard.Command}"
                );
            }

            var pendingAction = new PendingAction
            {
                InitiatorUserId = initiator.UserId,
                ActionType = commandCard.Command,
            };
            var newActionContext = base.CreateActionContext(
                card.CardGuid.ToString(),
                DialogTypeEnum.PlayerSelection,
                initiator,
                null,
                allPlayers,
                pendingAction
            );
            base.SetNextDialog(newActionContext, DialogTypeEnum.PlayerSelection, initiator, null);

            return newActionContext;
        }

        public ActionResult? ProcessResponse(Player responder, ActionContext currentContext)
        {
            var pendingAction = base.PendingActionManager.CurrPendingAction;
            if (pendingAction == null)
                throw new InvalidOperationException("No pending action found.");

            switch (currentContext.DialogToOpen)
            {
                case DialogTypeEnum.PlayerSelection:
                    this.ProcessPlayerSelection(currentContext);
                    break;

                case DialogTypeEnum.PropertySetSelection:
                    return this.HandlePropertySetSelection(currentContext, responder);

                case DialogTypeEnum.ShieldsUp:
                    return base.HandleShieldsUp(
                        responder,
                        currentContext,
                        this.HandlePropertySetSelection
                    );

                default:
                    throw new InvalidOperationException(
                        $"Invalid state for HostileTakeover action: {currentContext.DialogToOpen}"
                    );
            }
            return null;
        }

        private void ProcessPlayerSelection(ActionContext currentContext)
        {
            var initiator = base.PlayerManager.GetPlayerByUserId(
                currentContext.ActionInitiatingPlayerId
            );
            var targetPlayer = base.PlayerManager.GetPlayerByUserId(currentContext.TargetPlayerId!);
            base.SetNextDialog(
                currentContext,
                DialogTypeEnum.PropertySetSelection,
                initiator,
                targetPlayer
            );
        }

        private ActionResult? HandlePropertySetSelection(
            ActionContext currentContext,
            Player responder,
            Boolean includeShieldsUpChecking = true
        )
        {
            var initiator = base.PlayerManager.GetPlayerByUserId(
                currentContext.ActionInitiatingPlayerId
            );
            Player? targetPlayer = null;
            if (currentContext.TargetPlayerId != null)
            {
                targetPlayer = base.PlayerManager.GetPlayerByUserId(currentContext.TargetPlayerId);
            }

            if (targetPlayer == null)
            {
                throw new InvalidOperationException(
                    "Target player cannot be null when doing hostile takeover!"
                );
            }
            if (!currentContext.TargetSetColor.HasValue)
            {
                throw new ActionContextParameterNullException(
                    currentContext,
                    "TargetSetColor was found to be null for hostile takeover!"
                );
            }
            var targetPlayerSelectedPropertySet =
                base.PlayerHandManager.GetPropertyGroupInPlayerTableHand(
                    targetPlayer.UserId,
                    currentContext.TargetSetColor.Value
                );

            base.RulesManager.ValidateHostileTakeoverTarget(
                targetPlayerSelectedPropertySet,
                currentContext.TargetSetColor.Value
            );

            var targetPlayerHand = base.PlayerHandManager.GetPlayerHand(targetPlayer.UserId);

            bool specialConditionHandled = false;

            if (includeShieldsUpChecking)
            {
                specialConditionHandled = this.TryHandleSpecialConditions(
                    currentContext,
                    initiator,
                    targetPlayer,
                    targetPlayerHand
                );
            }

            if (!specialConditionHandled)
            {
                base.ActionExecutor.ExecuteHostileTakeover(
                    currentContext.ActionInitiatingPlayerId,
                    targetPlayer.UserId,
                    currentContext.TargetSetColor!.Value
                );

                base.CompleteAction();

                if (currentContext.DialogResponse == CommandResponseEnum.RejectShieldsUp)
                {
                    return null;
                }

                return new ActionResult
                {
                    ActionInitiatingPlayerId = currentContext.ActionInitiatingPlayerId,
                    AffectedPlayerId = targetPlayer.UserId,
                    ActionType = currentContext.ActionType,
                    TakenPropertySet = currentContext.TargetSetColor!.Value,
                };
            }

            return null;
        }

        private bool TryHandleSpecialConditions(
            ActionContext currentContext,
            Player initiator,
            Player targetPlayer,
            List<Card> targetPlayerHand
        )
        {
            if (base.RulesManager.DoesPlayerHaveShieldsUp(targetPlayer, targetPlayerHand))
            {
                base.BuildShieldsUpContext(currentContext, initiator, targetPlayer);
                return true;
            }
            return false; // No special conditions were met
        }
    }
}
