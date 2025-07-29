

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
            this._playerHandManager = playerHandManager;
            this._playerManager = playerManager;
            this._rulesManager = rulesManager;
            this._pendingActionManager = pendingActionManager;
            this._deckManager = deckManager;
        }

        //=================== Action context building ===========================//
        #region Action context building
        public ActionContext? ExecuteAction(string userId, string cardId, Card card, Player currentUser, List<Player> allPlayers)
        {
            // Handle all action types in one place
            switch (card)
            {
                case CommandCard commandCard:
                    return this.HandleCommandCard(commandCard, userId, cardId, currentUser, allPlayers);

                case TributeCard tributeCard:
                    return this.HandleTributeCard(tributeCard, userId, cardId, currentUser, allPlayers);

                case SystemWildCard wildCard:
                    return this.HandleSystemWildCard(wildCard, userId, cardId, currentUser, allPlayers);

                default:
                    throw new InvalidOperationException($"Unsupported card type: {card.GetType().Name}");
            }
        }

        private ActionContext? HandleCommandCard(CommandCard commandCard, string userId, string cardId, Player currentUser, List<Player> allPlayers)
        {
            var pendingAction = new PendingAction { InitiatorUserId = userId, ActionType = commandCard.Command };

            switch (commandCard.Command)
            {
                // Immediate actions - no dialog needed
                case ActionTypes.ExploreNewSector:
                    this.AssignCardToPlayer(userId, 2);
                    return null;

                case ActionTypes.ShieldsUp:
                    // Cannot use shields up without any attack
                    return null;

                // Player selection actions
                case ActionTypes.HostileTakeover:
                case ActionTypes.PirateRaid:
                case ActionTypes.ForcedTrade:
                case ActionTypes.BountyHunter:
                    return this.CreateActionContext(userId, cardId, DialogTypeEnum.PlayerSelection, currentUser, null, allPlayers, pendingAction);

                // Direct payment actions
                case ActionTypes.TradeDividend:
                    return this.CreateActionContext(userId, cardId, DialogTypeEnum.PayValue, currentUser, null, allPlayers, pendingAction);

                // Property set selection actions
                case ActionTypes.TradeEmbargo:
                case ActionTypes.SpaceStation:
                case ActionTypes.Starbase:
                    return this.CreateActionContext(userId, cardId, DialogTypeEnum.PropertySetSelection, currentUser, null, allPlayers, pendingAction);

                default:
                    throw new InvalidOperationException($"Unsupported command action: {commandCard.Command}");
            }
        }

        private ActionContext HandleTributeCard(TributeCard tributeCard, string userId, string cardId, Player currentUser, List<Player> allPlayers)
        {
            var pendingAction = new PendingAction { InitiatorUserId = userId, ActionType = ActionTypes.Tribute };
            return this.CreateActionContext(userId, cardId, DialogTypeEnum.PropertySetSelection, currentUser, null, allPlayers, pendingAction);
        }

        private ActionContext HandleSystemWildCard(SystemWildCard wildCard, string userId, string cardId, Player currentUser, List<Player> allPlayers)
        {
            var pendingAction = new PendingAction { InitiatorUserId = userId, ActionType = ActionTypes.SystemWildCard };
            return this.CreateActionContext(userId, cardId, DialogTypeEnum.WildcardColor, currentUser, null, allPlayers, pendingAction);
        }
        private ActionContext CreateActionContext(string userId, string cardId, DialogTypeEnum dialogType, Player currentUser, Player? targetUser, List<Player> allPlayers, PendingAction pendingAction)
        {
            var dialogTargetList = this._rulesManager.IdentifyWhoSeesDialog(currentUser, targetUser, allPlayers, dialogType);
            var amountToPay = this._rulesManager.GetPaymentAmount(pendingAction.ActionType);
            pendingAction.RequiredResponders = new ConcurrentBag<Player>(dialogTargetList);

            // Set the pending action
            this._pendingActionManager.CurrPendingAction = pendingAction;
            this._pendingActionManager.CanClearPendingAction = false;

            return new ActionContext
            {
                CardId = cardId,
                ActionInitiatingPlayerId = userId,
                ActionType = pendingAction.ActionType,
                DialogTargetList = dialogTargetList,
                DialogToOpen = dialogType,
                PaymentAmount = amountToPay
            };
        }

        #endregion

        #region Dialog Response Handling

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
                        actionContext.PaymentAmount = this._rulesManager.CalculateRentAmount(actionContext.ActionInitiatingPlayerId, tributeCard, actionContext.TargetSetColor.Value, playerTableHand);
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
                        if (actionContext.TargetCardId == null || actionContext.OwnTargetCardId == null)
                        {
                            throw new ActionContextParameterNullException(actionContext, "TargetCardId and OwnTargetCardId was found null in forced trade!");
                        }

                        // Execute forced trade
                        this.ExecuteForcedTrade(actionContext.ActionInitiatingPlayerId, targetPlayer.UserId, actionContext.TargetCardId, actionContext.OwnTargetCardId.First());
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
                        if (actionContext.TargetCardId == null)
                        {
                            throw new ActionContextParameterNullException(actionContext, "TargetCardId was found null in PirateRaid!");
                        }

                        // Execute pirate raid - steal the selected property
                        this.ExecutePirateRaid(actionContext.ActionInitiatingPlayerId, targetPlayer.UserId, actionContext.TargetCardId);
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

            if (!targetSetColor.HasValue)
            {
                throw new InvalidOperationException("Wildcard operation tried accessing target color with it being null!");
            }

            var foundCard = this._playerHandManager.GetCardFromPlayerHandById(context.ActionInitiatingPlayerId, context.CardId);
            this._playerHandManager.AddCardToPlayerTableHand(context.ActionInitiatingPlayerId, foundCard, targetSetColor.Value);

            this._pendingActionManager.CanClearPendingAction = true;
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

        #endregion

        #region Helper methods
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

        #endregion
    }
}
