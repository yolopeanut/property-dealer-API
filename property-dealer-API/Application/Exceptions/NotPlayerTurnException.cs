namespace property_dealer_API.Application.Exceptions
{
    public class NotPlayerTurnException : Exception
    {
        public NotPlayerTurnException(string userId, string currentUserIdTurn) : base($"Error, it is {currentUserIdTurn}'s turn, but got {userId} playing.") { }
    }
}
