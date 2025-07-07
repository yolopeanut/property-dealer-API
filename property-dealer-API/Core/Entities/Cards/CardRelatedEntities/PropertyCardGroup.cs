using property_dealer_API.Application.DTOs.Responses;
using property_dealer_API.Models.Enums.Cards;

namespace property_dealer_API.Core.Entities.Cards.CardRelatedEntities
{
    public record PropertyCardGroup(PropertyCardColoursEnum cardColorEnum, List<CardDto> groupedPropertyCards);
}
