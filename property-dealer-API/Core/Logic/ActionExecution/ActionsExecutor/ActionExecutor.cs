using Microsoft.AspNetCore.Mvc;
using property_dealer_API.Application.Exceptions;
using property_dealer_API.Core.Entities;
using property_dealer_API.Core.Logic.DecksManager;
using property_dealer_API.Core.Logic.GameRulesManager;
using property_dealer_API.Core.Logic.PlayerHandsManager;
using property_dealer_API.Models.Cards;
using property_dealer_API.Models.Enums.Cards;

namespace property_dealer_API.Core.Logic.ActionExecution
{
    public class ActionExecutor : IActionExecutor
    {
        private readonly IPlayerHandManager _playerHandManager;
        private readonly IDeckManager _deckManager;
        private readonly IGameRuleManager _rulesManager;

        public ActionExecutor(
            IPlayerHandManager playerHandManager,
            IDeckManager deckManager,
            IGameRuleManager rulesManager)
        {
            this._playerHandManager = playerHandManager;
            this._deckManager = deckManager;
            this._rulesManager = rulesManager;
        }

        public void ExecuteHostileTakeover(string initiatorUserId, string targetUserId, PropertyCardColoursEnum targetSetColor)
        {
            var (propertyGroup, cardsInPropertyGroup) = this._playerHandManager.RemovePropertyGroupFromPlayerTableHand(targetUserId, targetSetColor);

            foreach (var card in cardsInPropertyGroup)
            {
                this._playerHandManager.AddCardToPlayerTableHand(initiatorUserId, card, propertyGroup);
            }
        }

        public void ExecuteForcedTrade(string initiatorUserId, string targetUserId, string targetCardId, string ownCardId)
        {
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

        public void ExecutePirateRaid(string initiatorUserId, string targetUserId, string targetCardId)
        {
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

        public Card HandleRemoveFromHand(string userId, string cardId)
        {
            var cardRemoved = this._playerHandManager.RemoveFromPlayerHand(userId, cardId);
            this._deckManager.Discard(cardRemoved);
            var handIsEmpty = this._rulesManager.IsPlayerHandEmpty(this._playerHandManager.GetPlayerHand(userId));
            if (handIsEmpty)
            {
                this.ExecuteDrawCards(userId, 5);
            }

            return cardRemoved;
        }

        public void ExecuteDrawCards(string userId, int numCardsToDraw)
        {
            var freshCards = this._deckManager.DrawCard(numCardsToDraw);
            this._playerHandManager.AssignPlayerHand(userId, freshCards);
        }

        public void ExecutePayment(string receivingPlayerId, string payingPlayerId, List<string> targetsChosenCards)
        {
            foreach (var card in targetsChosenCards ?? [])
            {
                Card cardRemoved;
                try
                {
                    var (handGroup, propertyGroup) = this._playerHandManager.FindCardInWhichHand(payingPlayerId, card);

                    if (handGroup == 0)
                    {
                        cardRemoved = this._playerHandManager.RemoveCardFromPlayerMoneyHand(payingPlayerId, card);
                        this._playerHandManager.AddCardToPlayerMoneyHand(receivingPlayerId, cardRemoved);
                    }
                    else
                    {
                        if (!propertyGroup.HasValue)
                        {
                            throw new InvalidOperationException("Property group has no value!");
                        }

                        cardRemoved = this._playerHandManager.RemoveCardFromPlayerTableHand(payingPlayerId, card);
                        this._playerHandManager.AddCardToPlayerTableHand(receivingPlayerId, cardRemoved, propertyGroup.Value);
                    }
                }
                catch
                {
                    throw;
                }
            }

        }

        public void ExecutePlayToTable(string actionInitiatingPlayerId, string cardId, PropertyCardColoursEnum targetSetColor)
        {
            Card foundCard = this._playerHandManager.GetCardFromPlayerHandById(actionInitiatingPlayerId, cardId);
            this._playerHandManager.AddCardToPlayerTableHand(actionInitiatingPlayerId, foundCard, targetSetColor);
        }

        public void ExecuteBuildOnSet(string actionInitiatingPlayerId, string cardId, PropertyCardColoursEnum targetSetColor)
        {
            var buildingCard = this._playerHandManager.GetCardFromPlayerHandById(actionInitiatingPlayerId, cardId);
            this._playerHandManager.AddCardToPlayerTableHand(actionInitiatingPlayerId, buildingCard, targetSetColor);
        }
    }
}