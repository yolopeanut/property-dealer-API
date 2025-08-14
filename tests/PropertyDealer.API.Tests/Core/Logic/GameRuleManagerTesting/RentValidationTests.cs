using property_dealer_API.Core.Logic.GameRulesManager;
using property_dealer_API.Models.Cards;
using property_dealer_API.Models.Enums.Cards;

namespace PropertyDealer.API.Tests.Core.Logic.GameRuleManagerTesting
{
    public class RentValidationTests
    {
        private readonly GameRuleManager _gameRuleManager;

        public RentValidationTests()
        {
            this._gameRuleManager = new GameRuleManager();
        }

        #region ValidateRentCardColors Tests

        [Fact]
        public void ValidateRentCardColors_MatchingColors_DoesNotThrowException()
        {
            // Arrange
            var rentCardColor = PropertyCardColoursEnum.Red;
            var targetColor = PropertyCardColoursEnum.Red;

            // Act & Assert
            var exception = Record.Exception(() =>
                this._gameRuleManager.ValidateRentCardColors(rentCardColor, targetColor));
            Assert.Null(exception);
        }

        [Fact]
        public void ValidateRentCardColors_OmniSectorCard_DoesNotThrowException()
        {
            // Arrange - OmniSector can target any color
            var rentCardColor = PropertyCardColoursEnum.OmniSector;
            var targetColor = PropertyCardColoursEnum.Red;

            // Act & Assert
            var exception = Record.Exception(() =>
                this._gameRuleManager.ValidateRentCardColors(rentCardColor, targetColor));
            Assert.Null(exception);
        }

        [Fact]
        public void ValidateRentCardColors_MismatchedColors_ThrowsInvalidOperationException()
        {
            // Arrange
            var rentCardColor = PropertyCardColoursEnum.Red;
            var targetColor = PropertyCardColoursEnum.Cyan;

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                this._gameRuleManager.ValidateRentCardColors(rentCardColor, targetColor));

            Assert.Contains("Cannot use Red rent card to charge rent on Cyan properties", exception.Message);
            Assert.Contains("Colors must match", exception.Message);
        }

        #endregion

        #region ValidateWildcardRentTarget Tests

        [Fact]
        public void ValidateWildcardRentTarget_SelectedColorAvailable_DoesNotThrowException()
        {
            // Arrange
            var availableColors = new List<PropertyCardColoursEnum>
            {
                PropertyCardColoursEnum.Red,
                PropertyCardColoursEnum.Cyan,
                PropertyCardColoursEnum.Green
            };
            var selectedColor = PropertyCardColoursEnum.Red;

            // Act & Assert
            var exception = Record.Exception(() =>
                this._gameRuleManager.ValidateWildcardRentTarget(availableColors, selectedColor));
            Assert.Null(exception);
        }

        [Fact]
        public void ValidateWildcardRentTarget_SelectedColorNotAvailable_ThrowsInvalidOperationException()
        {
            // Arrange
            var availableColors = new List<PropertyCardColoursEnum>
            {
                PropertyCardColoursEnum.Red,
                PropertyCardColoursEnum.Cyan
            };
            var selectedColor = PropertyCardColoursEnum.Green; // Not in available colors

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                this._gameRuleManager.ValidateWildcardRentTarget(availableColors, selectedColor));

            Assert.Contains("Cannot charge rent for Green properties", exception.Message);
            Assert.Contains("Available colors are: Red, Cyan", exception.Message);
        }

        #endregion
    }
}