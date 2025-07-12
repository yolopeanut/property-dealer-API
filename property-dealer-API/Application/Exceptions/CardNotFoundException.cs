namespace property_dealer_API.Application.Exceptions
{
    public class CardNotFoundException : Exception
    {
        public CardNotFoundException(string cardId, string userId) : base($"{cardId} was not found in {userId}'s hand") { }
        public CardNotFoundException(string msg) : base(msg) { }
    }
}
