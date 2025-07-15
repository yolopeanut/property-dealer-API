using property_dealer_API.Core.Entities;

namespace property_dealer_API.Core.Logic.PlayersManager
{
    public interface IReadOnlyPlayerManager
    {
        List<Player> GetAllPlayers();
        Player GetPlayerByUserId(string userId);
        int CountPlayers();
    }
}
