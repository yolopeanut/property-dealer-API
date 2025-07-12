namespace property_dealer_API.Application.Exceptions
{
    public class NoPlayersFoundException : Exception
    {
        public NoPlayersFoundException(string roomId) : base($"Turn manager cannot find any players in room {roomId}") { }
    }
}
