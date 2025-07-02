using TypedSignalR.Client;

namespace property_dealer_API.Hubs.GameWaitingRoom
{
    [Hub]
    public interface IWaitingRoomHubServer
    {
        Task GetAllPlayerList(string gameRoomLobbyId);
        Task GetGameRoomCfg(string gameRoomId);
    }
}
