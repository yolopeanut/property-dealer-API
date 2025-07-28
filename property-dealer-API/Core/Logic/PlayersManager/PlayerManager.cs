using property_dealer_API.Application.Exceptions;
using property_dealer_API.Core.Entities;
using property_dealer_API.Models.Enums;
using System.Collections.Concurrent;

namespace property_dealer_API.Core.Logic.PlayersManager
{
    /// <summary>
    /// Manages the state for a game with a small number of players.
    /// </summary>
    public class PlayerManager : IReadOnlyPlayerManager
    {
        private ConcurrentDictionary<string, Player> Players { get; } = new ConcurrentDictionary<string, Player>();

        public PlayerManager()
        {
        }

        // Interface methods (read-only)
        public int CountPlayers()
        {
            return Players.Count;
        }

        public List<Player> GetAllPlayers()
        {
            return Players.Values.ToList();
        }

        public Player GetPlayerByUserId(string userId)
        {
            var player = Players.Values.FirstOrDefault(player => player.UserId == userId);

            if (player != null)
            {
                return player;
            }
            else
            {
                throw new PlayerNotFoundException(userId);
            }
        }

        public JoinGameResponseEnum AddPlayerToDict(Player player)
        {
            if (Players.TryAdd(player.UserId, player))
            {
                return JoinGameResponseEnum.JoinedSuccess;
            }
            else
            {
                //Found a player
                return JoinGameResponseEnum.AlreadyInGame;
            }
        }

        public string RemovePlayerFromDictByUserId(string userId)
        {
            if (Players.TryRemove(userId, out Player? player))
            {
                return player.PlayerName;
            }
            else
            {
                throw new PlayerNotFoundException(userId);
            }
        }
    }
}
