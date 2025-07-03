using property_dealer_API.Application.Enums;

namespace property_dealer_API.Application.MethodReturns
{
    public record RemovePlayerReturn(string? PlayerName, RemovePlayerResponse Response);
}
