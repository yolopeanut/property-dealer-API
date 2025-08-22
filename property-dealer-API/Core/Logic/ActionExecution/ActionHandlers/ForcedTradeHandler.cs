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
    public class ForcedTradeHandler : ActionHandlerBase, IActionHandler
    {
        public ActionTypes ActionType => ActionTypes.ForcedTrade;

        public ForcedTradeHandler(
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

        /// <summary>
        /// Validates the action and sets up the initial dialog for selecting a player to trade with.
        /// </summary>
        public ActionContext? Initialize(Player initiator, Card card, List<Player> allPlayers)
        {
            if (
                card is not CommandCard commandCard
                || commandCard.Command != ActionTypes.ForcedTrade
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
                DialogTypeEnum.PlayerSelection,
                initiator,
                null,
                allPlayers,
                pendingAction
            );

            // The first step is always PlayerSelection for this action.
            base.SetNextDialog(newActionContext, DialogTypeEnum.PlayerSelection, initiator, null);

            return newActionContext;
        }

        /// <summary>
        /// Manages the multi-step process of a Forced Trade action.
        /// </summary>
        public ActionResult? ProcessResponse(Player responder, ActionContext currentContext)
        {
            var isNotActionInitiatingPlayer =
                responder.UserId != currentContext.ActionInitiatingPlayerId;
            var isNotTargetPlayer = responder.UserId != currentContext.TargetPlayerId;

            // For this action, only the initiator should be responding after the first step unless for shields up.
            if (isNotActionInitiatingPlayer && isNotTargetPlayer)
            {
                throw new InvalidOperationException(
                    "Only the action initiator and target player can respond during a Forced Trade."
                );
            }

            var initiator = base.PlayerManager.GetPlayerByUserId(
                currentContext.ActionInitiatingPlayerId
            );

            switch (currentContext.DialogToOpen)
            {
                case DialogTypeEnum.PlayerSelection:
                    this.ProcessPlayerSelection(currentContext);
                    break;

                case DialogTypeEnum.TableHandSelector:
                    return this.ProcessTableHandSelection(currentContext, responder);

                case DialogTypeEnum.ShieldsUp:
                    return base.HandleShieldsUp(
                        responder,
                        currentContext,
                        this.ProcessTableHandSelection
                    );

                case DialogTypeEnum.WildcardColor:
                    return this.ProcessWildcardColorSelection(currentContext, responder);

                default:
                    throw new InvalidOperationException(
                        $"Invalid state for ForcedTrade action: {currentContext.DialogToOpen}"
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
                DialogTypeEnum.TableHandSelector,
                initiator,
                targetPlayer
            );
        }

        private ActionResult? ProcessTableHandSelection(
            ActionContext currentContext,
            Player responder,
            Boolean includeShieldsUpChecking = true
        )
        {
            var pendingAction = base.PendingActionManager.CurrPendingAction;
            if (pendingAction == null)
                throw new InvalidOperationException("No pending action found.");

            if (string.IsNullOrEmpty(currentContext.TargetPlayerId))
                throw new ActionContextParameterNullException(
                    currentContext,
                    "TargetPlayerId cannot be null for TableHandSelector."
                );
            if (currentContext.TargetCardId == null || !currentContext.TargetCardId.Any())
                throw new ActionContextParameterNullException(
                    currentContext,
                    $"TargetCardId was found null in {pendingAction.ActionType}!"
                );
            if (currentContext.OwnTargetCardId == null || !currentContext.OwnTargetCardId.Any())
                throw new ActionContextParameterNullException(
                    currentContext,
                    $"OwnTargetCardId was found null in {pendingAction.ActionType}!"
                );

            var initiatorId = currentContext.ActionInitiatingPlayerId;
            var initiator = base.PlayerManager.GetPlayerByUserId(initiatorId);
            string initiatorCardId = currentContext.OwnTargetCardId.First();
            var (cardFromInitiator, initiatorCardPropertyColorGroup) =
                base.PlayerHandManager.GetCardInTableHand(initiatorId, initiatorCardId);

            string targetId = currentContext.TargetPlayerId;
            string targetCardId = currentContext.TargetCardId;
            var targetPlayer = base.PlayerManager.GetPlayerByUserId(targetId);
            var (cardFromTarget, targetCardPropertyColorGroup) =
                base.PlayerHandManager.GetCardInTableHand(targetId, targetCardId);
            var targetPlayerHand = base.PlayerHandManager.GetPlayerHand(targetPlayer.UserId);

            this.ValidateActionPrerequisites(pendingAction, targetPlayer, cardFromTarget);
            bool specialConditionHandled = false;

            if (includeShieldsUpChecking)
            {
                specialConditionHandled = this.TryHandleSpecialConditions(
                    currentContext,
                    responder,
                    targetPlayer,
                    cardFromTarget,
                    targetPlayerHand
                );
            }

            if (!specialConditionHandled)
            {
                this.ExecuteNormalAction(
                    currentContext,
                    targetPlayer,
                    targetCardPropertyColorGroup,
                    initiatorCardPropertyColorGroup
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
                    TakenCard = cardFromTarget.ToDto(),
                    GivenCard = cardFromInitiator.ToDto(),
                };
            }
            return null;
        }

        private ActionResult? ProcessWildcardColorSelection(
            ActionContext currentContext,
            Player responder
        )
        {
            if (!currentContext.TargetSetColor.HasValue)
                throw new ActionContextParameterNullException(
                    currentContext,
                    "A color must be selected for the wildcard property."
                );
            if (String.IsNullOrEmpty(currentContext.TargetPlayerId))
                throw new ActionContextParameterNullException(
                    currentContext,
                    "TargetPlayerId is null for wildcard property selection."
                );
            if (currentContext.OwnTargetCardId == null)
                throw new ActionContextParameterNullException(
                    currentContext,
                    "OwnTargetCardId is null for wildcard property."
                );
            if (String.IsNullOrEmpty(currentContext.TargetCardId))
                throw new ActionContextParameterNullException(
                    currentContext,
                    "TargetCardId must be selected for the wildcard property."
                );

            string initiatorId = currentContext.ActionInitiatingPlayerId;
            string initiatorCardId = currentContext.OwnTargetCardId.First();
            var (cardFromInitiator, _) = base.PlayerHandManager.GetCardInTableHand(
                initiatorId,
                initiatorCardId
            );

            string targetId = currentContext.TargetPlayerId;
            string targetCardId = currentContext.TargetCardId;
            var (cardFromTarget, _) = base.PlayerHandManager.GetCardInTableHand(
                targetId,
                targetCardId
            );

            PropertyCardColoursEnum propertyGroupFromInitiator;
            PropertyCardColoursEnum propertyGroupFromTarget;

            // Determine destination colors based on which card is the wildcard and who is receiving it.
            if (cardFromTarget is SystemWildCard && responder.UserId == initiatorId)
            {
                propertyGroupFromTarget = currentContext.TargetSetColor.Value;
                propertyGroupFromInitiator = (
                    (StandardSystemCard)cardFromInitiator
                ).CardColoursEnum;
            }
            else if (cardFromInitiator is SystemWildCard && responder.UserId == targetId)
            {
                propertyGroupFromInitiator = currentContext.TargetSetColor.Value;
                propertyGroupFromTarget = ((StandardSystemCard)cardFromTarget).CardColoursEnum;
            }
            else
            {
                throw new InvalidOperationException(
                    "Could not resolve wildcard trade logic. The responder may not match the wildcard recipient."
                );
            }

            base.ActionExecutor.MovePropertyBetweenTableHands(
                initiatorId: currentContext.ActionInitiatingPlayerId,
                targetId: currentContext.TargetPlayerId,
                cardIdToTake: currentContext.TargetCardId,
                colorForTakenCard: propertyGroupFromTarget,
                cardIdToGive: currentContext.OwnTargetCardId.First(),
                colorForGivenCard: propertyGroupFromInitiator
            );
            base.CompleteAction();

            if (currentContext.DialogResponse == CommandResponseEnum.RejectShieldsUp)
            {
                return null;
            }

            return new ActionResult
            {
                ActionInitiatingPlayerId = currentContext.ActionInitiatingPlayerId,
                AffectedPlayerId = targetId,
                ActionType = currentContext.ActionType,
                TakenCard = cardFromTarget.ToDto(),
                GivenCard = cardFromInitiator.ToDto(),
            };
        }

        private void ValidateActionPrerequisites(
            PendingAction pendingAction,
            Player targetPlayer,
            Card targetCard
        )
        {
            if (targetCard is not StandardSystemCard systemCard)
                return;

            var targetPlayerTableHand = base.PlayerHandManager.GetPropertyGroupInPlayerTableHand(
                targetPlayer.UserId,
                systemCard.CardColoursEnum
            );

            if (pendingAction.ActionType == ActionTypes.ForcedTrade)
            {
                base.RulesManager.ValidateForcedTradeTarget(
                    targetPlayerTableHand,
                    systemCard.CardColoursEnum
                );
            }
        }

        private bool TryHandleSpecialConditions(
            ActionContext currentContext,
            Player initiator,
            Player targetPlayer,
            Card targetCard,
            List<Card> targetPlayerHand
        )
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

            if (currentContext.OwnTargetCardId == null || !currentContext.OwnTargetCardId.Any())
            {
                throw new ActionContextParameterNullException(
                    currentContext,
                    "OwnTargetCardId null when handling forced trade!"
                );
            }

            var (ownTargetCard, _) = base.PlayerHandManager.GetCardInTableHand(
                currentContext.ActionInitiatingPlayerId,
                currentContext.OwnTargetCardId.First()
            );
            if (base.RulesManager.IsCardSystemWildCard(ownTargetCard))
            {
                this.BuildWildCardMovementContext(currentContext, targetPlayer); // Target player benefits
                return true;
            }

            return false; // No special conditions were met
        }

        private void ExecuteNormalAction(
            ActionContext currentContext,
            Player targetPlayer,
            PropertyCardColoursEnum colorForTakenCard,
            PropertyCardColoursEnum colorForGivenCard
        )
        {
            if (currentContext.TargetCardId == null || !currentContext.TargetCardId.Any())
            {
                throw new ActionContextParameterNullException(
                    currentContext,
                    "TargetCardId is null for normal execution."
                );
            }

            if (currentContext.OwnTargetCardId == null || !currentContext.OwnTargetCardId.Any())
            {
                throw new ActionContextParameterNullException(
                    currentContext,
                    "OwnTargetCardId is null for ForcedTrade execution."
                );
            }

            base.ActionExecutor.MovePropertyBetweenTableHands(
                initiatorId: currentContext.ActionInitiatingPlayerId,
                targetId: targetPlayer.UserId,
                cardIdToTake: currentContext.TargetCardId,
                colorForTakenCard: colorForTakenCard,
                cardIdToGive: currentContext.OwnTargetCardId.First(),
                colorForGivenCard: colorForGivenCard
            );
        }

        private void BuildWildCardMovementContext(ActionContext context, Player beneficiary)
        {
            base.SetNextDialog(context, DialogTypeEnum.WildcardColor, beneficiary, null); // Wildcard choice is seen by the beneficiary
        }
    }
}
