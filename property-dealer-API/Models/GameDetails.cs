using property_dealer_API.Models.DTOs;
using property_dealer_API.Models.Enums;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace property_dealer_API.Models
{
    public class GameDetails
    {
        public required string RoomId { get; set; }
        public required string RoomName { get; set; }
        public required GameStateEnum GameState { get; set; }
        public required GameConfig Config { get; set; }
        public ConcurrentDictionary<string, Player> Players { get; } = new ConcurrentDictionary<string, Player>();

        [SetsRequiredMembers]
        public GameDetails(string roomId, string roomName, GameConfig config, Player player)
        {
            this.RoomId = roomId;
            this.RoomName = roomName;
            this.GameState = GameStateEnum.WaitingRoom;
            this.Config = config;

            this.Players.TryAdd(player.UserId, player);
        }
    }
}
