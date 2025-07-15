using property_dealer_API.Application.DTOs.Responses;
using property_dealer_API.Models.Enums.Cards;

namespace property_dealer_API.Models.Cards
{
    public class StandardSystemCard : SystemCard
    {
        public readonly PropertyCardColoursEnum CardColoursEnum;
        public readonly int MaxCards;
        public readonly List<int> RentalValues;

        public StandardSystemCard(CardTypesEnum cardType, string name, int value, PropertyCardColoursEnum cardColoursEnum, string description, int maxCards, List<int> rentalValues) : base(cardType, name, value, description)
        {
            this.CardColoursEnum = cardColoursEnum;
            this.MaxCards = maxCards;
            this.RentalValues = [.. rentalValues];
        }

        public override CardDto ToDto()
        {
            var dto = base.ToDto();

            dto.CardColoursEnum = this.CardColoursEnum;
            dto.MaxCards = this.MaxCards;
            dto.RentalValues = this.RentalValues;

            return dto;
        }
    }
}
