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
    public class ActionValidationTests
    {
        private readonly GameRuleManager _gameRuleManager;

        public ActionValidationTests()
        {
            _gameRuleManager = new GameRuleManager();
        }

        #region ValidateHostileTakeoverTarget Tests

        [Fact]
        public void ValidateHostileTakeoverTarget_CompletePropertySet_DoesNotThrowException()
        {
            // Arrange - Create a complete property set (3/3 Red properties)
            var targetPlayerTableHand = CreatePropertyGroups(
                (PropertyCardColoursEnum.Red, 3, 3) // Complete set
            );
            var targetColor = PropertyCardColoursEnum.Red;

            // Act & Assert
            var exception = Record.Exception(() =>
                _gameRuleManager.ValidateHostileTakeoverTarget(targetPlayerTableHand, targetColor));
            Assert.Null(exception);
        }

        [Fact]
        public void ValidateHostileTakeoverTarget_IncompletePropertySet_ThrowsIncompletePropertySetException()
        {
            // Arrange - Create incomplete property set (2/3 Red properties)
            var targetPlayerTableHand = CreatePropertyGroups(
                (PropertyCardColoursEnum.Red, 2, 3) // Incomplete set
            );
            var targetColor = PropertyCardColoursEnum.Red;

            // Act & Assert
            var exception = Assert.Throws<IncompletePropertySetException>(() =>
                _gameRuleManager.ValidateHostileTakeoverTarget(targetPlayerTableHand, targetColor));

            Assert.Equal(PropertyCardColoursEnum.Red, exception.Color);
            Assert.Equal(2, exception.CurrentCount);
            Assert.Equal(3, exception.RequiredCount);
        }

        [Fact]
        public void ValidateHostileTakeoverTarget_NoPropertiesOfTargetColor_ThrowsInvalidTargetException()
        {
            // Arrange - Create property set without target color
            var targetPlayerTableHand = CreatePropertyGroups(
                (PropertyCardColoursEnum.Cyan, 2, 2) // Only Cyan properties
            );
            var targetColor = PropertyCardColoursEnum.Red; // Target Red but player has no Red

            // Act & Assert
            var exception = Assert.Throws<InvalidTargetException>(() =>
                _gameRuleManager.ValidateHostileTakeoverTarget(targetPlayerTableHand, targetColor));

            Assert.Equal("Hostile Takeover", exception.ActionType);
            Assert.Equal(PropertyCardColoursEnum.Red, exception.TargetColor);
        }

        #endregion

        #region ValidatePirateRaidTarget Tests

        [Fact]
        public void ValidatePirateRaidTarget_IncompletePropertySet_DoesNotThrowException()
        {
            // Arrange - Create incomplete property set (2/3 Red properties)
            var targetPlayerTableHand = CreatePropertyGroups(
                (PropertyCardColoursEnum.Red, 2, 3) // Incomplete set
            );
            var targetColor = PropertyCardColoursEnum.Red;

            // Act & Assert
            var exception = Record.Exception(() =>
                _gameRuleManager.ValidatePirateRaidTarget(targetPlayerTableHand, targetColor));
            Assert.Null(exception);
        }

        [Fact]
        public void ValidatePirateRaidTarget_CompletePropertySet_ThrowsCompletePropertySetException()
        {
            // Arrange - Create complete property set (3/3 Red properties)
            var targetPlayerTableHand = CreatePropertyGroups(
                (PropertyCardColoursEnum.Red, 3, 3) // Complete set
            );
            var targetColor = PropertyCardColoursEnum.Red;

            // Act & Assert
            var exception = Assert.Throws<CompletePropertySetException>(() =>
                _gameRuleManager.ValidatePirateRaidTarget(targetPlayerTableHand, targetColor));

            Assert.Equal(PropertyCardColoursEnum.Red, exception.Color);
        }

        [Fact]
        public void ValidatePirateRaidTarget_NoPropertiesOfTargetColor_ThrowsInvalidTargetException()
        {
            // Arrange - Create property set without target color
            var targetPlayerTableHand = CreatePropertyGroups(
                (PropertyCardColoursEnum.Cyan, 1, 2) // Only Cyan properties
            );
            var targetColor = PropertyCardColoursEnum.Red; // Target Red but player has no Red

            // Act & Assert
            var exception = Assert.Throws<InvalidTargetException>(() =>
                _gameRuleManager.ValidatePirateRaidTarget(targetPlayerTableHand, targetColor));

            Assert.Equal("Pirate Raid", exception.ActionType);
            Assert.Equal(PropertyCardColoursEnum.Red, exception.TargetColor);
        }

        #endregion

        #region ValidateForcedTradeTarget Tests

        [Fact]
        public void ValidateForcedTradeTarget_IncompletePropertySet_DoesNotThrowException()
        {
            // Arrange - Create incomplete property set (2/3 Red properties)
            var targetPlayerTableHand = CreatePropertyGroups(
                (PropertyCardColoursEnum.Red, 2, 3) // Incomplete set
            );
            var targetColor = PropertyCardColoursEnum.Red;

            // Act & Assert
            var exception = Record.Exception(() =>
                _gameRuleManager.ValidateForcedTradeTarget(targetPlayerTableHand, targetColor));
            Assert.Null(exception);
        }

        [Fact]
        public void ValidateForcedTradeTarget_CompletePropertySet_ThrowsInvalidOperationException()
        {
            // Arrange - Create complete property set (3/3 Red properties)
            var targetPlayerTableHand = CreatePropertyGroups(
                (PropertyCardColoursEnum.Red, 3, 3) // Complete set
            );
            var targetColor = PropertyCardColoursEnum.Red;

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                _gameRuleManager.ValidateForcedTradeTarget(targetPlayerTableHand, targetColor));

            Assert.Contains("Forced Trade cannot target completed property sets", exception.Message);
            Assert.Contains("Red", exception.Message);
        }

        [Fact]
        public void ValidateForcedTradeTarget_NoPropertiesOfTargetColor_ThrowsInvalidOperationException()
        {
            // Arrange - Create property set without target color
            var targetPlayerTableHand = CreatePropertyGroups(
                (PropertyCardColoursEnum.Cyan, 1, 2) // Only Cyan properties
            );
            var targetColor = PropertyCardColoursEnum.Red; // Target Red but player has no Red

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                _gameRuleManager.ValidateForcedTradeTarget(targetPlayerTableHand, targetColor));

            Assert.Contains("Forced Trade cannot target the Red property set", exception.Message);
            Assert.Contains("doesn't own any Red properties", exception.Message);
        }

        #endregion

        #region ValidateSpaceStationPlacement Tests

        [Fact]
        public void ValidateSpaceStationPlacement_CompletePropertySet_DoesNotThrowException()
        {
            // Arrange - Create complete property set (3/3 Red properties)
            var playerTableHand = CreatePropertyGroups(
                (PropertyCardColoursEnum.Red, 3, 3) // Complete set
            );
            var targetColor = PropertyCardColoursEnum.Red;

            // Act & Assert
            var exception = Record.Exception(() =>
                _gameRuleManager.ValidateSpaceStationPlacement(playerTableHand, targetColor));
            Assert.Null(exception);
        }

        [Fact]
        public void ValidateSpaceStationPlacement_IncompletePropertySet_ThrowsInvalidOperationException()
        {
            // Arrange - Create incomplete property set (2/3 Red properties)
            var playerTableHand = CreatePropertyGroups(
                (PropertyCardColoursEnum.Red, 2, 3) // Incomplete set
            );
            var targetColor = PropertyCardColoursEnum.Red;

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                _gameRuleManager.ValidateSpaceStationPlacement(playerTableHand, targetColor));

            Assert.Contains("Space Station can only be played on completed property sets", exception.Message);
            Assert.Contains("Red set has 2/3 properties", exception.Message);
        }

        [Fact]
        public void ValidateSpaceStationPlacement_NoPropertiesOfTargetColor_ThrowsInvalidOperationException()
        {
            // Arrange - Create property set without target color
            var playerTableHand = CreatePropertyGroups(
                (PropertyCardColoursEnum.Cyan, 2, 2) // Only Cyan properties
            );
            var targetColor = PropertyCardColoursEnum.Red; // Target Red but player has no Red

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                _gameRuleManager.ValidateSpaceStationPlacement(playerTableHand, targetColor));

            Assert.Contains("Space Station cannot be played on the Red property set", exception.Message);
            Assert.Contains("don't own any Red properties", exception.Message);
        }

        #endregion

        #region ValidateStarbasePlacement Tests

        [Fact]
        public void ValidateStarbasePlacement_CompletePropertySet_DoesNotThrowException()
        {
            // Arrange - Create complete property set (2/2 Cyan properties)
            var playerTableHand = CreatePropertyGroups(
                (PropertyCardColoursEnum.Cyan, 2, 2) // Complete set
            );
            var targetColor = PropertyCardColoursEnum.Cyan;

            // Act & Assert
            var exception = Record.Exception(() =>
                _gameRuleManager.ValidateStarbasePlacement(playerTableHand, targetColor));
            Assert.Null(exception);
        }

        [Fact]
        public void ValidateStarbasePlacement_IncompletePropertySet_ThrowsInvalidOperationException()
        {
            // Arrange - Create incomplete property set (1/2 Cyan properties)
            var playerTableHand = CreatePropertyGroups(
                (PropertyCardColoursEnum.Cyan, 1, 2) // Incomplete set
            );
            var targetColor = PropertyCardColoursEnum.Cyan;

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                _gameRuleManager.ValidateStarbasePlacement(playerTableHand, targetColor));

            Assert.Contains("Starbase can only be played on completed property sets", exception.Message);
            Assert.Contains("Cyan set has 1/2 properties", exception.Message);
        }

        #endregion

        #region ValidateTradeEmbargoTarget Tests

        [Fact]
        public void ValidateTradeEmbargoTarget_PlayerHasPropertiesOfTargetColor_DoesNotThrowException()
        {
            // Arrange
            var targetPlayerTableHand = CreatePropertyGroups(
                (PropertyCardColoursEnum.Red, 2, 3) // Has Red properties
            );
            var targetColor = PropertyCardColoursEnum.Red;

            // Act & Assert
            var exception = Record.Exception(() =>
                _gameRuleManager.ValidateTradeEmbargoTarget(targetPlayerTableHand, targetColor));
            Assert.Null(exception);
        }

        [Fact]
        public void ValidateTradeEmbargoTarget_PlayerDoesNotHavePropertiesOfTargetColor_ThrowsInvalidTargetException()
        {
            // Arrange
            var targetPlayerTableHand = CreatePropertyGroups(
                (PropertyCardColoursEnum.Cyan, 1, 2) // Only Cyan properties
            );
            var targetColor = PropertyCardColoursEnum.Red; // Target Red but player has no Red

            // Act & Assert
            var exception = Assert.Throws<InvalidTargetException>(() =>
                _gameRuleManager.ValidateTradeEmbargoTarget(targetPlayerTableHand, targetColor));

            Assert.Equal("Trade Embargo", exception.ActionType);
            Assert.Equal(PropertyCardColoursEnum.Red, exception.TargetColor);
        }

        #endregion

        #region ValidateEndOfTurnCardLimit Tests

        [Theory]
        [InlineData(0)]
        [InlineData(3)]
        [InlineData(7)]
        public void ValidateEndOfTurnCardLimit_WithinLimit_DoesNotThrowException(int cardCount)
        {
            // Arrange
            var playerHand = CreateTestCards(cardCount);

            // Act & Assert
            var exception = Record.Exception(() =>
                _gameRuleManager.ValidateEndOfTurnCardLimit(playerHand));
            Assert.Null(exception);
        }

        [Theory]
        [InlineData(8, 1)]
        [InlineData(10, 3)]
        [InlineData(15, 8)]
        public void ValidateEndOfTurnCardLimit_ExceedsLimit_ThrowsInvalidOperationException(int cardCount, int expectedExcess)
        {
            // Arrange
            var playerHand = CreateTestCards(cardCount);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                _gameRuleManager.ValidateEndOfTurnCardLimit(playerHand));

            Assert.Contains($"You must discard {expectedExcess} card(s) to end your turn", exception.Message);
            Assert.Contains("Hand limit is 7 cards", exception.Message);
        }

        #endregion

        #region Helper Methods
        private List<PropertyCardGroup> CreatePropertyGroups(params (PropertyCardColoursEnum color, int currentCount, int maxCards)[] groupDefinitions)
        {
            var groups = new List<PropertyCardGroup>();

            foreach (var (color, currentCount, maxCards) in groupDefinitions)
            {
                var cardDtos = new List<CardDto>();

                for (int i = 0; i < currentCount; i++)
                {
                    cardDtos.Add(new CardDto
                    {
                        CardGuid = Guid.NewGuid(),
                        CardType = CardTypesEnum.SystemCard,
                        Name = $"{color} Property {i + 1}",
                        BankValue = 3,
                        Description = "Test property",
                        CardColoursEnum = color,
                        MaxCards = maxCards,
                        RentalValues = new List<int> { 2, 4, 7 }
                    });
                }

                groups.Add(new PropertyCardGroup(color, cardDtos));
            }

            return groups;
        }

        private List<Card> CreateTestCards(int count)
        {
            var cards = new List<Card>();
            for (int i = 0; i < count; i++)
            {
                cards.Add(new CommandCard(CardTypesEnum.CommandCard, ActionTypes.ExploreNewSector, $"Card {i}", 2, "Test card"));
            }
            return cards;
        }
        #endregion
    }
}