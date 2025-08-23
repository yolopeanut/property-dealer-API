using property_dealer_API.Application.Exceptions;
using property_dealer_API.Core.Logic.GameRulesManager;
using property_dealer_API.Models.Cards;
using property_dealer_API.Models.Enums.Cards;
using PropertyDealer.API.Tests.TestHelpers;

namespace PropertyDealer.API.Tests.Core.Logic.GameRuleManagerTesting
{
    public class ActionValidationTests
    {
        private readonly GameRuleManager _gameRuleManager;

        public ActionValidationTests()
        {
            this._gameRuleManager = new GameRuleManager();
        }

        #region ValidateHostileTakeoverTarget Tests

        [Fact]
        public void ValidateHostileTakeoverTarget_CompletePropertySet_DoesNotThrowException()
        {
            // Arrange - Create a complete property set (3/3 Red properties)
            var targetPlayerSelectedPropertySet = CardTestHelpers.CreatePropertyCardSet(
                PropertyCardColoursEnum.Red,
                3,
                3
            ); // Complete set
            var targetColor = PropertyCardColoursEnum.Red;

            // Act & Assert
            var exception = Record.Exception(() =>
                this._gameRuleManager.ValidateHostileTakeoverTarget(
                    targetPlayerSelectedPropertySet,
                    targetColor
                )
            );
            Assert.Null(exception);
        }

        [Fact]
        public void ValidateHostileTakeoverTarget_IncompletePropertySet_ThrowsInvalidOperationException()
        {
            // Arrange - Create incomplete property set (2/3 Red properties)
            var targetPlayerSelectedPropertySet = CardTestHelpers.CreatePropertyCardSet(
                PropertyCardColoursEnum.Red,
                2,
                3
            ); // Incomplete set
            var targetColor = PropertyCardColoursEnum.Red;

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                this._gameRuleManager.ValidateHostileTakeoverTarget(
                    targetPlayerSelectedPropertySet,
                    targetColor
                )
            );

            Assert.Contains(
                "HostileTakeover can only be used on completed property sets",
                exception.Message
            );
            Assert.Contains("Red set has 2/3 properties", exception.Message);
        }

        [Fact]
        public void ValidateHostileTakeoverTarget_EmptyPropertySet_ThrowsInvalidTargetException()
        {
            // Arrange - Empty property set
            var targetPlayerSelectedPropertySet = new List<Card>();
            var targetColor = PropertyCardColoursEnum.Red;

            // Act & Assert
            var exception = Assert.Throws<InvalidTargetException>(() =>
                this._gameRuleManager.ValidateHostileTakeoverTarget(
                    targetPlayerSelectedPropertySet,
                    targetColor
                )
            );

            Assert.Contains("player doesn't own any properties of this color", exception.Message);
        }

        #endregion

        #region ValidatePirateRaidTarget Tests

        [Fact]
        public void ValidatePirateRaidTarget_IncompletePropertySet_DoesNotThrowException()
        {
            // Arrange - Create incomplete property set (2/3 Red properties)
            var targetPlayerSelectedPropertySet = CardTestHelpers.CreatePropertyCardSet(
                PropertyCardColoursEnum.Red,
                2,
                3
            ); // Incomplete set
            var targetColor = PropertyCardColoursEnum.Red;

            // Act & Assert
            var exception = Record.Exception(() =>
                this._gameRuleManager.ValidatePirateRaidTarget(
                    targetPlayerSelectedPropertySet,
                    targetColor
                )
            );
            Assert.Null(exception);
        }

        [Fact]
        public void ValidatePirateRaidTarget_CompletePropertySet_ThrowsCompletePropertySetException()
        {
            // Arrange - Create complete property set (3/3 Red properties)
            var targetPlayerSelectedPropertySet = CardTestHelpers.CreatePropertyCardSet(
                PropertyCardColoursEnum.Red,
                3,
                3
            ); // Complete set
            var targetColor = PropertyCardColoursEnum.Red;

            // Act & Assert
            var exception = Assert.Throws<CompletePropertySetException>(() =>
                this._gameRuleManager.ValidatePirateRaidTarget(
                    targetPlayerSelectedPropertySet,
                    targetColor
                )
            );

            Assert.Equal(PropertyCardColoursEnum.Red, exception.Color);
        }

        [Fact]
        public void ValidatePirateRaidTarget_EmptyPropertySet_ThrowsInvalidTargetException()
        {
            // Arrange - Empty property set
            var targetPlayerSelectedPropertySet = new List<Card>();
            var targetColor = PropertyCardColoursEnum.Red;

            // Act & Assert
            var exception = Assert.Throws<InvalidTargetException>(() =>
                this._gameRuleManager.ValidatePirateRaidTarget(
                    targetPlayerSelectedPropertySet,
                    targetColor
                )
            );

            Assert.Equal("PirateRaid", exception.ActionType);
            Assert.Equal(PropertyCardColoursEnum.Red, exception.TargetColor);
        }

        #endregion

        #region ValidateForcedTradeTarget Tests

        [Fact]
        public void ValidateForcedTradeTarget_IncompletePropertySet_DoesNotThrowException()
        {
            // Arrange - Create incomplete property set (2/3 Red properties)
            var targetPlayerSelectedPropertySet = CardTestHelpers.CreatePropertyCardSet(
                PropertyCardColoursEnum.Red,
                2,
                3
            ); // Incomplete set
            var targetColor = PropertyCardColoursEnum.Red;

            // Act & Assert
            var exception = Record.Exception(() =>
                this._gameRuleManager.ValidateForcedTradeTarget(
                    targetPlayerSelectedPropertySet,
                    targetColor
                )
            );
            Assert.Null(exception);
        }

        [Fact]
        public void ValidateForcedTradeTarget_CompletePropertySet_ThrowsInvalidOperationException()
        {
            // Arrange - Create complete property set (3/3 Red properties)
            var targetPlayerSelectedPropertySet = CardTestHelpers.CreatePropertyCardSet(
                PropertyCardColoursEnum.Red,
                3,
                3
            ); // Complete set
            var targetColor = PropertyCardColoursEnum.Red;

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                this._gameRuleManager.ValidateForcedTradeTarget(
                    targetPlayerSelectedPropertySet,
                    targetColor
                )
            );

            Assert.Contains(
                "Forced Trade cannot target completed property sets",
                exception.Message
            );
            Assert.Contains("Red", exception.Message);
        }

        [Fact]
        public void ValidateForcedTradeTarget_EmptyPropertySet_ThrowsInvalidTargetException()
        {
            // Arrange - Empty property set
            var targetPlayerSelectedPropertySet = new List<Card>();
            var targetColor = PropertyCardColoursEnum.Red;

            // Act & Assert
            var exception = Assert.Throws<InvalidTargetException>(() =>
                this._gameRuleManager.ValidateForcedTradeTarget(
                    targetPlayerSelectedPropertySet,
                    targetColor
                )
            );

            Assert.Contains("player doesn't own any properties of this color", exception.Message);
        }

        #endregion

        #region ValidateSpaceStationPlacement Tests

        [Fact]
        public void ValidateSpaceStationPlacement_CompletePropertySet_DoesNotThrowException()
        {
            // Arrange - Create complete property set (3/3 Red properties)
            var playerTableHand = CardTestHelpers.CreatePropertyCardSet(
                PropertyCardColoursEnum.Red,
                3,
                3
            ); // Complete set
            var targetColor = PropertyCardColoursEnum.Red;

            // Act & Assert
            var exception = Record.Exception(() =>
                this._gameRuleManager.ValidateSpaceStationPlacement(playerTableHand, targetColor)
            );
            Assert.Null(exception);
        }

        [Fact]
        public void ValidateSpaceStationPlacement_IncompletePropertySet_ThrowsInvalidOperationException()
        {
            // Arrange - Create incomplete property set (2/3 Red properties)
            var playerTableHand = CardTestHelpers.CreatePropertyCardSet(
                PropertyCardColoursEnum.Red,
                2,
                3
            ); // Incomplete set
            var targetColor = PropertyCardColoursEnum.Red;

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                this._gameRuleManager.ValidateSpaceStationPlacement(playerTableHand, targetColor)
            );

            Assert.Contains(
                "SpaceStation can only be used on completed property sets",
                exception.Message
            );
            Assert.Contains("Red set has 2/3 properties", exception.Message);
        }

        [Fact]
        public void ValidateSpaceStationPlacement_EmptyPropertySet_ThrowsInvalidTargetException()
        {
            // Arrange - Empty property set
            var playerTableHand = new List<Card>();
            var targetColor = PropertyCardColoursEnum.Red;

            // Act & Assert
            var exception = Assert.Throws<InvalidTargetException>(() =>
                this._gameRuleManager.ValidateSpaceStationPlacement(playerTableHand, targetColor)
            );

            Assert.Contains("player doesn't own any properties of this color", exception.Message);
        }

        #endregion

        #region ValidateStarbasePlacement Tests

        [Fact]
        public void ValidateStarbasePlacement_CompletePropertySet_DoesNotThrowException()
        {
            // Arrange - Create complete property set (2/2 Cyan properties)
            var playerTableHand = CardTestHelpers.CreatePropertyCardSet(
                PropertyCardColoursEnum.Cyan,
                2,
                2
            ); // Complete set
            var spaceStationCommandCard = CardTestHelpers.CreateCommandCard(
                ActionTypes.SpaceStation
            );
            playerTableHand.Add(spaceStationCommandCard);

            var targetColor = PropertyCardColoursEnum.Cyan;

            // Act & Assert
            var exception = Record.Exception(() =>
                this._gameRuleManager.ValidateStarbasePlacement(playerTableHand, targetColor)
            );
            Assert.Null(exception);
        }

        [Fact]
        public void ValidateStarbasePlacement_MissingSpaceStationFirst_ThrowException()
        {
            // Arrange - Create complete property set (2/2 Cyan properties)
            var playerTableHand = CardTestHelpers.CreatePropertyCardSet(
                PropertyCardColoursEnum.Cyan,
                2,
                2
            ); // Complete set
            var targetColor = PropertyCardColoursEnum.Cyan;

            // Act & Assert
            var exception = Record.Exception(() =>
                this._gameRuleManager.ValidateStarbasePlacement(playerTableHand, targetColor)
            );
            Assert.Contains("Cannot place card without SpaceStation", exception.Message);
        }

        [Fact]
        public void ValidateStarbasePlacement_IncompletePropertySet_ThrowsInvalidOperationException()
        {
            // Arrange - Create incomplete property set (1/2 Cyan properties)
            var playerTableHand = CardTestHelpers.CreatePropertyCardSet(
                PropertyCardColoursEnum.Cyan,
                1,
                2
            ); // Incomplete set
            var targetColor = PropertyCardColoursEnum.Cyan;

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                this._gameRuleManager.ValidateStarbasePlacement(playerTableHand, targetColor)
            );

            Assert.Contains(
                "Starbase can only be used on completed property sets",
                exception.Message
            );
            Assert.Contains("Cyan set has 1/2 properties", exception.Message);
        }

        [Fact]
        public void ValidateStarbasePlacement_EmptyPropertySet_ThrowsInvalidTargetException()
        {
            // Arrange - Empty property set
            var playerTableHand = new List<Card>();
            var targetColor = PropertyCardColoursEnum.Cyan;

            // Act & Assert
            var exception = Assert.Throws<InvalidTargetException>(() =>
                this._gameRuleManager.ValidateStarbasePlacement(playerTableHand, targetColor)
            );

            Assert.Contains("player doesn't own any properties of this color", exception.Message);
        }

        #endregion

        #region ValidateTradeEmbargoTarget Tests

        [Fact]
        public void ValidateTradeEmbargoTarget_PlayerHasPropertiesOfTargetColor_DoesNotThrowException()
        {
            // Arrange
            var targetPlayerSelectedPropertySet = CardTestHelpers.CreatePropertyCardSet(
                PropertyCardColoursEnum.Red,
                2,
                3
            ); // Has Red properties
            var targetColor = PropertyCardColoursEnum.Red;

            // Act & Assert
            var exception = Record.Exception(() =>
                this._gameRuleManager.ValidateTradeEmbargoTarget(
                    targetPlayerSelectedPropertySet,
                    targetColor
                )
            );
            Assert.Null(exception);
        }

        [Fact]
        public void ValidateTradeEmbargoTarget_EmptyPropertySet_ThrowsInvalidTargetException()
        {
            // Arrange
            var targetPlayerSelectedPropertySet = new List<Card>();
            var targetColor = PropertyCardColoursEnum.Red;

            // Act & Assert
            var exception = Assert.Throws<InvalidTargetException>(() =>
                this._gameRuleManager.ValidateTradeEmbargoTarget(
                    targetPlayerSelectedPropertySet,
                    targetColor
                )
            );

            Assert.Equal("TradeEmbargo", exception.ActionType);
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
            var playerHand = this.CreateTestCards(cardCount);

            // Act & Assert
            var exception = Record.Exception(() =>
                this._gameRuleManager.ValidateEndOfTurnCardLimit(playerHand)
            );
            Assert.Null(exception);
        }

        [Theory]
        [InlineData(8, 1)]
        [InlineData(10, 3)]
        [InlineData(15, 8)]
        public void ValidateEndOfTurnCardLimit_ExceedsLimit_ThrowsInvalidOperationException(
            int cardCount,
            int expectedExcess
        )
        {
            // Arrange
            var playerHand = this.CreateTestCards(cardCount);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                this._gameRuleManager.ValidateEndOfTurnCardLimit(playerHand)
            );

            Assert.Contains(
                $"You must discard {expectedExcess} card(s) to end your turn",
                exception.Message
            );
            Assert.Contains("Hand limit is 7 cards", exception.Message);
        }

        #endregion

        #region Helper Methods

        private List<Card> CreateTestCards(int count)
        {
            var cards = new List<Card>();
            for (int i = 0; i < count; i++)
            {
                cards.Add(
                    CardTestHelpers.CreateCommandCard(
                        ActionTypes.ExploreNewSector,
                        $"Card {i}",
                        2,
                        "Test card"
                    )
                );
            }
            return cards;
        }

        #endregion
    }
}
