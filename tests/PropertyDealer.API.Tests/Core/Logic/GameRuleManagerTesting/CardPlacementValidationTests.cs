using property_dealer_API.Core.Logic.GameRulesManager;
using property_dealer_API.Models.Enums.Cards;
using PropertyDealer.API.Tests.TestHelpers;

namespace PropertyDealer.API.Tests.Core.Logic.GameRuleManagerTesting
{
    public class CardPlacementValidationTests
    {
        private readonly GameRuleManager _gameRuleManager;

        public CardPlacementValidationTests()
        {
            _gameRuleManager = new GameRuleManager();
        }

        #region ValidateStandardPropertyCardDestination Tests

        [Fact]
        public void ValidateStandardPropertyCardDestination_ValidColor_ReturnsColor()
        {
            // Arrange
            var cardColor = PropertyCardColoursEnum.Red;

            // Act
            var result = _gameRuleManager.ValidateStandardPropertyCardDestination(cardColor);

            // Assert
            Assert.Equal(PropertyCardColoursEnum.Red, result);
        }

        [Fact]
        public void ValidateStandardPropertyCardDestination_NullColor_ThrowsInvalidOperationException()
        {
            // Arrange
            PropertyCardColoursEnum? cardColor = null;

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                _gameRuleManager.ValidateStandardPropertyCardDestination(cardColor));

            Assert.Contains("color cannot be null. Please select a valid color.", exception.Message);
        }

        #endregion

        #region ValidatePropertyPileCardType Tests

        [Fact]
        public void ValidatePropertyPileCardType_StandardSystemCard_DoesNotThrowException()
        {
            // Arrange
            var card = CardTestHelpers.CreateStandardSystemCard();

            // Act & Assert
            var exception = Record.Exception(() => _gameRuleManager.ValidatePropertyPileCardType(card));
            Assert.Null(exception);
        }

        [Theory]
        [InlineData(ActionTypes.HostileTakeover)]
        [InlineData(ActionTypes.ForcedTrade)]
        [InlineData(ActionTypes.PirateRaid)]
        [InlineData(ActionTypes.BountyHunter)]
        [InlineData(ActionTypes.TradeDividend)]
        [InlineData(ActionTypes.ExploreNewSector)]
        [InlineData(ActionTypes.SpaceStation)]
        [InlineData(ActionTypes.Starbase)]
        [InlineData(ActionTypes.TradeEmbargo)]
        [InlineData(ActionTypes.ShieldsUp)]
        public void ValidatePropertyPileCardType_CommandCard_ThrowsInvalidOperationException(ActionTypes actionType)
        {
            // Arrange
            var card = CardTestHelpers.CreateCommandCard(actionType);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                _gameRuleManager.ValidatePropertyPileCardType(card));

            Assert.Contains("Cannot play a CommandCard card on the property section", exception.Message);
        }

        [Fact]
        public void ValidatePropertyPileCardType_TributeCard_ThrowsInvalidOperationException()
        {
            // Arrange
            var card = CardTestHelpers.CreateTributeCard();

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                _gameRuleManager.ValidatePropertyPileCardType(card));

            Assert.Contains("Cannot play a TributeCard card on the property section", exception.Message);
        }

        [Fact]
        public void ValidatePropertyPileCardType_SystemWildCard_ThrowsInvalidOperationException()
        {
            // Arrange
            var card = CardTestHelpers.CreateSystemWildCard();

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                _gameRuleManager.ValidatePropertyPileCardType(card));

            Assert.Contains("Cannot play a SystemWildCard card on the property section", exception.Message);
        }

        [Fact]
        public void ValidatePropertyPileCardType_MoneyCard_ThrowsInvalidOperationException()
        {
            // Arrange
            var card = CardTestHelpers.CreateMoneyCard();

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                _gameRuleManager.ValidatePropertyPileCardType(card));

            Assert.Contains("Cannot play a MoneyCard card on the property section", exception.Message);
        }

        #endregion    

        #region ValidateCommandPileCardType Tests

        [Theory]
        [InlineData(ActionTypes.HostileTakeover)]
        [InlineData(ActionTypes.ForcedTrade)]
        [InlineData(ActionTypes.PirateRaid)]
        [InlineData(ActionTypes.BountyHunter)]
        [InlineData(ActionTypes.TradeDividend)]
        [InlineData(ActionTypes.ExploreNewSector)]
        [InlineData(ActionTypes.SpaceStation)]
        [InlineData(ActionTypes.Starbase)]
        [InlineData(ActionTypes.TradeEmbargo)]
        [InlineData(ActionTypes.ShieldsUp)]
        public void ValidateCommandPileCardType_CommandCard_DoesNotThrowException(ActionTypes actionType)
        {
            // Arrange
            var card = CardTestHelpers.CreateCommandCard(actionType);

            // Act & Assert
            var exception = Record.Exception(() => _gameRuleManager.ValidateCommandPileCardType(card));
            Assert.Null(exception);
        }

        [Fact]
        public void ValidateCommandPileCardType_TributeCard_DoesNotThrowException()
        {
            // Arrange
            var card = CardTestHelpers.CreateTributeCard();

            // Act & Assert
            var exception = Record.Exception(() => _gameRuleManager.ValidateCommandPileCardType(card));
            Assert.Null(exception);
        }

        [Fact]
        public void ValidateCommandPileCardType_SystemWildCard_DoesNotThrowException()
        {
            // Arrange
            var card = CardTestHelpers.CreateSystemWildCard();

            // Act & Assert
            var exception = Record.Exception(() => _gameRuleManager.ValidateCommandPileCardType(card));
            Assert.Null(exception);
        }

        [Fact]
        public void ValidateCommandPileCardType_StandardSystemCard_ThrowsInvalidOperationException()
        {
            // Arrange
            var card = CardTestHelpers.CreateStandardSystemCard();

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                _gameRuleManager.ValidateCommandPileCardType(card));

            Assert.Contains("Cannot play a SystemCard card on the command section", exception.Message);
        }

        [Fact]
        public void ValidateCommandPileCardType_MoneyCard_ThrowsInvalidOperationException()
        {
            // Arrange
            var card = CardTestHelpers.CreateMoneyCard();

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                _gameRuleManager.ValidateCommandPileCardType(card));

            Assert.Contains("Cannot play a MoneyCard card on the command section", exception.Message);
        }

        #endregion    

        #region ValidateMoneyPileCardType Tests

        [Fact]
        public void ValidateMoneyPileCardType_MoneyCard_DoesNotThrowException()
        {
            // Arrange
            var card = CardTestHelpers.CreateMoneyCard();

            // Act & Assert
            var exception = Record.Exception(() => _gameRuleManager.ValidateMoneyPileCardType(card));
            Assert.Null(exception);
        }

        [Theory]
        [InlineData(ActionTypes.HostileTakeover)]
        [InlineData(ActionTypes.ForcedTrade)]
        [InlineData(ActionTypes.PirateRaid)]
        [InlineData(ActionTypes.BountyHunter)]
        [InlineData(ActionTypes.TradeDividend)]
        [InlineData(ActionTypes.ExploreNewSector)]
        [InlineData(ActionTypes.SpaceStation)]
        [InlineData(ActionTypes.Starbase)]
        [InlineData(ActionTypes.TradeEmbargo)]
        [InlineData(ActionTypes.ShieldsUp)]
        public void ValidateMoneyPileCardType_CommandCard_DoesNotThrowException(ActionTypes actionType)
        {
            // Arrange
            var card = CardTestHelpers.CreateCommandCard(actionType);

            // Act & Assert
            var exception = Record.Exception(() => _gameRuleManager.ValidateMoneyPileCardType(card));
            Assert.Null(exception);
        }

        [Fact]
        public void ValidateMoneyPileCardType_TributeCard_DoesNotThrowException()
        {
            // Arrange
            var card = CardTestHelpers.CreateTributeCard();

            // Act & Assert
            var exception = Record.Exception(() => _gameRuleManager.ValidateMoneyPileCardType(card));
            Assert.Null(exception);
        }

        [Fact]
        public void ValidateMoneyPileCardType_StandardSystemCard_ThrowsInvalidOperationException()
        {
            // Arrange
            var card = CardTestHelpers.CreateStandardSystemCard();

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                _gameRuleManager.ValidateMoneyPileCardType(card));

            Assert.Contains("Cannot play a SystemCard card on the money section", exception.Message);
        }

        [Fact]
        public void ValidateMoneyPileCardType_SystemWildCard_ThrowsInvalidOperationException()
        {
            // Arrange
            var card = CardTestHelpers.CreateSystemWildCard();

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                _gameRuleManager.ValidateMoneyPileCardType(card));

            Assert.Contains("Cannot play a SystemWildCard card on the money section", exception.Message);
        }

        #endregion    
    }
}