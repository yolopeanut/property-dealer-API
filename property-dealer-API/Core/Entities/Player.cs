using property_dealer_API.Models.Cards;

namespace property_dealer_API.Core.Entities
{
    public class Player
    {
        public required string UserId { get; set; }
        public required string PlayerName { get; set; }
        public List<Card> Hand { get; set; } = [];
    }
}
