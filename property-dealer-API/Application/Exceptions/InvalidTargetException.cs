using property_dealer_API.Models.Enums.Cards;

namespace property_dealer_API.Application.Exceptions
{
    public class InvalidTargetException : Exception
    {
        public string ActionType { get; }
        public PropertyCardColoursEnum? TargetColor { get; }
        public string? TargetPlayerId { get; }

        public InvalidTargetException(string actionType, PropertyCardColoursEnum targetColor, string targetPlayerId, string reason)
            : base($"{actionType} cannot target {targetPlayerId}'s {targetColor} property set: {reason}")
        {
            this.ActionType = actionType;
            this.TargetColor = targetColor;
            this.TargetPlayerId = targetPlayerId;
        }

        public InvalidTargetException(string actionType, string reason)
            : base($"{actionType} cannot be used: {reason}")
        {
            this.ActionType = actionType;
        }
    }
}