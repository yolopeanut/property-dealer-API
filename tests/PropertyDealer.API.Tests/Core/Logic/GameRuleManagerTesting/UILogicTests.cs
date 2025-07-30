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
    public class UILogicTests
    {
        private readonly GameRuleManager _gameRuleManager;

        public UILogicTests()
        {
            _gameRuleManager = new GameRuleManager();
        }

        #region IdentifyWhoSeesDialog Tests

        [Fact]
        public void IdentifyWhoSeesDialog_PayValueWithSpecificTarget_ReturnsTargetUser()
        {
            // Arrange
            var callerUser = new Player { UserId = "caller", PlayerName = "Caller" };
            var targetUser = new Player { UserId = "target", PlayerName = "Target" };
            var otherPlayer = new Player { UserId = "other", PlayerName = "Other" };
            var playerList = new List<Player> { callerUser, targetUser, otherPlayer };

            // Act
            var result = _gameRuleManager.IdentifyWhoSeesDialog(callerUser, targetUser, playerList, DialogTypeEnum.PayValue);

            // Assert
            Assert.Single(result);
            Assert.Equal("target", result[0].UserId);
        }

        [Fact]
        public void IdentifyWhoSeesDialog_PayValueWithNullTarget_ReturnsAllExceptCaller()
        {
            // Arrange
            var callerUser = new Player { UserId = "caller", PlayerName = "Caller" };
            var targetUser = new Player { UserId = "target", PlayerName = "Target" };
            var otherPlayer = new Player { UserId = "other", PlayerName = "Other" };
            var playerList = new List<Player> { callerUser, targetUser, otherPlayer };

            // Act
            var result = _gameRuleManager.IdentifyWhoSeesDialog(callerUser, null, playerList, DialogTypeEnum.PayValue);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.DoesNotContain(result, p => p.UserId == "caller");
            Assert.Contains(result, p => p.UserId == "target");
            Assert.Contains(result, p => p.UserId == "other");
        }

        [Theory]
        [InlineData(DialogTypeEnum.PlayerSelection)]
        [InlineData(DialogTypeEnum.PropertySetSelection)]
        [InlineData(DialogTypeEnum.TableHandSelector)]
        [InlineData(DialogTypeEnum.WildcardColor)]
        public void IdentifyWhoSeesDialog_CallerOnlyDialogs_ReturnsCallerOnly(DialogTypeEnum dialogType)
        {
            // Arrange
            var callerUser = new Player { UserId = "caller", PlayerName = "Caller" };
            var targetUser = new Player { UserId = "target", PlayerName = "Target" };
            var playerList = new List<Player> { callerUser, targetUser };

            // Act
            var result = _gameRuleManager.IdentifyWhoSeesDialog(callerUser, targetUser, playerList, dialogType);

            // Assert
            Assert.Single(result);
            Assert.Equal("caller", result[0].UserId);
        }

        [Fact]
        public void IdentifyWhoSeesDialog_ShieldsUpWithTarget_ReturnsTargetOnly()
        {
            // Arrange
            var callerUser = new Player { UserId = "caller", PlayerName = "Caller" };
            var targetUser = new Player { UserId = "target", PlayerName = "Target" };
            var playerList = new List<Player> { callerUser, targetUser };

            // Act
            var result = _gameRuleManager.IdentifyWhoSeesDialog(callerUser, targetUser, playerList, DialogTypeEnum.ShieldsUp);

            // Assert
            Assert.Single(result);
            Assert.Equal("target", result[0].UserId);
        }

        [Fact]
        public void IdentifyWhoSeesDialog_ShieldsUpWithNullTarget_ThrowsInvalidOperationException()
        {
            // Arrange
            var callerUser = new Player { UserId = "caller", PlayerName = "Caller" };
            var playerList = new List<Player> { callerUser };

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                _gameRuleManager.IdentifyWhoSeesDialog(callerUser, null, playerList, DialogTypeEnum.ShieldsUp));

            Assert.Contains("Cannot give ShieldsUp dialog if target user is null", exception.Message);
        }

        #endregion

        #region Helper Methods
        private List<Player> CreateTestPlayers(int count)
        {
            var players = new List<Player>();
            for (int i = 1; i <= count; i++)
            {
                players.Add(new Player
                {
                    UserId = $"user{i}",
                    PlayerName = $"Player{i}"
                });
            }
            return players;
        }
        #endregion
    }
}