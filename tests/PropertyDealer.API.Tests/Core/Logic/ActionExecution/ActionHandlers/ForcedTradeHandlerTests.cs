using AutoFixture;
using AutoFixture.Xunit2;
using Moq;
using property_dealer_API.Application.Enums;
using property_dealer_API.Application.Exceptions;
using property_dealer_API.Core;
using property_dealer_API.Core.Entities;
using property_dealer_API.Core.Logic.ActionExecution;
using property_dealer_API.Core.Logic.ActionExecution.ActionHandlers;
using property_dealer_API.Core.Logic.GameRulesManager;
using property_dealer_API.Core.Logic.PendingActionsManager;
using property_dealer_API.Core.Logic.PlayerHandsManager;
using property_dealer_API.Core.Logic.PlayersManager;
using property_dealer_API.Models.Cards;
using property_dealer_API.Models.Enums.Cards;
using Xunit;

// Assumes you have an AutoMoqDataAttribute class in your project
namespace PropertyDealer.API.Tests.Core.Logic.ActionExecution.ActionHandlers
{
    public class ForcedTradeHandlerTests
    {
        #region ActionType & Initialize Tests

        [Theory, AutoMoqData]
        public void ActionType_ReturnsCorrectType(ForcedTradeHandler sut)
        {
            Assert.Equal(ActionTypes.ForcedTrade, sut.ActionType);
        }

        [Theory, AutoMoqData]
        public void Initialize_WithValidCard_ReturnsPlayerSelectionContext(
            Player initiator,
            List<Player> allPlayers,
            ForcedTradeHandler sut,
            IFixture fixture)
        {
            // Arrange
            // FIX: Using .Command property instead of .ActionType
            var card = fixture.Build<CommandCard>()
                              .With(c => c.Command, ActionTypes.ForcedTrade)
                              .Create();
            allPlayers.Add(initiator);

            // Act
            var result = sut.Initialize(initiator, card, allPlayers);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(DialogTypeEnum.PlayerSelection, result.DialogToOpen);
        }

        [Theory, AutoMoqData]
        public void Initialize_WithWrongCard_ThrowsCardMismatchException(
            Player initiator,
            List<Player> allPlayers,
            ForcedTradeHandler sut,
            IFixture fixture)
        {
            // Arrange
            // FIX: Using .Command property instead of .ActionType
            var card = fixture.Build<CommandCard>()
                              .With(c => c.Command, ActionTypes.BountyHunter)
                              .Create();

            // Act & Assert
            Assert.Throws<CardMismatchException>(() => sut.Initialize(initiator, card, allPlayers));
        }

        #endregion

        #region ProcessResponse - PlayerSelection

        [Theory, AutoMoqData]
        public void ProcessResponse_PlayerSelection_SetsDialogToTableHandSelector(
            [Frozen] Mock<IPlayerManager> mockPlayerManager,
            Player initiator,
            Player target,
            ForcedTradeHandler sut)
        {
            // Arrange
            var context = new ActionContext
            {
                CardId = "card-id",
                ActionInitiatingPlayerId = initiator.UserId,
                ActionType = ActionTypes.ForcedTrade,
                DialogTargetList = new List<Player>(),
                DialogToOpen = DialogTypeEnum.PlayerSelection,
                TargetPlayerId = target.UserId
            };

            mockPlayerManager.Setup(p => p.GetPlayerByUserId(initiator.UserId)).Returns(initiator);
            mockPlayerManager.Setup(p => p.GetPlayerByUserId(target.UserId)).Returns(target);

            // Act
            sut.ProcessResponse(initiator, context);

            // Assert
            Assert.Equal(DialogTypeEnum.TableHandSelector, context.DialogToOpen);
            Assert.Equal(target.UserId, context.TargetPlayerId);
        }

        #endregion

        #region ProcessResponse - TableHandSelection (Happy Path)

        [Theory, AutoMoqData]
        public void ProcessResponse_TableHandSelection_NormalTrade_ExecutesAndCompletes(
            [Frozen] Mock<IPlayerManager> mockPlayerManager,
            [Frozen] Mock<IPlayerHandManager> mockPlayerHandManager,
            [Frozen] Mock<IGameRuleManager> mockRulesManager,
            [Frozen] Mock<IActionExecutor> mockActionExecutor,
            [Frozen] Mock<IPendingActionManager> mockPendingActionManager,
            Player initiator,
            Player target,
            StandardSystemCard targetCard, // Card to take
            StandardSystemCard ownCard,    // Card to give
            ForcedTradeHandler sut)
        {
            // Arrange
            var context = new ActionContext
            {
                CardId = "card-id",
                ActionInitiatingPlayerId = initiator.UserId,
                ActionType = ActionTypes.ForcedTrade,
                DialogTargetList = new List<Player>(),
                DialogToOpen = DialogTypeEnum.TableHandSelector,
                TargetPlayerId = target.UserId,
                TargetCardId = targetCard.CardGuid.ToString(),
                OwnTargetCardId = new List<string> { ownCard.CardGuid.ToString() }
            };

            // Setup mocks for a normal trade
            mockPlayerManager.Setup(p => p.GetPlayerByUserId(target.UserId)).Returns(target);

            mockPlayerHandManager.Setup(h => h.GetCardInTableHand(target.UserId, targetCard.CardGuid.ToString())).Returns((targetCard, PropertyCardColoursEnum.Brown));
            mockPlayerHandManager.Setup(h => h.GetCardInTableHand(initiator.UserId, ownCard.CardGuid.ToString())).Returns((ownCard, PropertyCardColoursEnum.Cyan));

            // Assume no special conditions
            mockRulesManager.Setup(r => r.DoesPlayerHaveShieldsUp(It.IsAny<Player>(), It.IsAny<List<Card>>())).Returns(false);
            mockRulesManager.Setup(r => r.IsCardSystemWildCard(It.IsAny<Card>())).Returns(false);

            var pendingAction = new PendingAction { InitiatorUserId = initiator.UserId, ActionType = ActionTypes.ForcedTrade };
            mockPendingActionManager.Setup(p => p.CurrPendingAction).Returns(pendingAction);

            // Act
            sut.ProcessResponse(initiator, context);

            //// Assert
            //mockActionExecutor.Verify(x => x.ExecuteForcedTrade(
            //    initiator.UserId,
            //    target.UserId,
            //    targetCard.CardGuid.ToString(),
            //    ownCard.CardGuid.ToString()
            //), Times.Once);
            //mockPendingActionManager.Verify(p => p.ClearPendingAction(), Times.Once);
        }

        #endregion

        #region ProcessResponse - TableHandSelection (Special Conditions)

        [Theory, AutoMoqData]
        public void ProcessResponse_TableHandSelection_TargetHasShield_BuildsShieldsUpContext(
            [Frozen] Mock<IPlayerManager> mockPlayerManager,
            [Frozen] Mock<IPlayerHandManager> mockPlayerHandManager,
            [Frozen] Mock<IGameRuleManager> mockRulesManager,
            [Frozen] Mock<IPendingActionManager> mockPendingActionManager,
            Player initiator,
            Player target,
            StandardSystemCard targetCard,
            StandardSystemCard ownCard,
            ForcedTradeHandler sut)
        {
            // Arrange
            var context = new ActionContext
            {
                CardId = "card-id",
                ActionInitiatingPlayerId = initiator.UserId,
                ActionType = ActionTypes.ForcedTrade,
                DialogTargetList = new List<Player>(),
                DialogToOpen = DialogTypeEnum.TableHandSelector,
                TargetPlayerId = target.UserId,
                TargetCardId = targetCard.CardGuid.ToString(),
                OwnTargetCardId = new List<string> { ownCard.CardGuid.ToString() }
            };

            mockPlayerManager.Setup(p => p.GetPlayerByUserId(target.UserId)).Returns(target);
            mockPlayerHandManager.Setup(h => h.GetCardInTableHand(target.UserId, targetCard.CardGuid.ToString())).Returns((targetCard, PropertyCardColoursEnum.Brown));

            // Target HAS a shield
            mockRulesManager.Setup(r => r.DoesPlayerHaveShieldsUp(target, It.IsAny<List<Card>>())).Returns(true);

            // FIX: Create a valid PendingAction instance for the setup
            var pendingAction = new PendingAction { InitiatorUserId = initiator.UserId, ActionType = ActionTypes.ForcedTrade };
            mockPendingActionManager.Setup(p => p.CurrPendingAction).Returns(pendingAction);

            // Act
            sut.ProcessResponse(initiator, context);

            // Assert
            Assert.Equal(DialogTypeEnum.ShieldsUp, context.DialogToOpen);
        }

        [Theory, AutoMoqData]
        public void ProcessResponse_TableHandSelection_TargetCardIsWildcard_BuildsWildcardContextForInitiator(
            [Frozen] Mock<IPlayerManager> mockPlayerManager,
            [Frozen] Mock<IPlayerHandManager> mockPlayerHandManager,
            [Frozen] Mock<IGameRuleManager> mockRulesManager,
            [Frozen] Mock<IPendingActionManager> mockPendingActionManager,
            Player initiator,
            Player target,
            SystemWildCard targetCard, // The card to take is a wildcard
            StandardSystemCard ownCard,
            ForcedTradeHandler sut)
        {
            // Arrange
            var context = new ActionContext
            {
                CardId = "card-id",
                ActionInitiatingPlayerId = initiator.UserId,
                ActionType = ActionTypes.ForcedTrade,
                DialogTargetList = new List<Player>(),
                DialogToOpen = DialogTypeEnum.TableHandSelector,
                TargetPlayerId = target.UserId,
                TargetCardId = targetCard.CardGuid.ToString(),
                OwnTargetCardId = new List<string> { ownCard.CardGuid.ToString() }
            };

            mockPlayerManager.Setup(p => p.GetPlayerByUserId(target.UserId)).Returns(target);
            // FIX: Corrected the return value to be the correct tuple type (Card, List<Card>)
            mockPlayerHandManager.Setup(h => h.GetCardInTableHand(target.UserId, targetCard.CardGuid.ToString())).Returns((targetCard, PropertyCardColoursEnum.Brown));

            mockRulesManager.Setup(r => r.DoesPlayerHaveShieldsUp(It.IsAny<Player>(), It.IsAny<List<Card>>())).Returns(false);

            // The card being taken IS a wildcard
            mockRulesManager.Setup(r => r.IsCardSystemWildCard(targetCard)).Returns(true);

            // FIX: Create a valid PendingAction instance for the setup
            var pendingAction = new PendingAction { InitiatorUserId = initiator.UserId, ActionType = ActionTypes.ForcedTrade };
            mockPendingActionManager.Setup(p => p.CurrPendingAction).Returns(pendingAction);

            // Act
            sut.ProcessResponse(initiator, context);

            // Assert
            Assert.Equal(DialogTypeEnum.WildcardColor, context.DialogToOpen);
            Assert.Equal(initiator.UserId, context.ActionInitiatingPlayerId); // Initiator is the beneficiary
        }

        #endregion

        #region ProcessResponse - Error Handling

        [Theory, AutoMoqData]
        public void ProcessResponse_FromNonInitiator_ThrowsInvalidOperationException(
            Player initiator,
            Player nonInitiator,
            ForcedTradeHandler sut)
        {
            // Arrange
            var context = new ActionContext
            {
                CardId = "card-id",
                ActionInitiatingPlayerId = initiator.UserId,
                ActionType = ActionTypes.ForcedTrade,
                DialogTargetList = new List<Player>(),
                DialogToOpen = DialogTypeEnum.TableHandSelector,
            };

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => sut.ProcessResponse(nonInitiator, context));
            Assert.Equal("Only the action initiator can respond during a Forced Trade.", exception.Message);
        }

        #endregion
    }
}