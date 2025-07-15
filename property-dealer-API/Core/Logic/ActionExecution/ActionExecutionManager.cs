

using property_dealer_API.Application.Enums;
using property_dealer_API.Application.Exceptions;
using property_dealer_API.Core.Entities;
using property_dealer_API.Core.Logic.DecksManager;
using property_dealer_API.Core.Logic.GameRulesManager;
using property_dealer_API.Core.Logic.PendingActionsManager;
using property_dealer_API.Core.Logic.PlayerHandsManager;
using property_dealer_API.Core.Logic.PlayersManager;
using property_dealer_API.Models.Cards;
using property_dealer_API.Models.Enums.Cards;
using System.Collections.Concurrent;

namespace property_dealer_API.Core.Logic.ActionExecution
{
    public class ActionExecutionManager
    {

        private readonly PlayersHandManager _playerHandManager;
        private readonly PlayerManager _playerManager;
        private readonly GameRuleManager _rulesManager;
        private readonly PendingActionManager _pendingActionManager;
        private readonly DeckManager _deckManager;

        public ActionExecutionManager(
            PlayersHandManager playerHandManager,
            PlayerManager playerManager,
            GameRuleManager rulesManager,
            PendingActionManager pendingActionManager,
            DeckManager deckManager)
        {
            _playerHandManager = playerHandManager;
            _playerManager = playerManager;
            _rulesManager = rulesManager;
            _pendingActionManager = pendingActionManager;
            _deckManager = deckManager;
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
                    // Throw before removing it in case an issue occurred.
                    if (!propertyGroup.HasValue)
                    {
                        throw new InvalidOperationException("Property group has no value!");
                    }

                    cardRemoved = this._playerHandManager.RemoveCardFromPlayerTableHand(player.UserId, ownCard);
                    this._playerHandManager.AddCardToPlayerTableHand(context.ActionInitiatingPlayerId, cardRemoved, propertyGroup.Value);
                }
            }

            //  Can clear pending action after taking everyones money & properties
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
                    actionContext.DialogToOpen = DialogTypeEnum.PayValue;
                    actionContext.DialogTargetList = this._rulesManager.IdentifyWhoSeesDialog(player, null, allPlayers, DialogTypeEnum.PayValue);
                    break;
                case ActionTypes.TradeEmbargo:
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

                    // TODO: Validate that the property set is complete and can accept the building
                    // For now, just add it to the property set
                    var buildingCard = this._playerHandManager.GetCardFromPlayerHandById(actionContext.ActionInitiatingPlayerId, actionContext.CardId);
                    this._playerHandManager.AddCardToPlayerTableHand(actionContext.ActionInitiatingPlayerId, buildingCard, actionContext.TargetSetColor.Value);

                    this._pendingActionManager.CanClearPendingAction = true;
                    break;

                case ActionTypes.Tribute:
                    // Calculate rent amount based on tribute card and selected property set
                    var tributeCard = this._playerHandManager.GetCardFromPlayerHandById(actionContext.ActionInitiatingPlayerId, actionContext.CardId) as TributeCard;
                    if (tributeCard != null && actionContext.TargetSetColor.HasValue)
                    {
                        var playerTableHand = this._playerHandManager.GetPropertyGroupInPlayerTableHand(actionContext.ActionInitiatingPlayerId, actionContext.TargetSetColor.Value);
                        actionContext.RentalAmount = this._rulesManager.CalculateRentAmount(actionContext.ActionInitiatingPlayerId, tributeCard, actionContext.TargetSetColor.Value, playerTableHand);
                    }

                    // Create PayValue dialog for all other players
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

                    var targetPlayerHand = this._playerHandManager.GetPlayerHand(targetPlayer.UserId);
                    if (this._rulesManager.DoesPlayerHaveShieldsUp(targetPlayer, targetPlayerHand))
                    {
                        actionContext.DialogToOpen = DialogTypeEnum.ShieldsUp;
                        actionContext.DialogTargetList = this._rulesManager.IdentifyWhoSeesDialog(player, targetPlayer, allPlayers, DialogTypeEnum.ShieldsUp);

                        pendingAction.RequiredResponders = new ConcurrentBag<Player>(actionContext.DialogTargetList);
                        this._pendingActionManager.IncrementCurrentStep();
                    }
                    else
                    {
                        // Execute hostile takeover - steal the entire property set
                        this.ExecuteHostileTakeover(actionContext.ActionInitiatingPlayerId, targetPlayer.UserId, actionContext.TargetSetColor!.Value);
                        this._pendingActionManager.CanClearPendingAction = true;
                    }
                    break;

                case ActionTypes.TradeEmbargo:
                    if (targetPlayer == null)
                    {
                        throw new InvalidOperationException("Target player cannot be null when doing trade embargo!");
                    }

                    actionContext.DialogToOpen = DialogTypeEnum.PayValue;
                    actionContext.DialogTargetList = this._rulesManager.IdentifyWhoSeesDialog(player, targetPlayer, allPlayers, DialogTypeEnum.PayValue);

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

            var targetPlayerHand = this._playerHandManager.GetPlayerHand(targetPlayer.UserId);

            switch (pendingAction.ActionType)
            {
                case ActionTypes.ForcedTrade:
                    if (this._rulesManager.DoesPlayerHaveShieldsUp(targetPlayer, targetPlayerHand))
                    {
                        actionContext.DialogToOpen = DialogTypeEnum.ShieldsUp;
                        actionContext.DialogTargetList = this._rulesManager.IdentifyWhoSeesDialog(player, targetPlayer, allPlayers, DialogTypeEnum.ShieldsUp);

                        pendingAction.RequiredResponders = new ConcurrentBag<Player>(actionContext.DialogTargetList);
                        this._pendingActionManager.IncrementCurrentStep();
                    }
                    else
                    {
                        // Execute forced trade
                        this.ExecuteForcedTrade(actionContext.ActionInitiatingPlayerId, targetPlayer.UserId, actionContext.TargetCardId!, actionContext.OwnTargetCardId!.First());
                        this._pendingActionManager.CanClearPendingAction = true;
                    }
                    break;

                case ActionTypes.PirateRaid:
                    if (this._rulesManager.DoesPlayerHaveShieldsUp(targetPlayer, targetPlayerHand))
                    {
                        actionContext.DialogToOpen = DialogTypeEnum.ShieldsUp;
                        actionContext.DialogTargetList = this._rulesManager.IdentifyWhoSeesDialog(player, targetPlayer, allPlayers, DialogTypeEnum.ShieldsUp);

                        pendingAction.RequiredResponders = new ConcurrentBag<Player>(actionContext.DialogTargetList);
                        this._pendingActionManager.IncrementCurrentStep();
                    }
                    else
                    {
                        // Execute pirate raid - steal the selected property
                        this.ExecutePirateRaid(actionContext.ActionInitiatingPlayerId, targetPlayer.UserId, actionContext.TargetCardId!);
                        this._pendingActionManager.CanClearPendingAction = true;
                    }
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported action type: {pendingAction.ActionType}");
            }
        }

        public void HandleWildCardResponse(Player player, ActionContext context)
        {
            var targetSetColor = context.TargetSetColor;
            var removedCard = this.HandleRemoveFromHand(context.ActionInitiatingPlayerId, context.CardId);

            if (!targetSetColor.HasValue)
            {
                throw new InvalidOperationException("Wildcard operation tried accessing target color with it being null!");
            }

            this._playerHandManager.AddCardToPlayerTableHand(context.ActionInitiatingPlayerId, removedCard, targetSetColor.Value);
            return;
        }

        public void HandleShieldsUpResponse(Player player, ActionContext context)
        {
            // Find and remove a shields up card from the player's hand
            var playerHand = this._playerHandManager.GetPlayerHand(player.UserId);
            var shieldsUpCard = playerHand.FirstOrDefault(card => card is CommandCard commandCard && commandCard.Command == ActionTypes.ShieldsUp);

            if (shieldsUpCard != null)
            {
                this.HandleRemoveFromHand(player.UserId, shieldsUpCard.CardGuid.ToString());
            }
        }

        private Card HandleRemoveFromHand(string userId, string cardId)
        {
            // Remove card and check if hand is empty, if it is, regerate all 5 cards for them.
            var cardRemoved = this._playerHandManager.RemoveFromPlayerHand(userId, cardId);
            this._deckManager.Discard(cardRemoved);
            var handIsEmpty = this._rulesManager.IsPlayerHandEmpty(this._playerHandManager.GetPlayerHand(userId));
            if (handIsEmpty)
            {
                this.AssignCardToPlayer(userId, 5);
            }

            return cardRemoved;
        }

        private void AssignCardToPlayer(string userId, int numCardsToDraw)
        {
            var freshCards = this._deckManager.DrawCard(numCardsToDraw);
            this._playerHandManager.AssignPlayerHand(userId, freshCards);
        }

        private void ExecuteHostileTakeover(string initiatorUserId, string targetUserId, PropertyCardColoursEnum targetSetColor)
        {
            var (propertyGroup, cardsInPropertyGroup) = this._playerHandManager.RemovePropertyGroupFromPlayerTableHand(targetUserId, targetSetColor);

            foreach (var card in cardsInPropertyGroup)
            {
                this._playerHandManager.AddCardToPlayerTableHand(initiatorUserId, card, propertyGroup);
            }
        }

        private void ExecuteForcedTrade(string initiatorUserId, string targetUserId, string targetCardId, string ownCardId)
        {
            // Remove from both player hands, then add to both

            var initiatorCard = this._playerHandManager.RemoveCardFromPlayerTableHand(initiatorUserId, ownCardId);
            var targetCard = this._playerHandManager.RemoveCardFromPlayerTableHand(targetUserId, targetCardId);

            if (targetCard is StandardSystemCard targetSystemCard)
            {
                this._playerHandManager.AddCardToPlayerTableHand(initiatorUserId, targetSystemCard, targetSystemCard.CardColoursEnum);
            }
            else
            {
                throw new StandardSystemCardException(targetCardId);
            }

            if (initiatorCard is StandardSystemCard initiatorSystemCard)
            {
                this._playerHandManager.AddCardToPlayerTableHand(targetUserId, initiatorSystemCard, initiatorSystemCard.CardColoursEnum);
            }
            else
            {
                throw new StandardSystemCardException(ownCardId);
            }
        }

        private void ExecutePirateRaid(string initiatorUserId, string targetUserId, string targetCardId)
        {
            // Remove from both player hands, then add to both

            var targetCard = this._playerHandManager.RemoveCardFromPlayerTableHand(targetUserId, targetCardId);

            if (targetCard is StandardSystemCard targetSystemCard)
            {
                this._playerHandManager.AddCardToPlayerTableHand(initiatorUserId, targetSystemCard, targetSystemCard.CardColoursEnum);
            }
            else
            {
                throw new StandardSystemCardException(targetCardId);
            }
        }
    }
}
