using property_dealer_API.Core.Entities;
using property_dealer_API.Models.Enums;
using System.Collections.Concurrent;

namespace property_dealer_API.Core.Logic
{
    public class PlayerManager
    {
        public ConcurrentDictionary<string, Player> Players { get; } = new ConcurrentDictionary<string, Player>();

        public PlayerManager(Player initialPlayer)
        {
            this.AddPlayerToDict(initialPlayer);
        }

        public List<Player> GetAllPlayers()
        {
            return this.Players.Values.ToList();
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
    }
}
