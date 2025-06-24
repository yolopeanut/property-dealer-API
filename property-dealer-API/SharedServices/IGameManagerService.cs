using property_dealer_API.Models;
using property_dealer_API.Models.DTOs;
using property_dealer_API.Models.Enums;

namespace property_dealer_API.SharedServices
{
    public interface IGameManagerService
    {
        IEnumerable<GameListSummaryDTO> GetGameListSummary();

        void AddNewGameToDict(string roomId, GameDetails gameDetails);

        JoinGameResponseEnum AddPlayerToDict(string roomId, Player player);

        Boolean IsGameIdExisting(string roomId);
    }
}
