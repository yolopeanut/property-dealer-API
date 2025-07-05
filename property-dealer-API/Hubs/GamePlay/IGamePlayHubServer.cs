using TypedSignalR.Client;

namespace property_dealer_API.Hubs.GameLobby
{
    [Hub]
    public interface IGamePlayHubServer
    {
        Task LeaveGameRoom(string gameRoomId, string userId);
        Task GetAllPlayerList(string gameRoomId);
        Task GetPlayerHand(string gameRoomId, string userId);
        Task GetAllTableCard(string gameRoomId, string userId);
        Task PlayCard(string gameRoomId, string userId);
        Task DrawCard(string gameRoomId, string userId);

    }
}
