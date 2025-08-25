using property_dealer_API.Application.DTOs.Responses;
using property_dealer_API.Application.Exceptions;
using property_dealer_API.Core.Entities.Cards.CardRelatedEntities;
using property_dealer_API.Core.Logic.PlayerHandsManager;
using property_dealer_API.Core.Logic.PlayersManager;

namespace property_dealer_API.Core.Logic.GameStateMapper
{
    public class GameStateMapper : IGameStateMapper
    {
        private readonly IReadOnlyPlayerHandManager _handManager;
        private readonly IReadOnlyPlayerManager _playermanager;

        public GameStateMapper(
            IReadOnlyPlayerHandManager handManager,
            IReadOnlyPlayerManager playerManager
        )
        {
            this._handManager = handManager;
            this._playermanager = playerManager;
        }

        public List<TableHands> GetAllTableHandsDto()
        {
            var allPlayer = this._playermanager.GetAllPlayers();

            List<TableHands> allHands = new();
            List<PropertyCardGroup> cardsDtoGroup;

            this._handManager.ProcessAllTableHandsSafely(
                (userId, tableHand, moneyHand) =>
                {
                    cardsDtoGroup = new List<PropertyCardGroup>();

                    var tableHandDto = tableHand
                        .Select(cardGroups => new PropertyCardGroup(
                            cardGroups.Key,
                            cardGroups.Value.Select(card => card.ToDto()).ToList()
                        ))
                        .ToList();

                    var moneyHandDto = moneyHand.Select(card => card.ToDto()).ToList();

                    var player = allPlayer.Find(player => player.UserId == userId);

                    if (player == null)
                    {
                        throw new PlayerNotFoundException(userId);
                    }

                    allHands.Add(new TableHands(player, tableHandDto, moneyHandDto));
                }
            );

            return allHands;
        }
    }
}
