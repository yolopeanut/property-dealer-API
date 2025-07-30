using property_dealer_API.Application.DTOs.Responses;
using property_dealer_API.Application.Enums;
using property_dealer_API.Application.Exceptions;
using property_dealer_API.Core.Entities;
using property_dealer_API.Core.Entities.Cards.CardRelatedEntities;
using property_dealer_API.Core.Logic.GameRulesManager;
using property_dealer_API.Models.Cards;
using property_dealer_API.Models.Enums;
using property_dealer_API.Models.Enums.Cards;

namespace PropertyDealer.API.Tests.Core.Logic.GameRuleManagerTesting
{
    public class RentValidationTests
    {
        private readonly GameRuleManager _gameRuleManager;

        public RentValidationTests()
        {
            _gameRuleManager = new GameRuleManager();
        }

        #region ValidateRentTarget Tests

        [Fact]
        public void ValidateRentTarget_PlayerHasTargetColorProperties_DoesNotThrowException()
        {
            // Arrange
            var targetColor = PropertyCardColoursEnum.Red;
            var targetPlayerProperties = new List<Card>
            {
                new StandardSystemCard(CardTypesEnum.SystemCard, "Red Property", 3, PropertyCardColoursEnum.Red, "Test", 3, new List<int> { 2, 4, 7 }),
                new StandardSystemCard(CardTypesEnum.SystemCard, "Cyan Property", 3, PropertyCardColoursEnum.Cyan, "Test", 2, new List<int> { 1, 3 })
            };

            // Act & Assert
            var exception = Record.Exception(() =>
                _gameRuleManager.ValidateRentTarget(targetColor, targetPlayerProperties));
            Assert.Null(exception);
        }

        [Fact]
        public void ValidateRentTarget_PlayerDoesNotHaveTargetColorProperties_ThrowsInvalidOperationException()
        {
            // Arrange
            var targetColor = PropertyCardColoursEnum.Red;
            var targetPlayerProperties = new List<Card>
            {
                new StandardSystemCard(CardTypesEnum.SystemCard, "Cyan Property", 3, PropertyCardColoursEnum.Cyan, "Test", 2, new List<int> { 1, 3 }),
                new CommandCard(CardTypesEnum.CommandCard, ActionTypes.ExploreNewSector, "Explore", 2, "Move card")
            };

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                _gameRuleManager.ValidateRentTarget(targetColor, targetPlayerProperties));

            Assert.Contains("Cannot charge rent for Red properties", exception.Message);
            Assert.Contains("doesn't own any Red properties", exception.Message);
        }

        #endregion

        #region ValidateRentCardColors Tests

        [Fact]
        public void ValidateRentCardColors_MatchingColors_DoesNotThrowException()
        {
            // Arrange
            var rentCardColor = PropertyCardColoursEnum.Red;
            var targetColor = PropertyCardColoursEnum.Red;

            // Act & Assert
            var exception = Record.Exception(() =>
                _gameRuleManager.ValidateRentCardColors(rentCardColor, targetColor));
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
                _gameRuleManager.ValidateRentCardColors(rentCardColor, targetColor));
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
                _gameRuleManager.ValidateRentCardColors(rentCardColor, targetColor));

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
                _gameRuleManager.ValidateWildcardRentTarget(availableColors, selectedColor));
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
                _gameRuleManager.ValidateWildcardRentTarget(availableColors, selectedColor));

            Assert.Contains("Cannot charge rent for Green properties", exception.Message);
            Assert.Contains("Available colors are: Red, Cyan", exception.Message);
        }

        #endregion
    }
}