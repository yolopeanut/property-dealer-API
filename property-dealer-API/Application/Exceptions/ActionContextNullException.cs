using property_dealer_API.Models.Cards;

namespace property_dealer_API.Application.Exceptions
{
    public class ActionContextNullException : Exception
    {
        public ActionContextNullException(Card cardPlayed) : base($"{cardPlayed} played did not have a action context") { }
    }
}
