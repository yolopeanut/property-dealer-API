using property_dealer_API.Models.Enums.Cards;

namespace property_dealer_API.Application.Exceptions
{
    public class IncompletePropertySetException : Exception
    {
        public PropertyCardColoursEnum Color { get; }
        public int CurrentCount { get; }
        public int RequiredCount { get; }

        public IncompletePropertySetException(PropertyCardColoursEnum color, int currentCount, int requiredCount)
            : base($"The {color} property set is incomplete ({currentCount}/{requiredCount} properties). This action requires a completed set.")
        {
            this.Color = color;
            this.CurrentCount = currentCount;
            this.RequiredCount = requiredCount;
        }
    }
}