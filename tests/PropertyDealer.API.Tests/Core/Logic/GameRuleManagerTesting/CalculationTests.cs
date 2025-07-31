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
    public class CalculationTests
    {
        private readonly GameRuleManager _gameRuleManager;

        public CalculationTests()
        {
            _gameRuleManager = new GameRuleManager();
        }


        #region CalculateRentAmount Tests

        [Fact]
        public void CalculateRentAmount_SingleProperty_ReturnsFirstRentalValue()
        {
            // Arrange
            var tributeCard = new TributeCard(CardTypesEnum.TributeCard, 3,
                new List<PropertyCardColoursEnum> { PropertyCardColoursEnum.Red }, "Red Rent");
            var targetColor = PropertyCardColoursEnum.Red;
            var playerPropertyCards = new List<Card>
            {
                new StandardSystemCard(CardTypesEnum.SystemCard, "Red Property", 3,
                    PropertyCardColoursEnum.Red, "Test", 3, new List<int> { 2, 4, 7 })
            };

            // Act
            var result = _gameRuleManager.CalculateRentAmount(targetColor, playerPropertyCards);

            // Assert
            Assert.Equal(2, result); // First rental value
        }

        [Fact]
        public void CalculateRentAmount_MultipleProperties_ReturnsCorrectRentalValue()
        {
            // Arrange
            var tributeCard = new TributeCard(CardTypesEnum.TributeCard, 3,
                new List<PropertyCardColoursEnum> { PropertyCardColoursEnum.Red }, "Red Rent");
            var targetColor = PropertyCardColoursEnum.Red;
            var playerPropertyCards = new List<Card>
            {
                new StandardSystemCard(CardTypesEnum.SystemCard, "Red Property 1", 3,
                    PropertyCardColoursEnum.Red, "Test", 3, new List<int> { 2, 4, 7 }),
                new StandardSystemCard(CardTypesEnum.SystemCard, "Red Property 2", 3,
                    PropertyCardColoursEnum.Red, "Test", 3, new List<int> { 2, 4, 7 }),
                new StandardSystemCard(CardTypesEnum.SystemCard, "Cyan Property", 3,
                    PropertyCardColoursEnum.Cyan, "Test", 2, new List<int> { 1, 3 })
            };

            // Act
            var result = _gameRuleManager.CalculateRentAmount(targetColor, playerPropertyCards);

            // Assert
            Assert.Equal(4, result); // Second rental value (index 1)
        }

        [Fact]
        public void CalculateRentAmount_NoPropertiesOfTargetColor_ReturnsZero()
        {
            // Arrange
            var tributeCard = new TributeCard(CardTypesEnum.TributeCard, 3,
                new List<PropertyCardColoursEnum> { PropertyCardColoursEnum.Red }, "Red Rent");
            var targetColor = PropertyCardColoursEnum.Red;
            var playerPropertyCards = new List<Card>
            {
                new StandardSystemCard(CardTypesEnum.SystemCard, "Cyan Property", 3,
                    PropertyCardColoursEnum.Cyan, "Test", 2, new List<int> { 1, 3 }),
                new CommandCard(CardTypesEnum.CommandCard, ActionTypes.ExploreNewSector, "Explore", 2, "Move")
            };

            // Act
            var result = _gameRuleManager.CalculateRentAmount(targetColor, playerPropertyCards);

            // Assert
            Assert.Equal(0, result);
        }

        #endregion

        #region GetPaymentAmount Tests

        [Theory]
        [InlineData(ActionTypes.BountyHunter, 5)]
        [InlineData(ActionTypes.TradeDividend, 2)]
        public void GetPaymentAmount_ValidActionType_ReturnsCorrectAmount(ActionTypes actionType, int expectedAmount)
        {
            // Act
            var result = _gameRuleManager.GetPaymentAmount(actionType);

            // Assert
            Assert.Equal(expectedAmount, result);
        }

        [Theory]
        [InlineData(ActionTypes.ExploreNewSector)]
        [InlineData(ActionTypes.ShieldsUp)]
        [InlineData(ActionTypes.HostileTakeover)]
        public void GetPaymentAmount_InvalidActionType_ReturnsNull(ActionTypes actionType)
        {
            // Act
            var result = _gameRuleManager.GetPaymentAmount(actionType);

            // Assert
            Assert.Null(result);
        }

        #endregion
    }
}