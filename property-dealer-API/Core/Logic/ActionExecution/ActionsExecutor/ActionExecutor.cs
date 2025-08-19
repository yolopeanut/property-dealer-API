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

        public void MovePropertyBetweenTableHands(string initiatorId, string targetId, string cardIdToTake, PropertyCardColoursEnum colorForTakenCard, string? cardIdToGive = null, PropertyCardColoursEnum? colorForGivenCard = null)
        {
            // The Initiator TAKES a card from the Target
            var cardTaken = this._playerHandManager.RemoveCardFromPlayerTableHand(targetId, cardIdToTake);
            if (cardTaken is not StandardSystemCard && cardTaken is not SystemWildCard)
            {
                throw new InvalidOperationException($"Card {cardIdToTake} is not a movable property card.");
            }
            this._playerHandManager.AddCardToPlayerTableHand(initiatorId, cardTaken, colorForTakenCard);

            // The Initiator GIVES a card to the Target (Optional)
            if (cardIdToGive != null && colorForGivenCard.HasValue)
            {
                var cardGiven = this._playerHandManager.RemoveCardFromPlayerTableHand(initiatorId, cardIdToGive);
                if (cardGiven is not StandardSystemCard && cardGiven is not SystemWildCard)
                {
                    throw new InvalidOperationException($"Card {cardIdToGive} is not a movable property card.");
                }
                this._playerHandManager.AddCardToPlayerTableHand(targetId, cardGiven, colorForGivenCard.Value);
            }
            else if (cardIdToGive != null || colorForGivenCard.HasValue)
            {
                throw new ArgumentException("For a return trade, both cardIdToGive and colorForGivenCard must be provided.");
            }
        }

        public void ExecutePropertyTrade(string initiatorId, string initiatorCardId, PropertyCardColoursEnum colorForCardFromTarget, string targetId, string targetCardId, PropertyCardColoursEnum colorForCardFromInitiator)
        {
            // 1. Remove both cards from their original owners.
            var cardFromInitiator = this._playerHandManager.RemoveCardFromPlayerTableHand(initiatorId, initiatorCardId);
            var cardFromTarget = this._playerHandManager.RemoveCardFromPlayerTableHand(targetId, targetCardId);

            // Basic validation to ensure we're only trading property cards.
            if (cardFromInitiator is not StandardSystemCard && cardFromInitiator is not SystemWildCard)
            {
                throw new InvalidOperationException($"Card {initiatorCardId} is not a tradable property card.");
            }
            if (cardFromTarget is not StandardSystemCard && cardFromTarget is not SystemWildCard)
            {
                throw new InvalidOperationException($"Card {targetCardId} is not a tradable property card.");
            }

            // 2. Add the cards to their new owners with the specified colors.
            this._playerHandManager.AddCardToPlayerTableHand(initiatorId, cardFromTarget, colorForCardFromTarget);
            this._playerHandManager.AddCardToPlayerTableHand(targetId, cardFromInitiator, colorForCardFromInitiator);
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