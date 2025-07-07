using property_dealer_API.Application.DTOs.Responses;
using property_dealer_API.Models.Enums.Cards;

namespace property_dealer_API.Models.Cards
{
    public class StandardSystemCard : SystemCard
    {
        public readonly PropertyCardColoursEnum CardColoursEnum;

        public StandardSystemCard(CardTypesEnum cardType, string name, int value, PropertyCardColoursEnum cardColoursEnum) : base(cardType, name, value)
        {
            this.CardColoursEnum = cardColoursEnum;
        }

        public override CardDto ToDto()
        {
            var dto = base.ToDto();

            dto.CardColoursEnum = this.CardColoursEnum;

            return dto;
        }
    }
}
