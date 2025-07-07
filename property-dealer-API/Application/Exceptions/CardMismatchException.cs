namespace property_dealer_API.Application.Exceptions
{
    public class CardMismatchException : Exception
    {
        public CardMismatchException(string userId, string cardId) : base($"{cardId}'s was trying to be converted into another type!") { }
    }
}
