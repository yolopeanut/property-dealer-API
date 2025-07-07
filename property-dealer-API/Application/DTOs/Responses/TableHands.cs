using property_dealer_API.Core.Entities;
using property_dealer_API.Core.Entities.Cards.CardRelatedEntities;

namespace property_dealer_API.Application.DTOs.Responses
{
    public record TableHands(Player? Player, List<PropertyCardGroup> TableHand, List<CardDto> MoneyHand);
}
