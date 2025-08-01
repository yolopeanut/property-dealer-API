using property_dealer_API.Core.Entities;
using property_dealer_API.Core.Logic.PlayerHandsManager;
using property_dealer_API.Core.Logic.PlayersManager;
using property_dealer_API.Models.Cards;
using property_dealer_API.Models.Enums.Cards;
using property_dealer_API.Core.Logic.DecksManager;

namespace PropertyDealer.API.Tests.TestHelpers
{
    public static class GameStateTestHelpers
    {
        public static Player SetupPlayerInGame(
            IPlayerManager playerManager,
            IPlayerHandManager handManager,
            string userId = null,
            string playerName = null)
        {
            var player = PlayerTestHelpers.CreatePlayer(userId, playerName);
            playerManager.AddPlayerToDict(player);
            handManager.AddPlayerHand(player.UserId);
            return player;
        }

        public static void GivePlayerMoney(
            IPlayerHandManager handManager,
            string userId,
            int totalValue)
        {
            for (int i = 0; i < totalValue; i++)
            {
                var moneyCard = CardTestHelpers.CreateMoneyCard(1);
                handManager.AddCardToPlayerMoneyHand(userId, moneyCard);
            }
        }

        public static List<Card> GivePlayerPropertySet(
            IPlayerHandManager handManager,
            string userId,
            PropertyCardColoursEnum color,
            int count)
        {
            var properties = CardTestHelpers.CreatePropertyCardSet(color, count);
            foreach (var property in properties)
            {
                handManager.AddCardToPlayerTableHand(userId, property, color);
            }
            return properties;
        }

        public static void GivePlayerCards(
            IPlayerHandManager handManager,
            string userId,
            List<Card> cards)
        {
            handManager.AssignPlayerHand(userId, cards);
        }

        public static void PopulateTestDeck(IDeckManager deckManager, int cardCount = 20)
        {
            var testDeck = new List<Card>();

            // Add enough money cards for various scenarios
            for (int i = 1; i <= cardCount / 4; i++)
            {
                testDeck.Add(CardTestHelpers.CreateMoneyCard(1));
                testDeck.Add(CardTestHelpers.CreateMoneyCard(2));
                testDeck.Add(CardTestHelpers.CreateMoneyCard(5));
            }

            // Add various command cards that tests might need
            var commandTypes = new[] {
                ActionTypes.BountyHunter,
                ActionTypes.ForcedTrade,
                ActionTypes.PirateRaid,
                ActionTypes.ExploreNewSector,
                ActionTypes.TradeDividend
            };

            for (int i = 0; i < cardCount / 2; i++)
            {
                testDeck.Add(CardTestHelpers.CreateCommandCard(commandTypes[i % commandTypes.Length]));
            }

            // Add some property cards for property-related tests
            var colors = new[] {
                PropertyCardColoursEnum.Red,
                PropertyCardColoursEnum.Green,
                PropertyCardColoursEnum.Cyan
            };

            for (int i = 0; i < cardCount / 4; i++)
            {
                testDeck.Add(CardTestHelpers.CreateStandardSystemCard(colors[i % colors.Length]));
            }

            deckManager.PopulateInitialDeck(testDeck);
        }
    }
}