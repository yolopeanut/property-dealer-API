using AutoFixture;
using AutoFixture.Xunit2;
using Moq;
using property_dealer_API.Core;
using property_dealer_API.Application.Exceptions;
using property_dealer_API.Core.Entities;
using property_dealer_API.Core.Logic.ActionExecution;
using property_dealer_API.Core.Logic.ActionExecution.ActionHandlers;
using property_dealer_API.Core.Logic.PendingActionsManager;
using property_dealer_API.Models.Cards;
using property_dealer_API.Models.Enums.Cards;

namespace PropertyDealer.API.Tests.Core.Logic.ActionExecution.ActionHandlers
{
    public class ExploreNewSectorHandlerTests
    {
        #region ActionType Property Tests

        [Theory, AutoMoqData]
        public void ActionType_ReturnsCorrectType(ExploreNewSectorHandler sut)
        {
            // Assert
            Assert.Equal(ActionTypes.ExploreNewSector, sut.ActionType);
        }

        #endregion

        #region Initialize Method Tests

        [Theory, AutoMoqData]
        public void Initialize_WithValidCard_ExecutesDrawCardsAndCompletesAction(
            [Frozen] Mock<IActionExecutor> mockActionExecutor,
            [Frozen] Mock<IPendingActionManager> mockPendingActionManager,
            Player initiator,
            ExploreNewSectorHandler sut,
            IFixture fixture)
        {
            // Arrange
            const int expectedCardsToDraw = 2;
            var card = fixture.Build<CommandCard>()
                              .With(c => c.Command, ActionTypes.ExploreNewSector)
                              .Create();

            // Act
            var result = sut.Initialize(initiator, card, null); // allPlayers is not used

            // Assert
            mockActionExecutor.Verify(
                x => x.ExecuteDrawCards(initiator.UserId, expectedCardsToDraw),
                Times.Once);

            // Verify the action was completed by checking if the pending action was cleared
            mockPendingActionManager.Verify(p => p.ClearPendingAction(), Times.Once);

            // An immediate action should return null, indicating no further context is needed
            Assert.Null(result);
        }

        [Theory, AutoMoqData]
        public void Initialize_WithWrongCardType_ThrowsCardMismatchException(
            Player initiator,
            TributeCard wrongCardType, // Any card that isn't a CommandCard
            ExploreNewSectorHandler sut)
        {
            // Act & Assert
            Assert.Throws<CardMismatchException>(() => sut.Initialize(initiator, wrongCardType, null));
        }

        [Theory, AutoMoqData]
        public void Initialize_WithWrongCommandType_ThrowsInvalidOperationException(
            Player initiator,
            ExploreNewSectorHandler sut,
            IFixture fixture)
        {
            // Arrange
            var card = fixture.Build<CommandCard>()
                              .With(c => c.Command, ActionTypes.BountyHunter) // A command, but the wrong one
                              .Create();

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => sut.Initialize(initiator, card, null));
            Assert.Contains("Wrong command card found for Explore New Sector!", exception.Message);
        }

        #endregion

        #region ProcessResponse Method Tests

        [Theory, AutoMoqData]
        public void ProcessResponse_Always_ThrowsInvalidOperationException(
            Player anyPlayer,
            ActionContext anyContext,
            ExploreNewSectorHandler sut)
        {
            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => sut.ProcessResponse(anyPlayer, anyContext));
            Assert.Equal("ExploreNewSector is an immediate action and does not have response steps to process.", exception.Message);
        }

        #endregion
    }
}