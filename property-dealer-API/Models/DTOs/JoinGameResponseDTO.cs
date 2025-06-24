using property_dealer_API.Models.Enums;

namespace property_dealer_API.Models.DTOs
{
    public record JoinGameResponseDTO(JoinGameResponseEnum joinGameResponseEnum, string roomId);
}
