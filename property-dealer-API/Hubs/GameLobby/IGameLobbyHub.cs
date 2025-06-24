using property_dealer_API.Models.DTOs;
using property_dealer_API.Models.Enums;

namespace property_dealer_API.Hubs.GameLobby
{
    public interface IGameLobbyHub
    {
        Task GetAllLobbySummary(IEnumerable<GameListSummaryDTO> gameListSummary);
        Task JoinGameRoomStatus(JoinGameResponseDTO joinGameResponseDTO);
        Task CreateGameRoomId(string roomId);
        Task SendMsg(string msg);
    }
}
