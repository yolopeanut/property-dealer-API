using property_dealer_API.Application.Exceptions;
using System.Collections.Concurrent;

namespace property_dealer_API.Core.Logic.TurnManager
{
    public class TurnManager
    {
        private ConcurrentQueue<string> _turnKeeper = new();
        private int CurrUserActionCount = 0;
        private int MaxNumAction = 2; // 3-1
        private readonly object _queueLock = new();

        private readonly string _roomId; // used only for error throwing

        public TurnManager(string roomId)
        {
            this._roomId = roomId;

        }

        public string GetCurrentUserTurn()
        {
            if (this._turnKeeper.TryPeek(out string? user))
            {
                return user;
            }
            throw new NoPlayersFoundException(this._roomId);
        }

        public void SetNextUsersTurn()
        {
            lock (this._queueLock)
            {
                this._turnKeeper.TryDequeue(out string? userId);

                if (userId == null)
                {
                    throw new NoPlayersFoundException(this._roomId);
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
                    _turnKeeper = new ConcurrentQueue<string>(listTurnKeeper);
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
