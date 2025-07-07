namespace property_dealer_API.Application.Exceptions
{
    public class TableHandNotFoundException : Exception
    {
        public TableHandNotFoundException(string userId) : base($"{userId}'s Table hand was not found") { }
    }
}
