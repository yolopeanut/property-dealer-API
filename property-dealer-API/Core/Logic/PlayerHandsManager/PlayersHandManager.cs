using property_dealer_API.Application.Exceptions;
using property_dealer_API.Models.Cards;
using property_dealer_API.Models.Enums.Cards;
using System.Collections.Concurrent;

namespace property_dealer_API.Core.Logic.PlayerHandsManager
{
    public class PlayersHandManager : IReadOnlyPlayerHandManager
    {
        private readonly ConcurrentDictionary<string, List<Card>> _playerHands = new(); // All cards
        private readonly ConcurrentDictionary<string, Dictionary<PropertyCardColoursEnum, List<Card>>> _playerTableHands = new(); // Can be system card or wildcard
        private readonly ConcurrentDictionary<string, List<Card>> _playerMoneyHands = new(); // Can be tribute, command, or money card

        public PlayersHandManager()
        {
        }

        public List<Card> GetPlayerHand(string userId)
        {
            if (_playerHands.TryGetValue(userId, out List<Card>? hand))
            {
                lock (hand) // Lock while making the copy
                {
                    return new List<Card>(hand); // Return a safe copy
                }
            }
            else
            {
                throw new HandNotFoundException(userId);
            }
        }

        public Card GetCardFromPlayerHandById(string userId, string cardId)
        {
            var card = this.GetPlayerHand(userId).Find(card => card.CardGuid.ToString() == cardId);

            if (card == null) { throw new CardNotFoundException(cardId, userId); }

            return card;
        }

        public (int handGroup, PropertyCardColoursEnum? propertyGroup) FindCardInWhichHand(string userId, string cardId)
        {
            // Try money hand first
            try
            {
                var moneyCard = this.GetCardInMoneyHand(userId, cardId);
                return (0, null); // Found in money hand
            }
            catch (CardNotFoundException)
            {
                // Not in money hand, continue to table hand
            }

            // Try table hand
            try
            {
                var tableResult = this.GetCardInTableHand(userId, cardId);
                return (1, tableResult.propertyGroup); // Found in table hand
            }
            catch (CardNotFoundException)
            {
                // Not in table hand either
            }

            // Not found in either hand
            throw new CardNotFoundException(cardId, userId);
        }

        public void ProcessAllTableHandsSafely(Action<string, IReadOnlyDictionary<PropertyCardColoursEnum, IReadOnlyList<Card>>, IReadOnlyList<Card>> processAction)
        {
            foreach (var playerTableHand in _playerTableHands)
            {
                var userId = playerTableHand.Key;
                var tableHandCards = playerTableHand.Value;

                _playerMoneyHands.TryGetValue(userId, out List<Card>? moneyHandCards);

                // Lock BOTH collections to ensure a consistent snapshot
                lock (tableHandCards)
                    lock (moneyHandCards ?? new object())
                    {
                        // Create safe, read-only representations to pass to the action
                        var readOnlyTable = tableHandCards.ToDictionary(
                            kvp => kvp.Key,
                            kvp => (IReadOnlyList<Card>)kvp.Value.AsReadOnly()
                        );

                        var readOnlyMoney = moneyHandCards?.AsReadOnly() ?? (IReadOnlyList<Card>)[];

                        processAction(userId, readOnlyTable, readOnlyMoney);
                    }
            }
        }

        // Method used to assign player hands e.g game start (5 cards), draw hand (2 cards), pass go (2 cards), etc
        public void AssignPlayerHand(string userId, List<Card> cards)
        {
            var hand = _playerHands.GetOrAdd(userId, (key) => new List<Card>());
            lock (hand)
            {
                hand.AddRange(cards);
            }
        }

        // Method used only for adding to player hands and table hands (instantiating on game start)
        public void AddPlayerHand(string userId)
        {
            _playerHands.TryAdd(userId, new List<Card>());
            _playerTableHands.TryAdd(userId, new Dictionary<PropertyCardColoursEnum, List<Card>>());
            _playerMoneyHands.TryAdd(userId, new List<Card>());
        }
        public Card RemoveFromPlayerHand(string userId, string cardId)
        {
            Card? foundCard = null;
            this._playerHands.AddOrUpdate(
                userId,
                (key) => { throw new HandNotFoundException(key); },
                (key, existingHand) =>
                {
                    // Find the card within the existing hand.
                    foundCard = existingHand.Find(card => card.CardGuid.ToString() == cardId);
                    if (foundCard == null)
                    {
                        // If not found, return the original hand unmodified.
                        return existingHand;
                    }

                    var newHand = new List<Card>(existingHand);
                    newHand.Remove(foundCard);
                    return newHand;
                });

            if (foundCard == null)
            {
                throw new CardNotFoundException(cardId, userId);
            }

            return foundCard;
        }

        public void RemovePlayerByUserId(string userId)
        {
            var playerHandRemovalSuccess = this._playerHands.TryRemove(userId, out List<Card>? _);
            var playerMoneyHandRemovalSuccess = this._playerMoneyHands.TryRemove(userId, out List<Card>? _);
            var playerTableHandRemovalSuccess = this._playerTableHands.TryRemove(userId, out Dictionary<PropertyCardColoursEnum, List<Card>>? _);

            if (!playerHandRemovalSuccess && !playerTableHandRemovalSuccess && !playerMoneyHandRemovalSuccess)
            {
                throw new PlayerRemovalFailedException(
                    userId,
                    $"PlayerHand: {playerHandRemovalSuccess} | " +
                    $"PlayerTableHand: {playerTableHandRemovalSuccess} | " +
                    $"PlayerMoneyHand: {playerMoneyHandRemovalSuccess}"
                );
            }
        }

        public Card RemoveCardFromPlayerMoneyHand(string userId, string cardId)
        {
            if (!this._playerMoneyHands.TryGetValue(userId, out List<Card>? cards))
            {
                throw new HandNotFoundException(userId);
            }

            lock (cards)
            {
                var cardToRemove = cards.Find(card => card.CardGuid.ToString() == cardId);
                if (cardToRemove == null)
                {
                    throw new CardNotFoundException(cardId, userId);
                }

                cards.Remove(cardToRemove);
                return cardToRemove;
            }
        }

        public Card RemoveCardFromPlayerTableHand(string userId, string cardId)
        {
            if (!this._playerTableHands.TryGetValue(userId, out Dictionary<PropertyCardColoursEnum, List<Card>>? propertyGroupDict))
            {
                throw new HandNotFoundException(userId);
            }

            lock (propertyGroupDict)
            {
                foreach (var propertyGroup in propertyGroupDict)
                {
                    var cardList = propertyGroup.Value;

                    lock (cardList)
                    {
                        var cardToRemove = cardList.Find(card => card.CardGuid.ToString() == cardId);
                        if (cardToRemove != null)
                        {
                            cardList.Remove(cardToRemove);

                            if (cardList.Count <= 0)
                            {
                                propertyGroupDict.Remove(propertyGroup.Key);
                            }

                            return cardToRemove;
                        }
                    }
                }
                throw new CardNotFoundException(cardId, userId);
            }
        }

        public (PropertyCardColoursEnum propertyGroup, List<Card> cardsInPropertyGroup) RemovePropertyGroupFromPlayerTableHand(string userId, PropertyCardColoursEnum propertyCardColoursEnum)
        {
            if (!this._playerTableHands.TryGetValue(userId, out Dictionary<PropertyCardColoursEnum, List<Card>>? propertyGroups))
            {
                throw new TableHandNotFoundException(userId);
            }

            lock (propertyGroups)
            {
                var groupExists = propertyGroups.TryGetValue(propertyCardColoursEnum, out List<Card>? cards);

                if (groupExists && cards != null)
                {
                    propertyGroups.Remove(propertyCardColoursEnum);
                    return (propertyCardColoursEnum, cards);
                }
            }

            throw new InvalidOperationException("Property group selected does not exist");
        }
        public List<Card> GetPropertyGroupInPlayerTableHand(string userId, PropertyCardColoursEnum propertyCardColoursEnum)
        {
            if (!this._playerTableHands.TryGetValue(userId, out Dictionary<PropertyCardColoursEnum, List<Card>>? propertyGroups))
            {
                throw new TableHandNotFoundException(userId);
            }

            lock (propertyGroups)
            {
                var groupExists = propertyGroups.TryGetValue(propertyCardColoursEnum, out List<Card>? cards);

                if (groupExists && cards != null)
                {
                    propertyGroups.Remove(propertyCardColoursEnum);
                    return cards;
                }
            }

            throw new InvalidOperationException("Property group selected does not exist");
        }

        public void AddCardToPlayerHand(string userId, Card cardToAdd)
        {
            if (this._playerHands.TryGetValue(userId, out List<Card>? playerHand))
            {
                lock (playerHand)
                {
                    playerHand.Add(cardToAdd);
                }
            }
            else
            {
                throw new TableHandNotFoundException(userId);
            }
        }
        public void AddCardToPlayerTableHand(string userId, Card cardToAdd, PropertyCardColoursEnum cardColorDestinationGroup)
        {
            if (this._playerTableHands.TryGetValue(userId, out Dictionary<PropertyCardColoursEnum, List<Card>>? playerTableHand))
            {
                lock (playerTableHand)
                {
                    if (!playerTableHand.TryGetValue(cardColorDestinationGroup, out List<Card>? cardList))
                    {
                        cardList = new List<Card>();
                        playerTableHand.TryAdd(cardColorDestinationGroup, cardList);
                    }
                    // Cardlist is null here even though we add it above
                    cardList!.Add(cardToAdd);
                }
            }
            else
            {
                throw new TableHandNotFoundException(userId);
            }
        }
        public void AddCardToPlayerMoneyHand(string userId, Card cardToAdd)
        {
            if (cardToAdd is not (CommandCard or MoneyCard or TributeCard))
            {
                throw new CardMismatchException(userId, cardToAdd.CardGuid.ToString());
            }

            if (this._playerMoneyHands.TryGetValue(userId, out List<Card>? bankedCards))
            {
                lock (bankedCards)
                {
                    bankedCards.Add(cardToAdd);
                }
            }
            else
            {
                throw new TableHandNotFoundException(userId);
            }
        }

        private (Card card, PropertyCardColoursEnum propertyGroup) GetCardInTableHand(string userId, string cardId)
        {
            if (!this._playerTableHands.TryGetValue(userId, out Dictionary<PropertyCardColoursEnum, List<Card>>? propertyGroupDict))
            {
                throw new HandNotFoundException(userId);
            }

            lock (propertyGroupDict)
            {
                foreach (var propertyGroup in propertyGroupDict)
                {
                    var cardList = propertyGroup.Value;

                    lock (cardList)
                    {
                        var cardToFind = cardList.Find(card => card.CardGuid.ToString() == cardId);
                        if (cardToFind != null)
                        {
                            return (cardToFind, propertyGroup.Key);
                        }
                    }
                }

            }
            throw new CardNotFoundException(cardId, userId);
        }
        private Card GetCardInMoneyHand(string userId, string cardId)
        {
            if (!this._playerMoneyHands.TryGetValue(userId, out List<Card>? cards))
            {
                throw new HandNotFoundException(userId);
            }

            lock (cards)
            {
                var cardToFind = cards.Find(card => card.CardGuid.ToString() == cardId);
                if (cardToFind == null)
                {
                    throw new CardNotFoundException(cardId, userId);
                }

                return cardToFind;
            }
        }
    }
}
