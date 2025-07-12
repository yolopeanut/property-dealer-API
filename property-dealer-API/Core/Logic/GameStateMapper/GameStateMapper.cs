using property_dealer_API.Application.DTOs.Responses;
using property_dealer_API.Application.Exceptions;
using property_dealer_API.Core.Entities.Cards.CardRelatedEntities;
using property_dealer_API.Core.Logic.PlayerHandsManager;
using property_dealer_API.Core.Logic.PlayerManager;

namespace property_dealer_API.Core.Logic.GameStateMapper
{
    public class GameStateMapper
    {
        private readonly IReadOnlyPlayerHandManager _handManager;
        private readonly IReadOnlyPlayerManager _playermanager;

        public GameStateMapper(IReadOnlyPlayerHandManager handManager, IReadOnlyPlayerManager playerManager)
        {
            this._handManager = handManager;
            this._playermanager = playerManager;
        }

        public List<TableHands> GetAllTableHandsDto()
        {
            var allPlayer = this._playermanager.GetAllPlayers();

            List<TableHands> allHands = new();
            List<PropertyCardGroup> cardsDtoGroup;

            // Using process all table hands safely function to get all table hands with locking and change to DTO.
            // I know its overkill for a turn based game like this, but i wanted to implement it to learn.
            this._handManager.ProcessAllTableHandsSafely((userId, tableHand, moneyHand) =>
            {
                cardsDtoGroup = new List<PropertyCardGroup>();

                var tableHandDto = tableHand.Select(
                    cardGroups => new PropertyCardGroup(
                        cardGroups.Key,
                        cardGroups.Value.Select(card => card.ToDto())
                            .ToList()
                        )
                     ).ToList();

                var moneyHandDto = moneyHand.Select(card => card.ToDto()).ToList();

                var player = allPlayer.Find(player => player.UserId == userId);

                if (player == null) { throw new PlayerNotFoundException(userId); }

                allHands.Add(new TableHands(player, tableHandDto, moneyHandDto));
            });

            return allHands;
        }
    }
}
