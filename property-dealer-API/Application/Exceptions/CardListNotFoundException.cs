using property_dealer_API.Models.Cards;
using property_dealer_API.Models.Enums.Cards;

namespace property_dealer_API.Application.Exceptions
{
    public class CardListNotFoundException : Exception
    {
        public CardListNotFoundException(string userId, Card cardToAdd, PropertyCardColoursEnum cardColorDestinationGroup) : base($"{userId} could not add ${cardToAdd} to ${cardColorDestinationGroup}") { }
    }
}
