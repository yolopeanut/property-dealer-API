using System.Collections.Concurrent;
using property_dealer_API.Application.DTOs.Responses;
using property_dealer_API.Application.Exceptions;
using property_dealer_API.Core;
using property_dealer_API.Core.Entities;
using property_dealer_API.Core.Factories;

namespace property_dealer_API.Application.Services.GameManagement
{
    public class GameManagerService : IGameManagerService
    {
        private readonly ConcurrentDictionary<string, GameDetails> _gamesDictConcurrent = new();
        private readonly IGameDetailsFactory _gameDetailsFactory;

        public GameManagerService(IGameDetailsFactory gameDetailsFactory)
        {
            this._gameDetailsFactory = gameDetailsFactory;
        }

        // Geting game list summary
        public IEnumerable<GameListSummaryResponse> GetGameListSummary()
        {
            var summaries = this._gamesDictConcurrent.Select(item => new GameListSummaryResponse(
                item.Key,
                item.Value.RoomName,
                item.Value.PublicPlayerManager.CountPlayers(),
                Convert.ToInt16(item.Value.Config.MaxNumPlayers),
                item.Value.GameState
            ));

            return summaries;
        }

        public void CreateNewGame(string roomId, string roomName, GameConfig config)
        {
            var gameDetails = this._gameDetailsFactory.CreateGameDetails(roomId, roomName, config);
            this.AddNewGameToDict(roomId, gameDetails);
        }

        // Adding new game to dictionary
        public void AddNewGameToDict(string roomId, GameDetails gameDetails)
        {
            this._gamesDictConcurrent.TryAdd(roomId, gameDetails);
        }

        public Boolean IsGameIdExisting(string roomId)
        {
            return this._gamesDictConcurrent.TryGetValue(roomId, out GameDetails? _value);
        }

        public GameDetails GetGameDetails(string roomId)
        {
            if (this._gamesDictConcurrent.TryGetValue(roomId, out GameDetails? gameInstance))
            {
                return gameInstance;
            }
            else
            {
                throw new GameNotFoundException(roomId);
            }
        }

        public void RemoveGame(string roomId)
        {
            this._gamesDictConcurrent.TryRemove(roomId, out GameDetails? _);
        }

        public void RemakeGame(string roomId)
        {
            var gameDetails = this.GetGameDetails(roomId);
            this.RemoveGame(roomId);
            this.CreateNewGame(roomId, gameDetails.RoomName, gameDetails.Config);
        }
    }
}
