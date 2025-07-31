using property_dealer_API.Application.Enums;
using property_dealer_API.Application.Exceptions;
using property_dealer_API.Core.Entities;
using property_dealer_API.Core.Logic.GameRulesManager;
using property_dealer_API.Core.Logic.PendingActionsManager;
using property_dealer_API.Core.Logic.PlayerHandsManager;
using property_dealer_API.Core.Logic.PlayersManager;
using property_dealer_API.Models.Cards;
using property_dealer_API.Models.Enums.Cards;
using System.Collections.Concurrent;

namespace property_dealer_API.Core.Logic.ActionExecution
{
    public class DialogResponseProcessor : IDialogResponseProcessor
    {
        private readonly IPlayerHandManager _playerHandManager;
        private readonly IPlayerManager _playerManager;
        private readonly IGameRuleManager _rulesManager;
        private readonly IPendingActionManager _pendingActionManager;
        private readonly IActionExecutor _actionExecutor;

        public DialogResponseProcessor(
            IPlayerHandManager playerHandManager,
            IPlayerManager playerManager,
            IGameRuleManager rulesManager,
            IPendingActionManager pendingActionManager,
            IActionExecutor actionExecutor)
        {
            this._playerHandManager = playerHandManager;
            this._playerManager = playerManager;
            this._rulesManager = rulesManager;
            this._pendingActionManager = pendingActionManager;
            this._actionExecutor = actionExecutor;
        }

        public void HandlePayValueResponse(Player player, ActionContext context)
        {
            foreach (var ownCard in context.OwnTargetCardId ?? [])
            {
                Card cardRemoved;
                var (handGroup, propertyGroup) = this._playerHandManager.FindCardInWhichHand(player.UserId, ownCard);

                if (handGroup == 0)
                {
                    cardRemoved = this._playerHandManager.RemoveCardFromPlayerMoneyHand(player.UserId, ownCard);
                    this._playerHandManager.AddCardToPlayerMoneyHand(context.ActionInitiatingPlayerId, cardRemoved);
                }
                else
                {
                    if (!propertyGroup.HasValue)
                    {
                        throw new InvalidOperationException("Property group has no value!");
                    }

                    cardRemoved = this._playerHandManager.RemoveCardFromPlayerTableHand(player.UserId, ownCard);
                    this._playerHandManager.AddCardToPlayerTableHand(context.ActionInitiatingPlayerId, cardRemoved, propertyGroup.Value);
                }
            }

            this._pendingActionManager.CanClearPendingAction = true;
        }

        public void HandlePlayerSelectionResponse(Player player, ActionContext actionContext)
        {
            var pendingAction = this._pendingActionManager.CurrPendingAction;
            var allPlayers = this._playerManager.GetAllPlayers();

            if (actionContext.TargetPlayerId == null)
            {
                throw new InvalidOperationException("Target player id is null, no player was selected!");
            }

            var targetPlayer = this._playerManager.GetPlayerByUserId(actionContext.TargetPlayerId);

            switch (pendingAction.ActionType)
            {
                case ActionTypes.PirateRaid:
                case ActionTypes.ForcedTrade:
                    actionContext.DialogToOpen = DialogTypeEnum.TableHandSelector;
                    actionContext.DialogTargetList = this._rulesManager.IdentifyWhoSeesDialog(player, targetPlayer, allPlayers, DialogTypeEnum.TableHandSelector);
                    break;
                case ActionTypes.HostileTakeover:
                    actionContext.DialogToOpen = DialogTypeEnum.PropertySetSelection;
                    actionContext.DialogTargetList = this._rulesManager.IdentifyWhoSeesDialog(player, targetPlayer, allPlayers, DialogTypeEnum.PropertySetSelection);
                    break;
                case ActionTypes.BountyHunter:
                case ActionTypes.TradeEmbargo:
                    // Calculate doubled rent amount based on the selected rent card and property set
                    if (actionContext.OwnTargetCardId?.Count > 0 && actionContext.TargetSetColor.HasValue)
                    {
                        var playerTableHand = this._playerHandManager.GetPropertyGroupInPlayerTableHand(actionContext.ActionInitiatingPlayerId, actionContext.TargetSetColor.Value);

                        // Calculate the base rent amount and then double it
                        var baseRentAmount = this._rulesManager.CalculateRentAmount(actionContext.TargetSetColor.Value, playerTableHand);
                        actionContext.PaymentAmount = baseRentAmount * 2; // Double the rent for Trade Embargo
                    }

                    actionContext.DialogToOpen = DialogTypeEnum.PayValue;
                    actionContext.DialogTargetList = this._rulesManager.IdentifyWhoSeesDialog(player, targetPlayer, allPlayers, DialogTypeEnum.PayValue);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported action type: {pendingAction.ActionType}");
            }
            pendingAction.RequiredResponders = new ConcurrentBag<Player>(actionContext.DialogTargetList);
            this._pendingActionManager.IncrementCurrentStep();
        }

        public void HandlePropertySetSelectionResponse(Player player, ActionContext actionContext)
        {
            var pendingAction = this._pendingActionManager.CurrPendingAction;
            var allPlayers = this._playerManager.GetAllPlayers();

            Player? targetPlayer = null;
            if (actionContext.TargetPlayerId != null)
            {
                targetPlayer = this._playerManager.GetPlayerByUserId(actionContext.TargetPlayerId);
            }

            switch (pendingAction.ActionType)
            {
                case ActionTypes.SpaceStation:
                case ActionTypes.Starbase:
                    if (actionContext.TargetSetColor == null)
                    {
                        throw new InvalidOperationException("Property set color must be specified for SpaceStation/Starbase");
                    }

                    ValidateAndExecuteBuildingPlacement(actionContext, pendingAction);
                    this._pendingActionManager.CanClearPendingAction = true;
                    break;

                case ActionTypes.Tribute:
                    var tributeCard = this._playerHandManager.GetCardFromPlayerHandById(actionContext.ActionInitiatingPlayerId, actionContext.CardId) as TributeCard;
                    if (tributeCard != null && actionContext.TargetSetColor.HasValue)
                    {
                        var playerTableHand = this._playerHandManager.GetPropertyGroupInPlayerTableHand(actionContext.ActionInitiatingPlayerId, actionContext.TargetSetColor.Value);
                        actionContext.PaymentAmount = this._rulesManager.CalculateRentAmount(actionContext.TargetSetColor.Value, playerTableHand);
                    }

                    actionContext.DialogToOpen = DialogTypeEnum.PayValue;
                    actionContext.DialogTargetList = this._rulesManager.IdentifyWhoSeesDialog(player, null, allPlayers, DialogTypeEnum.PayValue);

                    pendingAction.RequiredResponders = new ConcurrentBag<Player>(actionContext.DialogTargetList);
                    this._pendingActionManager.IncrementCurrentStep();
                    break;

                case ActionTypes.HostileTakeover:
                    if (targetPlayer == null)
                    {
                        throw new InvalidOperationException("Target player cannot be null when doing hostile takeover!");
                    }
                    if (!actionContext.TargetSetColor.HasValue)
                    {
                        throw new ActionContextParameterNullException(actionContext, "TargetSetColor was found to be null for hostile takeover!");
                    }
                    var targetPlayerSelectedPropertySet = this._playerHandManager.GetPropertyGroupInPlayerTableHand(targetPlayer.UserId, actionContext.TargetSetColor.Value);

                    this._rulesManager.ValidateHostileTakeoverTarget(targetPlayerSelectedPropertySet, actionContext.TargetSetColor.Value);

                    var targetPlayerHand = this._playerHandManager.GetPlayerHand(targetPlayer.UserId);
                    if (this._rulesManager.DoesPlayerHaveShieldsUp(targetPlayer, targetPlayerHand))
                    {
                        BuildShieldsUpContext(actionContext, player, targetPlayer, allPlayers, pendingAction);
                    }
                    else
                    {
                        this._actionExecutor.ExecuteHostileTakeover(actionContext.ActionInitiatingPlayerId, targetPlayer.UserId, actionContext.TargetSetColor!.Value);
                        this._pendingActionManager.CanClearPendingAction = true;
                    }
                    break;

                case ActionTypes.TradeEmbargo:
                    // For Trade Embargo, we don't have a target player yet at this step
                    // We're selecting the property set first, then player selection
                    actionContext.DialogToOpen = DialogTypeEnum.PlayerSelection;
                    actionContext.DialogTargetList = this._rulesManager.IdentifyWhoSeesDialog(player, null, allPlayers, DialogTypeEnum.PlayerSelection);

                    pendingAction.RequiredResponders = new ConcurrentBag<Player>(actionContext.DialogTargetList);
                    this._pendingActionManager.IncrementCurrentStep();

                    break;
                default:
                    throw new InvalidOperationException($"Unsupported action type: {pendingAction.ActionType}");
            }
        }

        public void HandleTableHandSelectorResponse(Player player, ActionContext actionContext)
        {
            var pendingAction = this._pendingActionManager.CurrPendingAction;
            var allPlayers = this._playerManager.GetAllPlayers();

            Player? targetPlayer = null;
            if (actionContext.TargetPlayerId != null)
            {
                targetPlayer = this._playerManager.GetPlayerByUserId(actionContext.TargetPlayerId);
            }

            if (targetPlayer == null)
            {
                throw new InvalidOperationException("Target player cannot be null when doing ForcedTrade or PirateRaid!");
            }

            this.ValidateAndExecuteTableHandAction(actionContext, player, targetPlayer, allPlayers, pendingAction);
            this._pendingActionManager.CanClearPendingAction = true;
        }

        public void HandleWildCardResponse(Player player, ActionContext context)
        {
            var targetSetColor = context.TargetSetColor;

            if (!targetSetColor.HasValue)
            {
                throw new InvalidOperationException("Wildcard operation tried accessing target color with it being null!");
            }

            var foundCard = this._playerHandManager.GetCardFromPlayerHandById(context.ActionInitiatingPlayerId, context.CardId);
            this._playerHandManager.AddCardToPlayerTableHand(context.ActionInitiatingPlayerId, foundCard, targetSetColor.Value);

            this._pendingActionManager.CanClearPendingAction = true;
        }

        public void HandleShieldsUpResponse(Player player, ActionContext context)
        {
            var playerHand = this._playerHandManager.GetPlayerHand(player.UserId);
            var shieldsUpCard = playerHand.FirstOrDefault(card => card is CommandCard commandCard && commandCard.Command == ActionTypes.ShieldsUp);

            if (shieldsUpCard != null)
            {
                this._actionExecutor.HandleRemoveFromHand(player.UserId, shieldsUpCard.CardGuid.ToString());
            }
        }

        public void HandleOwnHandSelectionResponse(Player player, ActionContext actionContext)
        {
            var pendingAction = this._pendingActionManager.CurrPendingAction;
            var allPlayers = this._playerManager.GetAllPlayers();

            switch (pendingAction.ActionType)
            {
                case ActionTypes.TradeEmbargo:
                    // The user has selected their rent card to double, now show player selection
                    actionContext.DialogToOpen = DialogTypeEnum.PlayerSelection;
                    actionContext.DialogTargetList = this._rulesManager.IdentifyWhoSeesDialog(player, null, allPlayers, DialogTypeEnum.PropertySetSelection);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported action type for own hand selection: {pendingAction.ActionType}");
            }

            pendingAction.RequiredResponders = new ConcurrentBag<Player>(actionContext.DialogTargetList);
            this._pendingActionManager.IncrementCurrentStep();
        }

        private void ValidateAndExecuteTableHandAction(ActionContext actionContext, Player player, Player targetPlayer, List<Player> allPlayers, PendingAction pendingAction)
        {
            if (actionContext.TargetCardId == null)
            {
                throw new ActionContextParameterNullException(actionContext, $"TargetCardId was found null in {pendingAction.ActionType}!");
            }

            var targetPlayerHand = this._playerHandManager.GetPlayerHand(targetPlayer.UserId);
            var targetCard = this._playerHandManager.GetCardFromPlayerHandById(targetPlayer.UserId, actionContext.TargetCardId);

            if (targetCard is StandardSystemCard systemCard)
            {
                var targetPlayerTableHand = this._playerHandManager.GetPropertyGroupInPlayerTableHand(targetPlayer.UserId, systemCard.CardColoursEnum);

                switch (pendingAction.ActionType)
                {
                    case ActionTypes.ForcedTrade:
                        this._rulesManager.ValidateForcedTradeTarget(targetPlayerTableHand, systemCard.CardColoursEnum);
                        break;
                    case ActionTypes.PirateRaid:
                        this._rulesManager.ValidatePirateRaidTarget(targetPlayerTableHand, systemCard.CardColoursEnum);
                        break;
                }
            }

            switch (pendingAction.ActionType)
            {
                case ActionTypes.ForcedTrade:
                    if (actionContext.OwnTargetCardId == null)
                    {
                        throw new ActionContextParameterNullException(actionContext, "OwnTargetCardId was found null in forced trade!");
                    }

                    if (this._rulesManager.DoesPlayerHaveShieldsUp(targetPlayer, targetPlayerHand))
                    {
                        BuildShieldsUpContext(actionContext, player, targetPlayer, allPlayers, pendingAction);
                    }
                    else
                    {
                        this._actionExecutor.ExecuteForcedTrade(actionContext.ActionInitiatingPlayerId, targetPlayer.UserId, actionContext.TargetCardId, actionContext.OwnTargetCardId.First());
                    }
                    break;

                case ActionTypes.PirateRaid:
                    if (this._rulesManager.DoesPlayerHaveShieldsUp(targetPlayer, targetPlayerHand))
                    {
                        BuildShieldsUpContext(actionContext, player, targetPlayer, allPlayers, pendingAction);
                    }
                    else
                    {
                        this._actionExecutor.ExecutePirateRaid(actionContext.ActionInitiatingPlayerId, targetPlayer.UserId, actionContext.TargetCardId);
                    }
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported action type: {pendingAction.ActionType}");
            }
        }

        private void ValidateAndExecuteBuildingPlacement(ActionContext actionContext, PendingAction pendingAction)
        {
            var playerTableHand = this._playerHandManager.GetPropertyGroupInPlayerTableHand(actionContext.ActionInitiatingPlayerId, actionContext.TargetSetColor!.Value);

            if (pendingAction.ActionType == ActionTypes.SpaceStation)
            {
                this._rulesManager.ValidateSpaceStationPlacement(playerTableHand, actionContext.TargetSetColor.Value);
            }
            else if (pendingAction.ActionType == ActionTypes.Starbase)
            {
                this._rulesManager.ValidateStarbasePlacement(playerTableHand, actionContext.TargetSetColor.Value);
            }

            var buildingCard = this._playerHandManager.GetCardFromPlayerHandById(actionContext.ActionInitiatingPlayerId, actionContext.CardId);
            this._playerHandManager.AddCardToPlayerTableHand(actionContext.ActionInitiatingPlayerId, buildingCard, actionContext.TargetSetColor.Value);
        }

        private void BuildShieldsUpContext(ActionContext actionContext, Player player, Player? targetPlayer, List<Player> allPlayers, PendingAction pendingAction)
        {
            actionContext.DialogToOpen = DialogTypeEnum.ShieldsUp;
            actionContext.DialogTargetList = this._rulesManager.IdentifyWhoSeesDialog(player, targetPlayer, allPlayers, DialogTypeEnum.ShieldsUp);

            pendingAction.RequiredResponders = new ConcurrentBag<Player>(actionContext.DialogTargetList);
            this._pendingActionManager.IncrementCurrentStep();
        }
    }
}