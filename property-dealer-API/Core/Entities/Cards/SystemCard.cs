using property_dealer_API.Models.Enums.Cards;

namespace property_dealer_API.Models.Cards
{
    public abstract class SystemCard : Card
    {
        public SystemCard(CardTypesEnum cardType, string name, int value, string description) : base(cardType, name, value, description)
        {
        }
    }
}
