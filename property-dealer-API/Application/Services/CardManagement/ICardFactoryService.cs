using property_dealer_API.Models.Cards;

namespace property_dealer_API.Application.Services.CardManagement
{
    public interface ICardFactoryService
    {
        List<Card> StartCardFactory();
    }
}
