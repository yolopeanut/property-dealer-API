using property_dealer_API.Models.Enums;

namespace property_dealer_API.Application.DTOs.Responses
{
    public record GameListSummaryResponse(string RoomId, string RoomName, int NumPlayers, int MaxNumPlayers, GameStateEnum GameState);
}
