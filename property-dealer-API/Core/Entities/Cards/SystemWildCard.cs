using property_dealer_API.Models.Enums.Cards;

namespace property_dealer_API.Models.Cards
{
    public class SystemWildCard : SystemCard
    {
        public IReadOnlyList<PropertyCardColoursEnum> CardColoursList;

        public SystemWildCard(CardTypesEnum cardType, string name, int value, List<PropertyCardColoursEnum> cardColoursList) : base(cardType, name, value)
        {
            this.CardColoursList = [.. cardColoursList];
        }
    }
}
