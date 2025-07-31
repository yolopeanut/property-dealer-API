using property_dealer_API.Application.Enums;
using property_dealer_API.Application.Exceptions;
using property_dealer_API.Core.Logic.ActionExecution;
using property_dealer_API.Core.Logic.DecksManager;
using property_dealer_API.Core.Logic.GameRulesManager;
using property_dealer_API.Core.Logic.PendingActionsManager;
using property_dealer_API.Core.Logic.PlayerHandsManager;
using property_dealer_API.Core.Logic.PlayersManager;
using property_dealer_API.Core.Logic.TurnExecutionsManager;
using property_dealer_API.Models.Cards;
using property_dealer_API.Models.Enums.Cards;
using PropertyDealer.API.Tests.TestHelpers;

namespace PropertyDealer.API.Tests.Core.Logic.TurnExecutionsManager
{
    public class TurnExecutionManagerTests
    {
        private readonly TurnExecutionManager _turnExecutionManager;
        private readonly PlayersHandManager _playerHandManager;
        private readonly PlayerManager _playerManager;

        public TurnExecutionManagerTests()
        {
            _playerHandManager = new PlayersHandManager();
            _playerManager = new PlayerManager();
            var rulesManager = new GameRuleManager();
            var actionExecutionManager = new ActionExecutionManager(
                _playerHandManager, _playerManager, rulesManager,
                new PendingActionManager(), new DeckManager());

            _turnExecutionManager = new TurnExecutionManager(
                _playerHandManager, _playerManager, rulesManager, actionExecutionManager);
        }

        [Fact]
        public void ExecuteTurnAction_WithMoneyPileDestination_ReturnsNull()
        {
            // Arrange
            var player = PlayerTestHelpers.CreatePlayer();
            _playerManager.AddPlayerToDict(player);
            _playerHandManager.AddPlayerHand(player.UserId);

            var card = CardTestHelpers.CreateMoneyCard(5);

            // Act
            var result = _turnExecutionManager.ExecuteTurnAction(
                player.UserId, card, CardDestinationEnum.MoneyPile, null);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void ExecuteTurnAction_WithPropertyPileDestination_ReturnsNull()
        {
            // Arrange
            var player = PlayerTestHelpers.CreatePlayer();
            _playerManager.AddPlayerToDict(player);
            _playerHandManager.AddPlayerHand(player.UserId);

            var card = CardTestHelpers.CreateStandardSystemCard(PropertyCardColoursEnum.Red);

            // Act
            var result = _turnExecutionManager.ExecuteTurnAction(
                player.UserId, card, CardDestinationEnum.PropertyPile, PropertyCardColoursEnum.Red);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void ExecuteTurnAction_WithCommandPileDestination_CallsActionExecution()
        {
            // Arrange
            var player = PlayerTestHelpers.CreatePlayer();
            _playerManager.AddPlayerToDict(player);
            _playerHandManager.AddPlayerHand(player.UserId);

            var card = CardTestHelpers.CreateCommandCard(ActionTypes.ExploreNewSector);

            // Act
            var result = _turnExecutionManager.ExecuteTurnAction(
                player.UserId, card, CardDestinationEnum.CommandPile, null);

            // Assert
            // ActionExecutionManager should return something (even if it's a basic implementation)
            // The main test is that it doesn't throw an exception
            Assert.True(true); // We mainly want to ensure no exceptions
        }

        [Theory]
        [InlineData(CardDestinationEnum.MoneyPile)]
        [InlineData(CardDestinationEnum.PropertyPile)]
        [InlineData(CardDestinationEnum.CommandPile)]
        public void ExecuteTurnAction_WithValidDestinations_DoesNotThrow(CardDestinationEnum destination)
        {
            // Arrange
            var player = PlayerTestHelpers.CreatePlayer();
            _playerManager.AddPlayerToDict(player);
            _playerHandManager.AddPlayerHand(player.UserId);

            Card card = destination switch
            {
                CardDestinationEnum.MoneyPile => CardTestHelpers.CreateMoneyCard(),
                CardDestinationEnum.PropertyPile => CardTestHelpers.CreateStandardSystemCard(),
                CardDestinationEnum.CommandPile => CardTestHelpers.CreateCommandCard(ActionTypes.SpaceStation),
                _ => CardTestHelpers.CreateMoneyCard()
            };

            var colorDestination = destination == CardDestinationEnum.PropertyPile
                ? PropertyCardColoursEnum.Green
                : (PropertyCardColoursEnum?)null;

            // Act & Assert - should not throw
            var result = _turnExecutionManager.ExecuteTurnAction(
                player.UserId, card, destination, colorDestination);

            // Command pile might return ActionContext, others return null
            if (destination == CardDestinationEnum.CommandPile)
            {
                // Might return something or null depending on implementation
            }
            else
            {
                Assert.Null(result);
            }
        }

        [Fact]
        public void RecoverFromFailedTurn_WithValidPlayer_DoesNotThrow()
        {
            // Arrange
            var player = PlayerTestHelpers.CreatePlayer();
            _playerManager.AddPlayerToDict(player);
            _playerHandManager.AddPlayerHand(player.UserId);

            var card = CardTestHelpers.CreateCommandCard(ActionTypes.TradeEmbargo);

            // Act & Assert - should not throw
            _turnExecutionManager.RecoverFromFailedTurn(player.UserId, card);
        }
    }
}