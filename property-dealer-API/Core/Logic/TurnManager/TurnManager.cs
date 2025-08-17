using property_dealer_API.Application.Exceptions;
using System.Collections.Concurrent;

namespace property_dealer_API.Core.Logic.TurnManager
{
    public class TurnManager : ITurnManager
    {
        private ConcurrentQueue<string> _turnKeeper = new();
        private int CurrUserActionCount = 0;
        private int MaxNumAction = 2; // 3-1
        private readonly object _queueLock = new();

        public string GetCurrentUserTurn()
        {
            if (this._turnKeeper.TryPeek(out string? user))
            {
                return user;
            }
            throw new InvalidOperationException("Cannot retrieve current user turn for user");
        }

        public void SetNextUsersTurn()
        {
            lock (this._queueLock)
            {
                this._turnKeeper.TryDequeue(out string? userId);

                if (userId == null)
                {
                    throw new InvalidOperationException("Cannot set next user turn");
                }

                this._turnKeeper.Enqueue(userId);
            }
        }

        public void AddPlayer(string userId)
        {
            this._turnKeeper.Enqueue(userId);
        }

        public void RemovePlayerFromQueue(string userId)
        {
            lock (this._queueLock)
            {
                var listTurnKeeper = this._turnKeeper.ToList();

                if (listTurnKeeper.Remove(userId))
                {
                    this._turnKeeper = new ConcurrentQueue<string>(listTurnKeeper);
                }
            }
        }

        public string? IncrementUserActionCount()
        {
            if (this.CurrUserActionCount < this.MaxNumAction)
            {
                this.CurrUserActionCount += 1;
            }
            else
            {
                this.SetNextUsersTurn();
                this.CurrUserActionCount = 0;
                return this.GetCurrentUserTurn();
            }

            return null;
        }

        public int GetCurrentUserActionCount()
        {
            return this.CurrUserActionCount;
        }
    }
}
