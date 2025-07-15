using System.Collections.Concurrent;
using property_dealer_API.Models.Enums.Cards;

namespace property_dealer_API.Core.Entities
{
    public class PendingAction
    {
        public required string InitiatorUserId { get; set; }
        public ActionTypes ActionType { get; set; }
        public int CurrentStep { get; set; } = 1;

        // Thread-safe collections for multi-player responses
        public ConcurrentBag<Player> RequiredResponders { get; set; } = new();
        public ConcurrentQueue<(Player player, ActionContext Response)> ResponseQueue { get; set; } = new();

        public bool IsWaitingForResponses => RequiredResponders.Count > ResponseQueue.Count;
    }
}
