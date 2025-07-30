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
            var card = new StandardSystemCard(
                CardTypesEnum.SystemCard,
                "Red Property",
                3,
                PropertyCardColoursEnum.Red,
                "Description",
                3,
                new List<int> { 2, 4, 7 });

            // Act & Assert
            var exception = Record.Exception(() => _gameRuleManager.ValidatePropertyPileCardType(card));
            Assert.Null(exception);
        }

        [Fact]
        public void ValidatePropertyPileCardType_CommandCard_ThrowsInvalidOperationException()
        {
            // Arrange
            var card = new CommandCard(CardTypesEnum.CommandCard, ActionTypes.ExploreNewSector, "Explore", 2, "Description");

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                _gameRuleManager.ValidatePropertyPileCardType(card));

            Assert.Contains("Cannot play a CommandCard card on the property section", exception.Message);
        }

        #endregion    
    }
}