using property_dealer_API.Models.Cards;
using System.Collections.Concurrent;

namespace property_dealer_API.Core.Logic
{
    public class DeckManager
    {
        private readonly ConcurrentStack<Card> _drawPile = new ConcurrentStack<Card>();
        private ConcurrentBag<Card> _discardPile = new ConcurrentBag<Card>();

        public DeckManager()
        {
        }
        public void PopulateInitialDeck(List<Card> initialDeck)
        {
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

                this.ReshuffleIfNeeded();

                if (this._drawPile.TryPop(out Card? afterReshuffle))
                {
                    cardList.Add(afterReshuffle);
                }

            }

            return cardList;
        }

        public void Discard(Card card)
        {
            _discardPile.Add(card);
        }

        private readonly object _reshuffleLock = new object();

        private void ReshuffleIfNeeded()
        {
            lock (_reshuffleLock)
            {
                // Double-check: Another thread might have already done this while we waited.
                if (this._drawPile.IsEmpty && !_discardPile.IsEmpty)
                {
                    // Atomically swap the current discard pile with a new empty one.
                    // This prevents the "lost card" race condition.
                    var cardsToReshuffle = Interlocked.Exchange(ref _discardPile, new ConcurrentBag<Card>());

                    var shuffledDiscards = cardsToReshuffle.ToList();
                    shuffledDiscards.Shuffle();

                    // PushRange is a thread-safe way to add multiple items.
                    _drawPile.PushRange(shuffledDiscards.ToArray());
                }
            }
        }


    }
}
