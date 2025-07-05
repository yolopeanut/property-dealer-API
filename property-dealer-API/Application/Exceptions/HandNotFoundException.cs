namespace property_dealer_API.Application.Exceptions
{
    public class TableHandNotFoundException : Exception
    {
        public TableHandNotFoundException(string userId) : base($"{userId}'s table hand was not found") { }
    }
}
