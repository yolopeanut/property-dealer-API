using property_dealer_API.Application.Exceptions;
using property_dealer_API.Models.Cards;
using System.Collections.Concurrent;

namespace property_dealer_API.Core.Logic.DecksManager
{
    public class DeckManager : IReadOnlyDeckManager
    {
        private readonly ConcurrentStack<Card> _drawPile = new ConcurrentStack<Card>();
        private ConcurrentStack<Card> _discardPile = new ConcurrentStack<Card>();

        public DeckManager()
        {
        }
        public void PopulateInitialDeck(List<Card> initialDeck)
        {
            Console.WriteLine("POPULATING INITIAL DECK");

            initialDeck.Shuffle();

            foreach (var card in initialDeck)
            {
                this._drawPile.Push(card);
            }
        }

        public List<Card> DrawCard(int numToDraw)
        {
            var cardList = new List<Card>();
            for (int i = 0; i < numToDraw; i++)
            {
                if (this._drawPile.TryPop(out Card? result))
                {
                    cardList.Add(result);
                    continue;
                }

                Console.WriteLine("RESHUFFLING DECK");
                ReshuffleIfNeeded();

                if (this._drawPile.TryPop(out Card? afterReshuffle))
                {
                    cardList.Add(afterReshuffle);
                }

            }

            return cardList;
        }

        public void Discard(Card card)
        {
            this._discardPile.Push(card);
        }

        private readonly object _reshuffleLock = new object();

        private void ReshuffleIfNeeded()
        {
            lock (_reshuffleLock)
            {
                // Double-check: Another thread might have already done this while we waited.
                if (!this._drawPile.IsEmpty || this._discardPile.IsEmpty)
                {
                    return;
                }

                // Atomically swap the current discard pile with a new empty one.
                // This prevents the "lost card" race condition.
                var cardsToReshuffle = Interlocked.Exchange(ref this._discardPile, new ConcurrentStack<Card>());

                var shuffledDiscards = cardsToReshuffle.ToList();
                shuffledDiscards.Shuffle();

                // PushRange is a thread-safe way to add multiple items.
                this._drawPile.PushRange(shuffledDiscards.ToArray());

            }
        }

        public Card GetMostRecentDiscardedCard()
        {
            if (this._discardPile.TryPeek(out Card? card))
            {
                return card;
            }

            throw new CardNotFoundException("Cannot retrieve most recent discarded card, there are no cards in the discard pile");
        }
    }
}
