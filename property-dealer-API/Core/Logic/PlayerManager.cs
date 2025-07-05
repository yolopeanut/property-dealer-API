using property_dealer_API.Core.Entities;
using property_dealer_API.Models.Enums;
using property_dealer_API.Application.Exceptions;
using System.Collections.Concurrent;

namespace property_dealer_API.Core.Logic
{
    /// <summary>
    /// Manages the state for a game with a small number of players.
    /// </summary>
    public class PlayerManager
    {
        private ConcurrentDictionary<string, Player> Players { get; } = new ConcurrentDictionary<string, Player>();

        public PlayerManager(Player initialPlayer)
        {
            this.AddPlayerToDict(initialPlayer);
        }

        public int CountPlayers()
        {
            return this.Players.Count;
        }

        public List<Player> GetAllPlayers()
        {
            return this.Players.Values.ToList();
        }
        public Player GetPlayerByUserId(string userId)
        {
            var player = this.Players.Values.FirstOrDefault(player => player.UserId == userId);

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
            if (this.Players.TryAdd(player.UserId, player))
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
            if (this.Players.TryRemove(userId, out Player? player))
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
