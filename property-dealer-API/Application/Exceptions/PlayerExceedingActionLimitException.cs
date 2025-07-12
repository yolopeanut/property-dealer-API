namespace property_dealer_API.Application.Exceptions
{
    public class PlayerExceedingActionLimitException : Exception
    {
        public PlayerExceedingActionLimitException(string userId) : base($"Player {userId} is playing more than allowed.") { }
    }
}
