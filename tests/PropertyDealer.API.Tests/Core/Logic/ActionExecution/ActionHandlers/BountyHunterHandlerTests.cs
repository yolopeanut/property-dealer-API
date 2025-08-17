using AutoFixture;
using AutoFixture.Xunit2;
using Moq;
using Moq.Protected;
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

// Assumes you have an AutoMoqDataAttribute class in your project.
// public class AutoMoqDataAttribute : AutoDataAttribute
// {
//     public AutoMoqDataAttribute()
//         : base(() => new Fixture().Customize(new AutoMoqCustomization()))
//     {
//     }
// }

namespace PropertyDealer.API.Tests.Core.Logic.ActionExecution.ActionHandlers
{
    public class BountyHunterHandlerTests
    {
        #region ActionType Property Tests

        [Theory, AutoMoqData]
        public void ActionType_ReturnsCorrectType(BountyHunterHandler sut)
        {
            // Assert
            Assert.Equal(ActionTypes.BountyHunter, sut.ActionType);
        }

        #endregion

        #region Initialize Method Tests

        [Theory, AutoMoqData]
        public void Initialize_WithValidBountyHunterCard_ReturnsPlayerSelectionContext(
            Player initiator,
            List<Player> allPlayers,
            BountyHunterHandler sut,
            IFixture fixture)
        {
            // Arrange
            var card = fixture.Build<CommandCard>()
                              .With(c => c.Command, ActionTypes.BountyHunter)
                              .Create();
            allPlayers.Add(initiator);

            // Act
            var result = sut.Initialize(initiator, card, allPlayers);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(DialogTypeEnum.PlayerSelection, result.DialogToOpen);
            Assert.Equal(initiator.UserId, result.ActionInitiatingPlayerId);
            Assert.Equal(card.CardGuid.ToString(), result.CardId);
        }

        [Theory, AutoMoqData]
        public void Initialize_WithWrongCardType_ThrowsCardMismatchException(
            Player initiator,
            List<Player> allPlayers,
            TributeCard wrongCardType, // Any card that isn't a CommandCard
            BountyHunterHandler sut)
        {
            // Act & Assert
            Assert.Throws<CardMismatchException>(() => sut.Initialize(initiator, wrongCardType, allPlayers));
        }

        [Theory, AutoMoqData]
        public void Initialize_WithWrongCommandType_ThrowsCardMismatchException(
            Player initiator,
            List<Player> allPlayers,
            BountyHunterHandler sut,
            IFixture fixture)
        {
            // Arrange
            var card = fixture.Build<CommandCard>()
                              .With(c => c.Command, ActionTypes.ExploreNewSector) // A command, but the wrong one
                              .Create();

            // Act & Assert
            Assert.Throws<CardMismatchException>(() => sut.Initialize(initiator, card, allPlayers));
        }

        #endregion

        #region ProcessResponse - PlayerSelection Tests

        [Theory, AutoMoqData]
        public void ProcessResponse_PlayerSelection_TargetHasNoShield_SetsDialogToPayValue(
            [Frozen] Mock<IPlayerManager> mockPlayerManager,
            [Frozen] Mock<IPlayerHandManager> mockPlayerHandManager,
            [Frozen] Mock<IGameRuleManager> mockRulesManager,
            Player initiator,
            Player target,
            List<Card> targetHand,
            BountyHunterHandler sut)
        {
            // Arrange
            var context = new ActionContext
            {
                CardId = "any-card-id",
                ActionInitiatingPlayerId = initiator.UserId,
                ActionType = ActionTypes.BountyHunter,
                DialogTargetList = new List<Player>(),
                DialogToOpen = DialogTypeEnum.PlayerSelection,
                TargetPlayerId = target.UserId
            };

            mockPlayerManager.Setup(p => p.GetPlayerByUserId(initiator.UserId)).Returns(initiator);
            mockPlayerManager.Setup(p => p.GetPlayerByUserId(target.UserId)).Returns(target);
            mockPlayerHandManager.Setup(p => p.GetPlayerHand(target.UserId)).Returns(targetHand);
            mockRulesManager.Setup(r => r.DoesPlayerHaveShieldsUp(target, targetHand)).Returns(false);

            // Act
            sut.ProcessResponse(initiator, context);

            // Assert
            Assert.Equal(DialogTypeEnum.PayValue, context.DialogToOpen);
            Assert.Equal(target.UserId, context.TargetPlayerId);
        }

        [Theory, AutoMoqData]
        public void ProcessResponse_PlayerSelection_TargetHasShield_SetsDialogToShieldsUp(
            [Frozen] Mock<IPlayerManager> mockPlayerManager,
            [Frozen] Mock<IPlayerHandManager> mockPlayerHandManager,
            [Frozen] Mock<IGameRuleManager> mockRulesManager,
            Player initiator,
            Player target,
            List<Card> targetHand,
            BountyHunterHandler sut)
        {
            // Arrange
            var context = new ActionContext
            {
                CardId = "any-card-id",
                ActionInitiatingPlayerId = initiator.UserId,
                ActionType = ActionTypes.BountyHunter,
                DialogTargetList = new List<Player>(),
                DialogToOpen = DialogTypeEnum.PlayerSelection,
                TargetPlayerId = target.UserId
            };

            mockPlayerManager.Setup(p => p.GetPlayerByUserId(initiator.UserId)).Returns(initiator);
            mockPlayerManager.Setup(p => p.GetPlayerByUserId(target.UserId)).Returns(target);
            mockPlayerHandManager.Setup(p => p.GetPlayerHand(target.UserId)).Returns(targetHand);
            mockRulesManager.Setup(r => r.DoesPlayerHaveShieldsUp(target, targetHand)).Returns(true);

            // Act
            sut.ProcessResponse(initiator, context);

            // Assert
            Assert.Equal(DialogTypeEnum.ShieldsUp, context.DialogToOpen);
            Assert.Equal(target.UserId, context.TargetPlayerId);
        }

        [Theory, AutoMoqData]
        public void ProcessResponse_PlayerSelection_ByNonInitiator_ThrowsInvalidOperationException(
            Player initiator,
            Player nonInitiator,
            BountyHunterHandler sut)
        {
            // Arrange
            var context = new ActionContext
            {
                CardId = "any-card-id",
                ActionInitiatingPlayerId = initiator.UserId,
                ActionType = ActionTypes.BountyHunter,
                DialogTargetList = new List<Player>(),
                DialogToOpen = DialogTypeEnum.PlayerSelection
            };

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => sut.ProcessResponse(nonInitiator, context));
            Assert.Equal("Only the action initiator can select a player.", exception.Message);
        }

        #endregion

        #region ProcessResponse - PayValue Tests

        [Theory, AutoMoqData]
        public void ProcessResponse_PayValue_ExecutesPayment(
            [Frozen] Mock<IActionExecutor> mockActionExecutor,
            BountyHunterHandler sut,
            Player initiator,
            Player target,
            List<string> paymentCardIds)
        {
            // ARRANGE
            var context = new ActionContext
            {
                CardId = "any-card-id",
                ActionInitiatingPlayerId = initiator.UserId,
                TargetPlayerId = target.UserId,
                ActionType = ActionTypes.BountyHunter,
                DialogTargetList = new List<Player>(),
                DialogToOpen = DialogTypeEnum.PayValue,
                OwnTargetCardId = paymentCardIds
            };

            // ACT
            sut.ProcessResponse(target, context);

            // ASSERT
            mockActionExecutor.Verify(x => x.ExecutePayment(
                initiator.UserId,
                target.UserId,
                paymentCardIds), Times.Once);
        }

        [Theory, AutoMoqData]
        public void ProcessResponse_PayValue_ByWrongPlayer_ThrowsInvalidOperationException(
            Player initiator,
            Player target,
            Player wrongResponder,
            BountyHunterHandler sut)
        {
            // Arrange
            var context = new ActionContext
            {
                CardId = "any-card-id",
                ActionInitiatingPlayerId = initiator.UserId,
                TargetPlayerId = target.UserId,
                ActionType = ActionTypes.BountyHunter,
                DialogTargetList = new List<Player>(),
                DialogToOpen = DialogTypeEnum.PayValue
            };

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => sut.ProcessResponse(wrongResponder, context));
            Assert.Equal("Only the target player can respond with payment.", exception.Message);
        }

        [Theory, AutoMoqData]
        public void ProcessResponse_PayValue_WithNoPaymentCards_ThrowsActionContextParameterNullException(
            Player initiator,
            Player target,
            BountyHunterHandler sut)
        {
            // Arrange
            var context = new ActionContext
            {
                CardId = "any-card-id",
                ActionInitiatingPlayerId = initiator.UserId,
                TargetPlayerId = target.UserId,
                ActionType = ActionTypes.BountyHunter,
                DialogTargetList = new List<Player>(),
                DialogToOpen = DialogTypeEnum.PayValue,
                OwnTargetCardId = new List<string>() // Empty list
            };

            // Act & Assert
            Assert.Throws<ActionContextParameterNullException>(() => sut.ProcessResponse(target, context));
        }

        #endregion
    }
}