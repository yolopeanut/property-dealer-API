using property_dealer_API.Models.Enums.Cards;

namespace property_dealer_API.Core.Entities
{
    public class PendingAction
    {
        public required string InitiatorUserId { get; set; }
        public ActionTypes ActionType { get; set; }
        public int CurrentStep { get; set; } = 1;
        public Dictionary<string, object> StoredData { get; set; } = new();
    }
}
