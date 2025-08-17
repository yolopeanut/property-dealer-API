using property_dealer_API.Application.Enums;
using property_dealer_API.Models.Enums.Cards;
using System.Collections.Concurrent;

namespace property_dealer_API.Core.Entities
{
    public class PendingAction
    {
        public required string InitiatorUserId { get; set; }
        public ActionTypes ActionType { get; set; }

        // Thread-safe collections for multi-player responses
        public ConcurrentBag<Player> RequiredResponders { get; set; } = new();
        public ConcurrentQueue<(Player player, ActionContext Response)> ResponseQueue { get; set; } = new();
        public int NumProcessedResponses { get; set; } = 0;
        public bool IsWaitingForResponses => this.RequiredResponders.Count > this.ResponseQueue.Count;
    }
}
