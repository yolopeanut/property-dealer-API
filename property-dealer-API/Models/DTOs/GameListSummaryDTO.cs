using property_dealer_API.Models.Enums;

namespace property_dealer_API.Models.DTOs
{
    public record GameListSummaryDTO(string RoomId, string RoomName, int NumPlayers, int MaxNumPlayers, GameStateEnum GameState);
}
