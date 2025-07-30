using property_dealer_API.Core.Entities;
using property_dealer_API.Models.Enums;

namespace property_dealer_API.Core.Logic.PlayersManager
{
    public interface IPlayerManager : IReadOnlyPlayerManager
    {
        JoinGameResponseEnum AddPlayerToDict(Player player);
        string RemovePlayerFromDictByUserId(string userId);
    }
}