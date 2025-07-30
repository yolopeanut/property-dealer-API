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
            ActionType = actionType;
            TargetColor = targetColor;
            TargetPlayerId = targetPlayerId;
        }

        public InvalidTargetException(string actionType, string reason)
            : base($"{actionType} cannot be used: {reason}")
        {
            ActionType = actionType;
        }
    }
}