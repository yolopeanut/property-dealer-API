namespace property_dealer_API.Models
{
    public class Player
    {
        public required string UserId { get; set; }
        public required string? ConnectionId { get; set; }
        public required string PlayerName { get; set; }
    }
}
