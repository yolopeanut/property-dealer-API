using property_dealer_API.Application.DTOs.Responses;
using property_dealer_API.Core.Entities;

namespace property_dealer_API.Hubs.GameWaitingRoom.Service
{
    public interface IWaitingRoomService
    {
        List<Player> GetAllPlayers(string gameRoomId);
        Player GetPlayerByUserId(string gameRoomId, string userId);
        string RemovePlayerFromGame(string gameRoomId, string userId);
        GameConfig GetRoomConfig(string gameRoomId);
        Boolean DoesRoomExist(string gameRoomId);
        Boolean DoesPlayerExist(string userId, string gameRoomId);
        void StartGame(string gameRoomId);

        IEnumerable<GameListSummaryResponse> GetAllExistingRoomIds();
    }
}