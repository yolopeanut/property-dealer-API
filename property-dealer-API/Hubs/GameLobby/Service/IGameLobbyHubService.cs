using property_dealer_API.Application.DTOs.Responses;
using property_dealer_API.Core.Entities;
using property_dealer_API.Models.Enums;

namespace property_dealer_API.Hubs.GameLobby
{

    public interface IGameLobbyHubService
    {
        IEnumerable<GameListSummaryResponse> GetGameListSummary();

        string CreateRoom(string userId, string playerName, string roomName, GameConfig config);
        JoinGameResponseEnum JoinRoom(string gameRoomId, string connectionId, string userId, string playerName);
    }
}
