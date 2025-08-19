using property_dealer_API.Models.Enums.Cards;

namespace property_dealer_API.Application.DTOs.Responses
{
    public class DebugContext
    {
        public required string UserId { get; set; }
        public CardTypesEnum? CardTypeToSpawn { get; set; }
        public ActionTypes? ActionCardToSpawnType { get; set; }
        public PropertyCardColoursEnum? SetColorToSpawn { get; set; }
        public List<PropertyCardColoursEnum>? TributeTargetColors { get; set; }
        public int NumberOfDummyPlayersToSpawn { get; set; }
        public string? CardIdToDelete { get; set; }
    }
}
