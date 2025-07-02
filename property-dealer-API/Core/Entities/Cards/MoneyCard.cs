using property_dealer_API.Models.Enums.Cards;

namespace property_dealer_API.Models.Cards
{
    public class MoneyCard : Card
    {
        public MoneyCard(CardTypesEnum cardType, int value) : base(cardType, $"${value}M", value, null)
        {

        }
    }
}
