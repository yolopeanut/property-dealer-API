namespace property_dealer_API.Application.Exceptions
{
    public class PlayerRemovalFailedException : Exception
    {
        public PlayerRemovalFailedException(string userId, string details) : base($"Failed to remove {userId} - {details}") { }
    }
}
