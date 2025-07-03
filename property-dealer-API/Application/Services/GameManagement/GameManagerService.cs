using property_dealer_API.Application.DTOs.Responses;
using property_dealer_API.Core;
using System.Collections.Concurrent;

namespace property_dealer_API.Application.Services.GameManagement
{
    public class GameManagerService : IGameManagerService
    {
        private static readonly ConcurrentDictionary<string, GameDetails> _gamesDictConcurrent = new ConcurrentDictionary<string, GameDetails>();

        // Geting game list summary
        public IEnumerable<GameListSummaryResponse> GetGameListSummary()
        {
            var summaries = _gamesDictConcurrent.Select(
                item => new GameListSummaryResponse(
                    item.Key,
                    item.Value.RoomName,
                    item.Value.GetPlayers().Count,
                    Convert.ToInt16(item.Value.Config.MaxNumPlayers),
                    item.Value.GameState
                )
            );

            return summaries;
        }

        // Adding new game to dictionary
        public void AddNewGameToDict(string roomId, GameDetails gameDetails)
        {
            _gamesDictConcurrent.TryAdd(roomId, gameDetails);
        }

        public Boolean IsGameIdExisting(string roomId)
        {
            return _gamesDictConcurrent.TryGetValue(roomId, out GameDetails? _value);
        }

        public GameDetails? GetGameDetails(string roomId)
        {
            if (_gamesDictConcurrent.TryGetValue(roomId, out GameDetails? gameInstance))
            {
                return gameInstance;
            }
            else
            {
                return null;
            }
        }

        public void RemoveGame(string roomId)
        {
            _gamesDictConcurrent.TryRemove(roomId, out GameDetails? _);
        }
    }
}
