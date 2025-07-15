namespace property_dealer_API.Application.Exceptions
{
    public class StandardSystemCardException : Exception
    {
        public StandardSystemCardException(string cardId) : base($"CardId given for pirate was not a standard system card!:{cardId}") { }
    }
}
