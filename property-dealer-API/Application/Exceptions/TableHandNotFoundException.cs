namespace property_dealer_API.Application.Exceptions
{
    public class HandNotFoundException : Exception
    {
        public HandNotFoundException(string userId) : base($"{userId}'s hand was not found") { }
    }
}
