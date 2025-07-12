using property_dealer_API.Application.DTOs.Requests;
using property_dealer_API.Models.Enums.Cards;
using System.Diagnostics.CodeAnalysis;

namespace property_dealer_API.Core
{
    // Not in use yet, might need to use when command responses are implmented.
    public class ActionContext
    {
        public required string CardId { get; set; }
        public required string InitiatingPlayerId { get; set; }
        public string? TargetPlayerId { get; set; }
        public string? TargetCardId { get; set; }
        public PropertyCardColoursEnum? TargetColor { get; set; }
        public string? OwnTargetCardId { get; set; }

        [SetsRequiredMembers]
        public ActionContext(string cardId, string initiatingPlayerId, ActionContextDto dto)
        {
            CardId = cardId;
            InitiatingPlayerId = initiatingPlayerId;
            TargetPlayerId = dto.TargetPlayerId;
            TargetCardId = dto.TargetCardId;
            TargetColor = dto.TargetColor;
            OwnTargetCardId = dto.OwnTargetCardId;
        }
    }
}
