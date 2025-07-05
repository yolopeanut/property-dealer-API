using property_dealer_API.Models.Cards;

namespace property_dealer_API.Application.DTOs.Responses
{
    public record TableHands(string PlayerId, List<Card> TableHand);
}
