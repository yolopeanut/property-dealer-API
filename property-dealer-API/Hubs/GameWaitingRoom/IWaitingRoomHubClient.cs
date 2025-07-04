using property_dealer_API.Core.Entities;
using TypedSignalR.Client;

namespace property_dealer_API.Hubs.GameWaitingRoom
{

    [Receiver]
    public interface IWaitingRoomHubClient
    {
        Task AllGameRoomPlayerList(List<Player> allPlayers);
        Task GameRoomCfg(GameConfig gameConfig);
        Task GameStarted(string roomId);
        Task ErrorMsg(string message);
        Task PlayerJoined(string name);
        Task PlayerLeft(string name);
    }
}
