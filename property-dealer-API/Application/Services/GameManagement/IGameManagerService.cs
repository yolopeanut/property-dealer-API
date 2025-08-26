using property_dealer_API.Application.DTOs.Responses;
using property_dealer_API.Core;
using property_dealer_API.Core.Entities;

namespace property_dealer_API.Application.Services.GameManagement
{
    public interface IGameManagerService
    {
        IEnumerable<GameListSummaryResponse> GetGameListSummary();
        void AddNewGameToDict(string roomId, GameDetails gameDetails);
        Boolean IsGameIdExisting(string roomId);
        GameDetails GetGameDetails(string roomId);
        void RemoveGame(string roomId);
        void RemakeGame(string roomId);
    }
}
