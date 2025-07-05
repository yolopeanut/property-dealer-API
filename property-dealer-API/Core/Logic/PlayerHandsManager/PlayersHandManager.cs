using property_dealer_API.Application.DTOs.Responses;
using property_dealer_API.Application.Exceptions;
using property_dealer_API.Models.Cards;
using System.Collections.Concurrent;

namespace property_dealer_API.Core.Logic.PlayerHandsManager
{
    public class PlayersHandManager : IReadOnlyPlayerHandManager
    {
        private readonly ConcurrentDictionary<string, List<Card>> _playerHands = new();
        public readonly ConcurrentDictionary<string, List<Card>> _playerTableHands = new();

        public PlayersHandManager()
        {
        }

        public List<Card> GetPlayerHand(string userId)
        {
            if (_playerHands.TryGetValue(userId, out List<Card>? hand))
            {
                return hand;
            }
            else
            {
                throw new HandNotFoundException(userId);
            }
        }

        public List<TableHands> GetAllTableHands()
        {
            List<TableHands> opponentHands = new();

            foreach (var player in _playerTableHands)
            {
                opponentHands.Add(new TableHands(player.Key, player.Value));
            }

            return opponentHands;
        }

        // Method used to assign player hands e.g game start (5 cards), draw hand (2 cards), pass go (2 cards), etc
        public void AssignPlayerHand(string userId, List<Card> cards)
        {
            _playerHands.AddOrUpdate(
                userId, cards, (key, existingHand) =>
                {
                    existingHand.AddRange(cards);
                    return existingHand;
                }
            );
        }

        // Method used only for adding to player hands and table hands (instantiating on game start)
        public void AddPlayerHand(string userId)
        {
            _playerHands.TryAdd(userId, new List<Card>());
            _playerTableHands.TryAdd(userId, new List<Card>());
        }

        internal void RemovePlayerByUserId(string userId)
        {
            var playerHandRemovalSuccess = this._playerHands.TryRemove(userId, out List<Card>? _);
            var playerTableHandRemovalSuccess = this._playerTableHands.TryRemove(userId, out List<Card>? _);

            if (!playerHandRemovalSuccess && !playerTableHandRemovalSuccess)
            {
                throw new PlayerNotFoundException(userId);
            }
        }
    }
}
