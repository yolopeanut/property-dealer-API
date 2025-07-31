using property_dealer_API.Application.Enums;
using property_dealer_API.Application.Exceptions;
using property_dealer_API.Core.Entities;
using property_dealer_API.Core.Logic.PendingActionsManager;
using property_dealer_API.Models.Enums.Cards;
using PropertyDealer.API.Tests.TestHelpers;
using System.Collections.Concurrent;

namespace PropertyDealer.API.Tests.Core.Logic.PendingActionsManager
{
    public class PendingActionManagerTests
    {
        private readonly PendingActionManager _pendingActionManager;

        public PendingActionManagerTests()
        {
            _pendingActionManager = new PendingActionManager();
        }

        [Fact]
        public void CurrPendingAction_WhenNull_ThrowsPendingActionNotFoundException()
        {
            // Act & Assert
            Assert.Throws<PendingActionNotFoundException>(() => _pendingActionManager.CurrPendingAction);
        }

        [Fact]
        public void CurrPendingAction_WhenSet_ReturnsCorrectPendingAction()
        {
            // Arrange
            var pendingAction = new PendingAction
            {
                InitiatorUserId = "user1",
                ActionType = ActionTypes.HostileTakeover
            };

            // Act
            _pendingActionManager.CurrPendingAction = pendingAction;

            // Assert
            Assert.Equal(pendingAction, _pendingActionManager.CurrPendingAction);
            Assert.Equal("user1", _pendingActionManager.CurrPendingAction.InitiatorUserId);
            Assert.Equal(ActionTypes.HostileTakeover, _pendingActionManager.CurrPendingAction.ActionType);
        }

        [Fact]
        public void CurrPendingAction_WhenSettingNewWhileCurrentExists_ThrowsInvalidOperationException()
        {
            // Arrange
            var pendingAction1 = new PendingAction
            {
                InitiatorUserId = "user1",
                ActionType = ActionTypes.HostileTakeover
            };
            var pendingAction2 = new PendingAction
            {
                InitiatorUserId = "user2",
                ActionType = ActionTypes.ForcedTrade
            };

            _pendingActionManager.CurrPendingAction = pendingAction1;

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => _pendingActionManager.CurrPendingAction = pendingAction2);
            Assert.Contains("Cannot set new pending action when current one has not ended", exception.Message);
        }

        [Fact]
        public void AddResponseToQueue_WhenNotWaitingForResponses_ReturnsTrue()
        {
            // Arrange
            var pendingAction = new PendingAction
            {
                InitiatorUserId = "user1",
                ActionType = ActionTypes.PirateRaid,
                RequiredResponders = new ConcurrentBag<Player>() // Empty = not waiting
            };
            _pendingActionManager.CurrPendingAction = pendingAction;

            var player = PlayerTestHelpers.CreatePlayer("user2", "Player2");
            var actionContext = PlayerTestHelpers.CreateActionContext(
                actionType: ActionTypes.PirateRaid,
                dialogToOpen: DialogTypeEnum.ShieldsUp);

            // Act
            var result = _pendingActionManager.AddResponseToQueue(player, actionContext);

            // Assert
            Assert.True(result);
            Assert.Single(_pendingActionManager.CurrPendingAction.ResponseQueue);
        }

        [Fact]
        public void AddResponseToQueue_WhenWaitingForResponses_ReturnsFalse()
        {
            // Arrange
            var requiredResponders = new ConcurrentBag<Player>();
            requiredResponders.Add(PlayerTestHelpers.CreatePlayer("user2", "Player2"));
            requiredResponders.Add(PlayerTestHelpers.CreatePlayer("user3", "Player3"));

            var pendingAction = new PendingAction
            {
                InitiatorUserId = "user1",
                ActionType = ActionTypes.TradeDividend,
                RequiredResponders = requiredResponders
            };
            _pendingActionManager.CurrPendingAction = pendingAction;

            var player = PlayerTestHelpers.CreatePlayer("user2", "Player2");
            var actionContext = PlayerTestHelpers.CreateActionContext(
                actionType: ActionTypes.TradeDividend,
                dialogToOpen: DialogTypeEnum.PayValue);

            // Act
            var result = _pendingActionManager.AddResponseToQueue(player, actionContext);

            // Assert
            Assert.False(result); // Still waiting for user3
            Assert.Single(_pendingActionManager.CurrPendingAction.ResponseQueue);
        }

        [Fact]
        public void IncrementCurrentStep_IncrementsCurrentStep()
        {
            // Arrange
            var pendingAction = new PendingAction
            {
                InitiatorUserId = "user1",
                ActionType = ActionTypes.BountyHunter,
                CurrentStep = 1
            };
            _pendingActionManager.CurrPendingAction = pendingAction;

            // Act
            _pendingActionManager.IncrementCurrentStep();

            // Assert
            Assert.Equal(2, _pendingActionManager.CurrPendingAction.CurrentStep);
        }
    }
}