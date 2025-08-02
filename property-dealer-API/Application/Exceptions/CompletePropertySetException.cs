using property_dealer_API.Models.Enums.Cards;

namespace property_dealer_API.Application.Exceptions
{
    public class CompletePropertySetException : Exception
    {
        public PropertyCardColoursEnum Color { get; }

        public CompletePropertySetException(PropertyCardColoursEnum color)
            : base($"The {color} property set is already complete and protected from this action.")
        {
            this.Color = color;
        }
    }
}