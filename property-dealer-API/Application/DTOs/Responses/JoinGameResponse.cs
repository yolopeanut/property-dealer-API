using property_dealer_API.Models.Enums;

namespace property_dealer_API.Application.DTOs.Responses
{
    public record JoinGameResponse(JoinGameResponseEnum joinGameResponseEnum, string roomId);
}
