using property_dealer_API.Core.Entities;

namespace property_dealer_API.Hubs.GameWaitingRoom.Service
{
    public interface IWaitingRoomService
    {
        public List<Player>? GetAllPlayers(string gameRoomLobbyId);

        GameConfig? GetRoomConfig(string gameRoomLobbyId);
    }
}