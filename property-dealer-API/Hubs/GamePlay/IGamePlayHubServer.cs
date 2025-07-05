using TypedSignalR.Client;

namespace property_dealer_API.Hubs.GameLobby
{
    [Hub]
    public interface IGamePlayHubServer
    {
        Task LeaveGameRoom(string gameRoomId, string userId);
        Task GetAllPlayerList(string gameRoomId);
    }
}
