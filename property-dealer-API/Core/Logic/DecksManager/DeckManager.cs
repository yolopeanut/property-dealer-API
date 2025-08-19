using property_dealer_API.Application.Exceptions;
using property_dealer_API.Models.Cards;
using System.Collections.Concurrent;

namespace property_dealer_API.Core.Logic.DecksManager
{
    public class DeckManager : IDeckManager
    {
        private readonly ConcurrentStack<Card> _drawPile = new ConcurrentStack<Card>();
        private ConcurrentStack<Card> _discardPile = new ConcurrentStack<Card>();
        private readonly object _discardPileLock = new object();

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

        // All card in deck excluding player hands
        public List<Card> ViewAllCardsInDeck()
        {
            if (this._drawPile == null || this._discardPile == null)
            {
                throw new InvalidOperationException("The draw pile or discard pile has not been initialized.");
            }

            return this._drawPile.Concat(this._discardPile).ToList();
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
            this._discardPile.Push(card);
        }

        private readonly object _reshuffleLock = new object();

        private void ReshuffleIfNeeded()
        {
            lock (this._reshuffleLock)
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

        public Card? GetMostRecentDiscardedCard()
        {
            if (this._discardPile.TryPeek(out Card? card))
            {
                if (card is CommandCard)
                {
                    return card;
                }
            }
            return null;
        }

        public Card GetDiscardedCardById(string cardId)
        {
            lock (this._discardPileLock)
            {
                var allCards = new List<Card>();
                Card? foundCard = null;

                // Pop all cards
                while (this._discardPile.TryPop(out Card? card))
                {
                    if (card.CardGuid.ToString() == cardId)
                    {
                        foundCard = card;
                    }
                    else
                    {
                        allCards.Add(card);
                    }
                }

                // Put back all cards except the found one
                if (allCards.Count > 0)
                {
                    this._discardPile.PushRange(allCards.ToArray());
                }

                if (foundCard == null)
                {
                    throw new CardNotFoundException("Card not found in discard pile", cardId);
                }

                return foundCard;
            }
        }
    }
}
