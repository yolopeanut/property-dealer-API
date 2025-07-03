using property_dealer_API.Core.Entities;
using TypedSignalR.Client;

namespace property_dealer_API.Hubs.GameWaitingRoom
{
    [Hub]
    public interface IWaitingRoomHubServer
    {
        Task GetAllPlayerList(string gameRoomId);
        Task GetGameRoomCfg(string gameRoomId);
        Task UpdateCfg(string gameRoomId, GameConfig newConfig);
        Task StartGame(string gameRoomId);
        Task LeaveWaitingRoom(string gameRoomId, string userId);
    }
}
