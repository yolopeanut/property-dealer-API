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
    public class GameLogicQueryTests
    {
        private readonly GameRuleManager _gameRuleManager;

        public GameLogicQueryTests()
        {
            _gameRuleManager = new GameRuleManager();
        }

        #region CheckIfPlayerWon Tests

        [Fact]
        public void CheckIfPlayerWon_ThreeCompleteSets_ReturnsTrue()
        {
            // Arrange - Create 3 complete property sets
            var tableHand = CreatePropertyGroups(
                (PropertyCardColoursEnum.Red, 3, 3),     // Complete set 1
                (PropertyCardColoursEnum.Cyan, 2, 2),    // Complete set 2  
                (PropertyCardColoursEnum.Green, 3, 3),   // Complete set 3
                (PropertyCardColoursEnum.Yellow, 1, 3)   // Incomplete set
            );

            // Act
            var result = _gameRuleManager.CheckIfPlayerWon(tableHand);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CheckIfPlayerWon_TwoCompleteSets_ReturnsFalse()
        {
            // Arrange - Create only 2 complete property sets
            var tableHand = CreatePropertyGroups(
                (PropertyCardColoursEnum.Red, 3, 3),     // Complete set 1
                (PropertyCardColoursEnum.Cyan, 2, 2),    // Complete set 2
                (PropertyCardColoursEnum.Green, 1, 3),   // Incomplete set 1
                (PropertyCardColoursEnum.Yellow, 2, 3)   // Incomplete set 2
            );

            // Act
            var result = _gameRuleManager.CheckIfPlayerWon(tableHand);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CheckIfPlayerWon_NoCompleteSets_ReturnsFalse()
        {
            // Arrange - Create all incomplete property sets
            var tableHand = CreatePropertyGroups(
                (PropertyCardColoursEnum.Red, 2, 3),     // Incomplete set 1
                (PropertyCardColoursEnum.Cyan, 1, 2),    // Incomplete set 2
                (PropertyCardColoursEnum.Green, 1, 3)    // Incomplete set 3
            );

            // Act
            var result = _gameRuleManager.CheckIfPlayerWon(tableHand);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region IsPropertySetComplete Tests

        [Fact]
        public void IsPropertySetComplete_CompleteSet_ReturnsTrue()
        {
            // Arrange - Create complete Red property set (3/3)
            var tableHand = CreatePropertyGroups(
                (PropertyCardColoursEnum.Red, 3, 3),
                (PropertyCardColoursEnum.Cyan, 1, 2)
            );

            // Act
            var result = _gameRuleManager.IsPropertySetComplete(tableHand, PropertyCardColoursEnum.Red);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsPropertySetComplete_IncompleteSet_ReturnsFalse()
        {
            // Arrange - Create incomplete Red property set (2/3)
            var tableHand = CreatePropertyGroups(
                (PropertyCardColoursEnum.Red, 2, 3),
                (PropertyCardColoursEnum.Cyan, 2, 2)
            );

            // Act
            var result = _gameRuleManager.IsPropertySetComplete(tableHand, PropertyCardColoursEnum.Red);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsPropertySetComplete_ColorNotFound_ReturnsFalse()
        {
            // Arrange - Create property sets without Red
            var tableHand = CreatePropertyGroups(
                (PropertyCardColoursEnum.Cyan, 2, 2),
                (PropertyCardColoursEnum.Green, 1, 3)
            );

            // Act
            var result = _gameRuleManager.IsPropertySetComplete(tableHand, PropertyCardColoursEnum.Red);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region IsPlayerHandEmpty Tests

        [Fact]
        public void IsPlayerHandEmpty_EmptyList_ReturnsTrue()
        {
            // Arrange
            var cards = new List<Card>();

            // Act
            var result = _gameRuleManager.IsPlayerHandEmpty(cards);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsPlayerHandEmpty_HasCards_ReturnsFalse()
        {
            // Arrange
            var cards = new List<Card>
            {
                new CommandCard(CardTypesEnum.CommandCard, ActionTypes.ExploreNewSector, "Explore", 2, "Description")
            };

            // Act
            var result = _gameRuleManager.IsPlayerHandEmpty(cards);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region DoesPlayerHaveShieldsUp Tests

        [Fact]
        public void DoesPlayerHaveShieldsUp_HasShieldsUpCard_ReturnsTrue()
        {
            // Arrange
            var player = new Player { UserId = "user1", PlayerName = "Player1" };
            var playerHand = new List<Card>
            {
                new CommandCard(CardTypesEnum.CommandCard, ActionTypes.ShieldsUp, "Shields Up", 4, "Defense card"),
                new CommandCard(CardTypesEnum.CommandCard, ActionTypes.ExploreNewSector, "Explore", 2, "Move card")
            };

            // Act
            var result = _gameRuleManager.DoesPlayerHaveShieldsUp(player, playerHand);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void DoesPlayerHaveShieldsUp_NoShieldsUpCard_ReturnsFalse()
        {
            // Arrange
            var player = new Player { UserId = "user1", PlayerName = "Player1" };
            var playerHand = new List<Card>
            {
                new CommandCard(CardTypesEnum.CommandCard, ActionTypes.ExploreNewSector, "Explore", 2, "Move card"),
                new CommandCard(CardTypesEnum.CommandCard, ActionTypes.BountyHunter, "Bounty", 3, "Attack card")
            };

            // Act
            var result = _gameRuleManager.DoesPlayerHaveShieldsUp(player, playerHand);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void DoesPlayerHaveShieldsUp_EmptyHand_ReturnsFalse()
        {
            // Arrange
            var player = new Player { UserId = "user1", PlayerName = "Player1" };
            var playerHand = new List<Card>();

            // Act
            var result = _gameRuleManager.DoesPlayerHaveShieldsUp(player, playerHand);

            // Assert
            Assert.False(result);
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