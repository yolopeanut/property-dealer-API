using property_dealer_API.Application.Exceptions;
using property_dealer_API.Core.Entities;
using property_dealer_API.Core.Logic.GameRulesManager;
using property_dealer_API.Models.Enums;

namespace PropertyDealer.API.Tests.Core.Logic.GameRuleManagerTesting
{
    public class GameStateValidationTests
    {
        private readonly GameRuleManager _gameRuleManager;

        public GameStateValidationTests()
        {
            this._gameRuleManager = new GameRuleManager();
        }

        #region ValidatePlayerJoining Tests

        [Fact]
        public void ValidatePlayerJoining_WaitingRoomWithSpace_ReturnsNull()
        {
            // Arrange - Set up test data
            var gameState = GameStateEnum.WaitingRoom;
            var players = new List<Player>
            {
                new Player { UserId = "user1", PlayerName = "Player1" },
                new Player { UserId = "user2", PlayerName = "Player2" }
            };
            var maxNumPlayers = "4";

            // Act - Execute the method being tested
            var result = this._gameRuleManager.ValidatePlayerJoining(gameState, players, maxNumPlayers);

            // Assert - Verify the result
            Assert.Null(result); // null means validation passed
        }

        [Fact]
        public void ValidatePlayerJoining_GameNotInWaitingRoom_ReturnsAlreadyInGame()
        {
            // Arrange
            var gameState = GameStateEnum.GameStarted; // Game already started
            var players = new List<Player> { new Player { UserId = "user1", PlayerName = "Player1" } };
            var maxNumPlayers = "4";

            // Act
            var result = this._gameRuleManager.ValidatePlayerJoining(gameState, players, maxNumPlayers);

            // Assert
            Assert.Equal(JoinGameResponseEnum.AlreadyInGame, result);
        }

        [Fact]
        public void ValidatePlayerJoining_GameFull_ReturnsGameFull()
        {
            // Arrange
            var gameState = GameStateEnum.WaitingRoom;
            var players = new List<Player>
            {
                new Player { UserId = "user1", PlayerName = "Player1" },
                new Player { UserId = "user2", PlayerName = "Player2" },
                new Player { UserId = "user3", PlayerName = "Player3" },
                new Player { UserId = "user4", PlayerName = "Player4" }
            };
            var maxNumPlayers = "4"; // Adding one more would exceed limit

            // Act
            var result = this._gameRuleManager.ValidatePlayerJoining(gameState, players, maxNumPlayers);

            // Assert
            Assert.Equal(JoinGameResponseEnum.GameFull, result);
        }

        [Theory]
        [InlineData("1")]
        [InlineData("2")]
        [InlineData("3")]
        public void ValidatePlayerJoining_DifferentMaxPlayers_RespectsLimit(string maxPlayers)
        {
            // Arrange
            var gameState = GameStateEnum.WaitingRoom;
            var maxPlayersInt = Convert.ToInt32(maxPlayers);
            var players = new List<Player>();

            // Create exactly the maximum number of players
            for (int i = 0; i < maxPlayersInt; i++)
            {
                players.Add(new Player { UserId = $"user{i}", PlayerName = $"Player{i}" });
            }

            // Act
            var result = this._gameRuleManager.ValidatePlayerJoining(gameState, players, maxPlayers);

            // Assert
            Assert.Equal(JoinGameResponseEnum.GameFull, result);
        }

        #endregion

        #region ValidatePlayerCanPlayCard Tests

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public void ValidatePlayerCanPlayCard_GameStartedAndCorrectTurnAndCorrectNumActionsPlayed_DoesNotThrowException(int noOfActionsPlayed)
        {
            // Arrange
            var gameState = GameStateEnum.GameStarted;
            var playerId = "player1";
            var currentTurnPlayerId = "player1";

            // Act
            var exception = Record.Exception(() => this._gameRuleManager.ValidatePlayerCanPlayCard(gameState, playerId, currentTurnPlayerId, noOfActionsPlayed));

            // Assert
            Assert.Null(exception);
        }

        [Theory]
        [InlineData(5)]
        [InlineData(10)]
        [InlineData(20)]
        public void ValidatePlayerCanPlayCard_PlayedTooManyTurns_DoesNotThrowException(int noOfActionsPlayed)
        {
            // Arrange
            var gameState = GameStateEnum.GameStarted;
            var playerId = "player1";
            var currentTurnPlayerId = "player1";

            // Act
            var exception = Assert.Throws<PlayerExceedingActionLimitException>(() => this._gameRuleManager.ValidatePlayerCanPlayCard(gameState, playerId, currentTurnPlayerId, noOfActionsPlayed));

            // Assert
            Assert.NotNull(exception);
            Assert.Contains("playing more than allowed", exception.Message);
        }

        [Theory]
        [InlineData(GameStateEnum.TimeOut)]
        [InlineData(GameStateEnum.WaitingRoom)]
        [InlineData(GameStateEnum.GamePaused)]
        [InlineData(GameStateEnum.GameOver)]
        public void ValidatePlayerCanPlayCard_GameIncorrectState_ThrowsInvalidOperationException(GameStateEnum currGameState)
        {
            // Arrange
            var playerId = "player1";
            var currentTurnPlayerId = "player1";
            var noOfActionsPlayed = 1;

            // Act and assert
            var exception = Assert.Throws<InvalidGameStateException>(() => this._gameRuleManager.ValidatePlayerCanPlayCard(currGameState, playerId, currentTurnPlayerId, noOfActionsPlayed));
            Assert.Contains("Cannot play cards when game is in", exception.Message);
        }

        [Fact]
        public void ValidatePlayerCanPlayCard_WrongPlayerTurn_ThrowsNotPlayerTurnException()
        {
            // Arrange
            var gameState = GameStateEnum.GameStarted;
            var playerId = "player1";
            var currentTurnPlayerId = "player2";
            var noOfActionsPlayed = 1;

            // Act
            var exception = Assert.Throws<NotPlayerTurnException>(() => this._gameRuleManager.ValidatePlayerCanPlayCard(gameState, playerId, currentTurnPlayerId, noOfActionsPlayed));

            // Assert
            Assert.Contains("player1", exception.Message);
            Assert.Contains("player2", exception.Message);
        }



        #endregion
    }
}
