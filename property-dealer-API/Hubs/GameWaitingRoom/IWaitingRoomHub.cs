using property_dealer_API.Models;
using property_dealer_API.Models.DTOs;

namespace property_dealer_API.Hubs.GameWaitingRoom
{
    public interface IWaitingRoomHub
    {
        Task AllGameRoomPlayerList(List<Player> allPlayers);
        Task GameRoomCfg(GameConfig gameConfig);

        Task ErrorMsg(string message);
    }
}
