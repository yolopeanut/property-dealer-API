namespace property_dealer_API.Application.Exceptions
{
    public class GameNotFoundException : Exception
    {
        public GameNotFoundException(string gameRoomId) : base($"Gameroom with id '{gameRoomId}' was not found") { }
    }
}
