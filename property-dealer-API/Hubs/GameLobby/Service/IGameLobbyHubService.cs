using property_dealer_API.Models.DTOs;
using property_dealer_API.Models.Enums;

namespace property_dealer_API.Hubs.GameLobby
{

    public interface IGameLobbyHubService
    {
        IEnumerable<GameListSummaryDTO> GetGameListSummary();

        string CreateRoom(string connectionId, string userId, string playerName, string roomName, CreateGameConfigDTO config);
        JoinGameResponseEnum JoinRoom(string gameRoomId, string connectionId, string userId, string playerName);
    }
}
