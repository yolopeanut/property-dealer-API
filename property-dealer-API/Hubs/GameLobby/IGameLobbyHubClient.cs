using property_dealer_API.Application.DTOs.Responses;
using TypedSignalR.Client;

namespace property_dealer_API.Hubs.GameLobby
{
    [Receiver]
    public interface IGameLobbyHubClient
    {
        Task GetAllLobbySummary(IEnumerable<GameListSummaryResponse> gameListSummary);
        Task JoinGameRoomStatus(JoinGameResponse joinGameResponseDTO);
        Task CreateGameRoomId(string roomId);
        Task SendMsg(string msg);
    }
}
