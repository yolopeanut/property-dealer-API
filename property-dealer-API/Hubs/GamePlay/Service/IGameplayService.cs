using property_dealer_API.Core.Entities;

namespace property_dealer_API.Hubs.GamePlay.Service
{
    public interface IGameplayService
    {
        bool DoesPlayerExist(string userId, string gameRoomId);
        bool DoesRoomExist(string gameRoomId);
        List<Player> GetAllPlayers(string gameRoomId);
        Player GetPlayerByUserId(string gameRoomId, string userId);
        string RemovePlayerFromGame(string gameRoomId, string userId);
    }
}
