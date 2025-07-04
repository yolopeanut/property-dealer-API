using property_dealer_API.Application.DTOs.Responses;
using property_dealer_API.Core;
using property_dealer_API.Models;
using property_dealer_API.Models.Cards;
using property_dealer_API.Models.Enums;

namespace property_dealer_API.Application.Services.GameManagement
{
    public interface IGameManagerService
    {
        IEnumerable<GameListSummaryResponse> GetGameListSummary();
        void AddNewGameToDict(string roomId, GameDetails gameDetails);
        Boolean IsGameIdExisting(string roomId);
        GameDetails? GetGameDetails(string roomId);
        void RemoveGame(string roomId);
    }
}
