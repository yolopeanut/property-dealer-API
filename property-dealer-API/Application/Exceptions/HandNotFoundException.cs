namespace property_dealer_API.Application.Exceptions
{
    public class HandNotFoundException : Exception
    {
        public HandNotFoundException(string userId) : base($"{userId}'s table hand was not found") { }
    }
}
