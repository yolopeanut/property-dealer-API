using property_dealer_API.Application.Enums;
using property_dealer_API.Core.Entities;
using property_dealer_API.Core.Logic.DecksManager;
using property_dealer_API.Core.Logic.GameRulesManager;
using property_dealer_API.Core.Logic.PendingActionsManager;
using property_dealer_API.Core.Logic.PlayerHandsManager;
using property_dealer_API.Models.Cards;
using property_dealer_API.Models.Enums.Cards;
using System.Collections.Concurrent;

namespace property_dealer_API.Core.Logic.ActionExecution.ActionsContextBuilder
{
    public class ActionContextBuilder : IActionContextBuilder
    {
        private readonly IPendingActionManager _pendingActionManager;
        private readonly IGameRuleManager _rulesManager;
        private readonly IDeckManager _deckManager;
        private readonly IPlayerHandManager _playerHandManager;

        public ActionContextBuilder(
            IPendingActionManager pendingActionManager,
            IGameRuleManager rulesManager,
            IDeckManager deckManager,
            IPlayerHandManager playerHandManager)
        {
            this._pendingActionManager = pendingActionManager;
            this._rulesManager = rulesManager;
            this._deckManager = deckManager;
            this._playerHandManager = playerHandManager;
        }

        public ActionContext? BuildActionContext(string userId, Card card, Player currentUser, List<Player> allPlayers)
        {
            var cardId = card.CardGuid.ToString();

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
                case ActionTypes.SpaceStation:
                case ActionTypes.Starbase:
                    return this.CreateActionContext(userId, cardId, DialogTypeEnum.PropertySetSelection, currentUser, null, allPlayers, pendingAction);

                // Own HAND selection (selecting rent card from hand to double)
                case ActionTypes.TradeEmbargo:
                    return this.CreateActionContext(userId, cardId, DialogTypeEnum.OwnHandSelection, currentUser, null, allPlayers, pendingAction);

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

        private void AssignCardToPlayer(string userId, int numCardsToDraw)
        {
            var freshCards = this._deckManager.DrawCard(numCardsToDraw);
            this._playerHandManager.AssignPlayerHand(userId, freshCards);
        }
    }
}