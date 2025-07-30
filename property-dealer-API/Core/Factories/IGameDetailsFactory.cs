using property_dealer_API.Core.Entities;

namespace property_dealer_API.Core.Factories
{
    public interface IGameDetailsFactory
    {
        GameDetails CreateGameDetails(string roomId, string roomName, GameConfig config);
    }
}