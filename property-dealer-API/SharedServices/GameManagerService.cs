using property_dealer_API.Models;
using property_dealer_API.Models.DTOs;
using property_dealer_API.Models.Enums;
using System.Collections.Concurrent;

namespace property_dealer_API.SharedServices
{
    public class GameManagerService : IGameManagerService
    {
        // This class will be holding all the main shared states between the lobby and the gameplay, such as number of players, game progress, game room name, etc
        // The role will be holding any functions related to data retrieval & data adding. 
        private static readonly ConcurrentDictionary<string, GameDetails> _gamesDictConcurrent = new ConcurrentDictionary<string, GameDetails>();
        // Geting game list summary
        public IEnumerable<GameListSummaryDTO> GetGameListSummary()
        {
            var summaries = _gamesDictConcurrent.Select(
                item => new GameListSummaryDTO(
                    item.Key,
                    item.Value.RoomName,
                    item.Value.Players.Count,
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
            return _gamesDictConcurrent.TryGetValue(roomId, out GameDetails? value);


        }

        public JoinGameResponseEnum AddPlayerToDict(string roomId, Player player)
        {
            if (!_gamesDictConcurrent.TryGetValue(roomId, out GameDetails? gameDetails))
            {
                return JoinGameResponseEnum.GameRoomNotFound;
            }

            if (gameDetails.Players.TryAdd(player.UserId, player))
            {
                return JoinGameResponseEnum.JoinedSuccess;
            }
            else
            {
                //Found a player
                return JoinGameResponseEnum.AlreadyInGame;
            }
        }

        public List<Player> GetAllPlayers(string roomId)
        {
            if (_gamesDictConcurrent.TryGetValue(roomId, out GameDetails? gameList))
            {
                return [.. gameList.Players.Values];
            }
            else return [];
        }

        public GameConfig? GetGameRoomConfig(string roomId)
        {
            if (_gamesDictConcurrent.TryGetValue(roomId, out GameDetails? gameDetails))
            {
                return gameDetails.Config;
            }
            else
            {
                return null;
            }
        }
    }
}
