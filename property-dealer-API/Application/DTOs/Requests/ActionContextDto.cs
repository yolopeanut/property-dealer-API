using property_dealer_API.Models.Enums.Cards;

namespace property_dealer_API.Application.DTOs.Requests
{
    // Not in use as of now, might use later for response mapping
    public class ActionContextDto
    {
        public string? TargetPlayerId { get; set; }
        public string? TargetCardId { get; set; }
        public PropertyCardColoursEnum? TargetColor { get; set; }
        public string? OwnTargetCardId { get; set; }
    }
}
