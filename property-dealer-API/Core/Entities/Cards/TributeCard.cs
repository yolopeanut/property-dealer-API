using property_dealer_API.Application.DTOs.Responses;
using property_dealer_API.Models.Enums.Cards;

namespace property_dealer_API.Models.Cards
{
    public class TributeCard : Card
    {
        public readonly List<PropertyCardColoursEnum> TargetColorsToApplyRent;

        public TributeCard(CardTypesEnum cardType, int value, List<PropertyCardColoursEnum> targetColorsToApplyRent, string description) : base(cardType, null, value, description)
        {
            this.TargetColorsToApplyRent = [.. targetColorsToApplyRent];
        }

        public override CardDto ToDto()
        {
            var dto = base.ToDto();

            dto.TargetColorsToApplyRent = this.TargetColorsToApplyRent;

            return dto;
        }
    }
}
