using property_dealer_API.Application.Enums;
using property_dealer_API.Core;
using property_dealer_API.Core.Entities;
using property_dealer_API.Models.Enums.Cards;

namespace PropertyDealer.API.Tests.TestHelpers
{
    public static class PlayerTestHelpers
    {
        public static Player CreatePlayer(string userId = "user1", string playerName = "Player1")
        {
            return new Player { UserId = userId, PlayerName = playerName };
        }

        public static List<Player> CreatePlayerList(int count = 2)
        {
            var players = new List<Player>();
            for (int i = 1; i <= count; i++)
            {
                players.Add(CreatePlayer($"user{i}", $"Player{i}"));
            }
            return players;
        }

        public static ActionContext CreateActionContext(
            string cardId = "card1",
            string actionInitiatingPlayerId = "user1",
            ActionTypes actionType = ActionTypes.HostileTakeover,
            DialogTypeEnum dialogToOpen = DialogTypeEnum.PayValue)
        {
            return new ActionContext
            {
                CardId = cardId,
                ActionInitiatingPlayerId = actionInitiatingPlayerId,
                ActionType = actionType,
                DialogTargetList = new List<Player>(),
                DialogToOpen = dialogToOpen
            };
        }
    }
}