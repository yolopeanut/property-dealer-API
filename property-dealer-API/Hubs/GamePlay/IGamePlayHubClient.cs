
using TypedSignalR.Client;

namespace property_dealer_API.Hubs.GamePlay
{

    [Receiver]
    public interface IGamePlayHubClient
    {
        Task AllGameRoomPlayerList(object allPlayers);
        Task ErrorMsg(string message);
        Task PlayerLeft(string playerName);
    }
}
