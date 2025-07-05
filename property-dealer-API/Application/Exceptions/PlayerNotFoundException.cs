namespace property_dealer_API.Application.Exceptions
{
    public class PlayerNotFoundException : Exception
    {
        public PlayerNotFoundException(string userId) : base($"Player with {userId} was not found in the game") { }
    }
}
