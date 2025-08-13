using property_dealer_API.Models.Enums.Cards;

namespace property_dealer_API.Core.Logic.ActionExecution
{
    public interface IActionHandlerResolver
    {
        IActionHandler GetHandler(ActionTypes actionType);
    }
}
