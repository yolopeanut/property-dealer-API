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
using property_dealer_API.Core.Logic.PlayerHandsManager;
using property_dealer_API.Core.Logic.PlayersManager;
using property_dealer_API.Models.Cards;
using property_dealer_API.Models.Enums.Cards;
using Xunit;

// Make sure you have the AutoMoqDataAttribute class defined in your test project.
// Example:
// public class AutoMoqDataAttribute : AutoDataAttribute
// {
//     public AutoMoqDataAttribute()
//         : base(() => new Fixture().Customize(new AutoMoqCustomization()))
//     {
//     }
// }

namespace PropertyDealer.API.Tests.Core.Logic.ActionExecution.ActionHandlers
{
    public class TributeCardHandlerTests
    {
        #region ActionType Property Tests

        [Theory, AutoMoqData]
        public void ActionType_ReturnsCorrectType(TributeCardHandler sut)
        {
            // Act & Assert
            Assert.Equal(ActionTypes.Tribute, sut.ActionType);
        }

        #endregion

        #region Initialize Method Tests

        [Theory, AutoMoqData]
        public void Initialize_WithValidTributeCard_ReturnsPropertySetSelectionContext(
            Player initiator,
            TributeCard tributeCard,
            List<Player> allPlayers,
            TributeCardHandler sut,
            IFixture fixture)
        {
            // Arrange
            // Ensure the list has at least one other player besides the initiator
            if (!allPlayers.Any())
            {
                allPlayers.Add(fixture.Create<Player>());
            }
            allPlayers.Add(initiator); // Ensure initiator is in the list

            // Act
            var result = sut.Initialize(initiator, tributeCard, allPlayers);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(DialogTypeEnum.PropertySetSelection, result.DialogToOpen);
            Assert.Equal(initiator.UserId, result.ActionInitiatingPlayerId);
            Assert.Equal(tributeCard.CardGuid.ToString(), result.CardId);
        }

        [Theory, AutoMoqData]
        public void Initialize_WithNonTributeCard_ThrowsCardMismatchException(
            Player initiator,
            List<Player> allPlayers,
            TributeCardHandler sut,
            IFixture fixture)
        {
            // Arrange
            var wrongCard = fixture.Build<CommandCard>()
                                   .With(c => c.Command, ActionTypes.ExploreNewSector) // Corrected from 'Command'
                                   .Create();

            // Act & Assert
            var exception = Assert.Throws<CardMismatchException>(() =>
                sut.Initialize(initiator, wrongCard, allPlayers));
            Assert.Contains(wrongCard.CardGuid.ToString(), exception.Message);
        }

        [Theory, AutoMoqData]
        public void Initialize_CreatesCorrectPendingAction(
            Player initiator,
            TributeCard tributeCard,
            List<Player> allPlayers,
            TributeCardHandler sut)
        {
            // Arrange
            allPlayers.Add(initiator);

            // Act
            var result = sut.Initialize(initiator, tributeCard, allPlayers);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(ActionTypes.Tribute, result.ActionType);
            Assert.Equal(initiator.UserId, result.ActionInitiatingPlayerId);
        }

        #endregion

        #region ProcessResponse - PropertySetSelection Tests

        [Theory, AutoMoqData]
        public void ProcessResponse_PropertySetSelection_ValidInitiator_ProcessesSelection(
            [Frozen] Mock<IPlayerHandManager> mockPlayerHandManager,
            [Frozen] Mock<IGameRuleManager> mockRulesManager,
            Player initiator,
            List<Card> properties,
            int rentAmount,
            TributeCardHandler sut)
        {
            // Arrange
            var context = new ActionContext
            {
                CardId = "any-card-id",
                ActionInitiatingPlayerId = initiator.UserId,
                ActionType = ActionTypes.Tribute,
                DialogTargetList = new List<Player>(),
                DialogToOpen = DialogTypeEnum.PropertySetSelection,
                TargetSetColor = PropertyCardColoursEnum.Red
            };

            mockPlayerHandManager.Setup(x => x.GetPropertyGroupInPlayerTableHand(initiator.UserId, PropertyCardColoursEnum.Red))
                .Returns(properties);
            mockRulesManager.Setup(x => x.CalculateRentAmount(PropertyCardColoursEnum.Red, properties))
                .Returns(rentAmount);

            // Act
            sut.ProcessResponse(initiator, context);

            // Assert
            Assert.Equal(DialogTypeEnum.PayValue, context.DialogToOpen);
            Assert.Equal(rentAmount, context.PaymentAmount);
            mockPlayerHandManager.VerifyAll();
            mockRulesManager.VerifyAll();
        }

        [Theory, AutoMoqData]
        public void ProcessResponse_PropertySetSelection_NonInitiator_ThrowsInvalidOperationException(
            Player initiator,
            Player nonInitiator,
            TributeCardHandler sut)
        {
            // Arrange
            var context = new ActionContext
            {
                CardId = "any-card-id",
                ActionInitiatingPlayerId = initiator.UserId,
                ActionType = ActionTypes.Tribute,
                DialogTargetList = new List<Player>(),
                DialogToOpen = DialogTypeEnum.PropertySetSelection,
                TargetSetColor = PropertyCardColoursEnum.Red
            };

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                sut.ProcessResponse(nonInitiator, context));
            Assert.Equal("Only the action initiator can select a property set.", exception.Message);
        }

        [Theory, AutoMoqData]
        public void ProcessResponse_PropertySetSelection_NullTargetSetColor_ThrowsException(
            Player initiator,
            TributeCardHandler sut)
        {
            // Arrange
            var context = new ActionContext
            {
                CardId = "any-card-id",
                ActionInitiatingPlayerId = initiator.UserId,
                ActionType = ActionTypes.Tribute,
                DialogTargetList = new List<Player>(),
                DialogToOpen = DialogTypeEnum.PropertySetSelection,
                TargetSetColor = null
            };

            // Act & Assert
            var exception = Assert.Throws<ActionContextParameterNullException>(() =>
                sut.ProcessResponse(initiator, context));
            Assert.Contains("Cannot have null target set color during tribute action!", exception.Message);
        }

        [Theory, AutoMoqData]
        public void ProcessResponse_PropertySetSelection_NoPropertiesOfSelectedColor_ThrowsException(
            [Frozen] Mock<IPlayerHandManager> mockPlayerHandManager,
            Player initiator,
            PropertyCardColoursEnum color,
            TributeCardHandler sut)
        {
            // Arrange
            var context = new ActionContext
            {
                CardId = "any-card-id",
                ActionInitiatingPlayerId = initiator.UserId,
                ActionType = ActionTypes.Tribute,
                DialogTargetList = new List<Player>(),
                DialogToOpen = DialogTypeEnum.PropertySetSelection,
                TargetSetColor = color
            };

            mockPlayerHandManager.Setup(x => x.GetPropertyGroupInPlayerTableHand(initiator.UserId, color))
                .Throws(new Exception("No properties found"));

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                sut.ProcessResponse(initiator, context));
            Assert.Contains($"Cannot charge rent for {color} properties", exception.Message);
        }

        #endregion

        #region ProcessResponse - PayValue Tests

        [Theory, AutoMoqData]
        public void ProcessResponse_PayValue_ValidPayment_ExecutesPayment(
            [Frozen] Mock<IActionExecutor> mockActionExecutor,
            Player initiator,
            Player target,
            List<string> paymentCardIds,
            TributeCardHandler sut)
        {
            // Arrange
            var context = new ActionContext
            {
                CardId = "any-card-id",
                ActionInitiatingPlayerId = initiator.UserId,
                ActionType = ActionTypes.Tribute,
                DialogTargetList = new List<Player>(),
                DialogToOpen = DialogTypeEnum.PayValue,
                OwnTargetCardId = paymentCardIds
            };

            // Act
            sut.ProcessResponse(target, context);

            // Assert
            mockActionExecutor.Verify(x => x.ExecutePayment(
                initiator.UserId,
                target.UserId,
                paymentCardIds), Times.Once);
        }

        [Theory, AutoMoqData]
        public void ProcessResponse_PayValue_InitiatorTryingToPay_ThrowsException(
            Player initiator,
            TributeCardHandler sut)
        {
            // Arrange
            var context = new ActionContext
            {
                CardId = "any-card-id",
                ActionInitiatingPlayerId = initiator.UserId,
                ActionType = ActionTypes.Tribute,
                DialogTargetList = new List<Player>(),
                DialogToOpen = DialogTypeEnum.PayValue
            };

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                sut.ProcessResponse(initiator, context));
            Assert.Equal("The action initiator cannot pay themselves rent.", exception.Message);
        }

        [Theory, AutoMoqData]
        public void ProcessResponse_PayValue_NullOwnTargetCardId_ThrowsException(
            Player initiator,
            Player target,
            TributeCardHandler sut)
        {
            // Arrange
            var context = new ActionContext
            {
                CardId = "any-card-id",
                ActionInitiatingPlayerId = initiator.UserId,
                ActionType = ActionTypes.Tribute,
                DialogTargetList = new List<Player>(),
                DialogToOpen = DialogTypeEnum.PayValue,
                OwnTargetCardId = null
            };

            // Act & Assert
            var exception = Assert.Throws<ActionContextParameterNullException>(() =>
                sut.ProcessResponse(target, context));
            Assert.Contains("A response (payment or shield) must be provided.", exception.Message);
        }

        #endregion

        #region ProcessResponse - ShieldsUp Tests

        [Theory, AutoMoqData]
        public void ProcessResponse_PayValue_ShieldsUpResponse_WithValidShieldsUp_HandlesShield(
            [Frozen] Mock<IPlayerManager> mockPlayerManager,
            [Frozen] Mock<IPlayerHandManager> mockPlayerHandManager,
            [Frozen] Mock<IGameRuleManager> mockRulesManager,
            [Frozen] Mock<IActionExecutor> mockActionExecutor,
            Player initiator,
            Player target,
            List<string> shieldCardId,
            List<Card> targetHand,
            TributeCardHandler sut)
        {
            // Arrange
            var context = new ActionContext
            {
                CardId = "any-card-id",
                ActionInitiatingPlayerId = initiator.UserId,
                ActionType = ActionTypes.Tribute,
                DialogTargetList = new List<Player>(),
                DialogToOpen = DialogTypeEnum.PayValue,
                DialogResponse = CommandResponseEnum.ShieldsUp,
                OwnTargetCardId = shieldCardId
            };

            mockPlayerManager.Setup(x => x.GetPlayerByUserId(target.UserId)).Returns(target);
            mockPlayerHandManager.Setup(x => x.GetPlayerHand(target.UserId)).Returns(targetHand);
            mockRulesManager.Setup(x => x.DoesPlayerHaveShieldsUp(target, targetHand)).Returns(true);

            // Act
            sut.ProcessResponse(target, context);

            // Assert
            mockRulesManager.Verify(x => x.DoesPlayerHaveShieldsUp(target, targetHand), Times.Once);
            mockActionExecutor.Verify(x => x.ExecutePayment(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>()), Times.Never);
        }

        [Theory, AutoMoqData]
        public void ProcessResponse_PayValue_ShieldsUpResponse_WithoutShieldsUp_ThrowsException(
            [Frozen] Mock<IPlayerManager> mockPlayerManager,
            [Frozen] Mock<IPlayerHandManager> mockPlayerHandManager,
            [Frozen] Mock<IGameRuleManager> mockRulesManager,
            Player initiator,
            Player target,
            List<string> shieldCardId,
            List<Card> targetHand,
            TributeCardHandler sut)
        {
            // Arrange
            var context = new ActionContext
            {
                CardId = "any-card-id",
                ActionInitiatingPlayerId = initiator.UserId,
                ActionType = ActionTypes.Tribute,
                DialogTargetList = new List<Player>(),
                DialogToOpen = DialogTypeEnum.PayValue,
                DialogResponse = CommandResponseEnum.ShieldsUp,
                OwnTargetCardId = shieldCardId
            };

            mockPlayerManager.Setup(x => x.GetPlayerByUserId(target.UserId)).Returns(target);
            mockPlayerHandManager.Setup(x => x.GetPlayerHand(target.UserId)).Returns(targetHand);
            mockRulesManager.Setup(x => x.DoesPlayerHaveShieldsUp(target, targetHand)).Returns(false);

            // Act & Assert
            var exception = Assert.Throws<CardNotFoundException>(() =>
                sut.ProcessResponse(target, context));
            Assert.Equal("Shields up was not found in players deck!", exception.Message);
        }

        #endregion

        #region ProcessResponse - Invalid Dialog Tests

        [Theory, AutoMoqData]
        public void ProcessResponse_InvalidDialog_ThrowsInvalidOperationException(
            Player player,
            TributeCardHandler sut)
        {
            // Arrange
            var context = new ActionContext
            {
                CardId = "any-card-id",
                ActionInitiatingPlayerId = player.UserId,
                ActionType = ActionTypes.Tribute,
                DialogTargetList = new List<Player>(),
                DialogToOpen = DialogTypeEnum.PlayerSelection,
            };

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                sut.ProcessResponse(player, context));
            Assert.Contains("Invalid state for Tribute action: PlayerSelection", exception.Message);
        }

        #endregion
    }
}