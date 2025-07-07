using property_dealer_API.Application.DTOs.Responses;
using property_dealer_API.Application.Exceptions;
using property_dealer_API.Core.Entities.Cards.CardRelatedEntities;
using property_dealer_API.Models.Cards;
using property_dealer_API.Models.Enums.Cards;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Concurrent;
using System.Collections.Generic;

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

        internal void RemovePlayerByUserId(string userId)
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
                        var cardListToAdd = new List<Card>();
                        playerTableHand.TryAdd(cardColorDestinationGroup, cardListToAdd);
                    }
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
            if (cardToAdd is not CommandCard or MoneyCard or TributeCard)
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
    }
}
