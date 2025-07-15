using property_dealer_API.Application.DTOs.Responses;
using property_dealer_API.Models.Enums.Cards;

namespace property_dealer_API.Models.Cards
{
    public class SystemWildCard : Card
    {
        public SystemWildCard(CardTypesEnum cardType, string name, int value, string description) : base(cardType, name, value, description)
        {
        }
    }
}
