namespace property_dealer_API.Application.Exceptions
{
    public class MoneyHandNotFoundException : Exception
    {
        public MoneyHandNotFoundException(string userId) : base($"{userId}'s Money hand was not found") { }
    }
}
