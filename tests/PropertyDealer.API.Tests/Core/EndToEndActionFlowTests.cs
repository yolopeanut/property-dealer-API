using property_dealer_API.Application.Enums;
using property_dealer_API.Core;
using property_dealer_API.Core.Entities;
using property_dealer_API.Core.Logic.ActionExecution;
using property_dealer_API.Core.Logic.ActionExecution.ActionsContextBuilder;
using property_dealer_API.Core.Logic.DecksManager;
using property_dealer_API.Core.Logic.DialogsManager;
using property_dealer_API.Core.Logic.GameRulesManager;
using property_dealer_API.Core.Logic.PendingActionsManager;
using property_dealer_API.Core.Logic.PlayerHandsManager;
using property_dealer_API.Core.Logic.PlayersManager;
using property_dealer_API.Models.Cards;
using property_dealer_API.Models.Enums.Cards;
using PropertyDealer.API.Tests.TestHelpers;
using System.Numerics;

namespace PropertyDealer.API.Tests.Core.Logic.Integration
{
    public class EndToEndActionFlowTests
    {
        private readonly IPlayerHandManager _playerHandManager;
        private readonly IPlayerManager _playerManager;
        private readonly IGameRuleManager _rulesManager;
        private readonly IPendingActionManager _pendingActionManager;
        private readonly IDeckManager _deckManager;
        private readonly IDialogManager _dialogManager;
        private readonly IActionExecutionManager _actionExecutionManager;

        public EndToEndActionFlowTests()
        {
            _playerHandManager = new PlayersHandManager();
            _playerManager = new PlayerManager();
            _rulesManager = new GameRuleManager();
            _pendingActionManager = new PendingActionManager();
            _deckManager = new DeckManager();

            var contextBuilder = new ActionContextBuilder(_pendingActionManager, _rulesManager, _deckManager, _playerHandManager);
            var actionExecutor = new ActionExecutor(_playerHandManager, _deckManager, _rulesManager);
            var dialogResponseProcessor = new DialogResponseProcessor(_playerHandManager, _playerManager, _rulesManager, _pendingActionManager, actionExecutor);
            _actionExecutionManager = new ActionExecutionManager(contextBuilder, dialogResponseProcessor);
            _dialogManager = new DialogManager(_actionExecutionManager, _pendingActionManager);
        }

        #region Immediate Actions

        [Fact]
        public void ExploreNewSector_EndToEnd_DrawsTwoCards()
        {
            // Arrange
            GameStateTestHelpers.PopulateTestDeck(_deckManager);

            var player = GameStateTestHelpers.SetupPlayerInGame(_playerManager, _playerHandManager, "user1", "Player1");
            var exploreCard = CardTestHelpers.CreateCommandCard(ActionTypes.ExploreNewSector);
            GameStateTestHelpers.GivePlayerCards(_playerHandManager, player.UserId, [exploreCard]);

            var initialHandSize = _playerHandManager.GetPlayerHand(player.UserId).Count;

            // Act
            var result = _actionExecutionManager.ExecuteAction(player.UserId, exploreCard, player, _playerManager.GetAllPlayers());

            // Assert
            Assert.Null(result); // Immediate action returns null
            var finalHandSize = _playerHandManager.GetPlayerHand(player.UserId).Count;
            Assert.Equal(initialHandSize + 2, finalHandSize); // Should have drawn 2 more cards
        }
        #endregion

        #region Single Step Actions

        [Fact]
        public void TradeDividend_EndToEnd_AllPlayersPayInitiator()
        {
            // Arrange
            GameStateTestHelpers.PopulateTestDeck(_deckManager);
            var initiator = GameStateTestHelpers.SetupPlayerInGame(_playerManager, _playerHandManager, "user1", "Initiator");
            var target1 = GameStateTestHelpers.SetupPlayerInGame(_playerManager, _playerHandManager, "user2", "Target1");
            var target2 = GameStateTestHelpers.SetupPlayerInGame(_playerManager, _playerHandManager, "user3", "Target2");

            var tradeDividendCard = CardTestHelpers.CreateCommandCard(ActionTypes.TradeDividend);
            GameStateTestHelpers.GivePlayerCards(_playerHandManager, initiator.UserId, [tradeDividendCard]);

            // Give targets money to pay
            GameStateTestHelpers.GivePlayerMoney(_playerHandManager, target1.UserId, 5);
            GameStateTestHelpers.GivePlayerMoney(_playerHandManager, target2.UserId, 5);

            // Act - Step 1: Execute action
            var actionContext = _actionExecutionManager.ExecuteAction(initiator.UserId, tradeDividendCard, initiator, _playerManager.GetAllPlayers());

            Assert.NotNull(actionContext);
            Assert.Equal(DialogTypeEnum.PayValue, actionContext.DialogToOpen);

            // Act - Step 2: Players pay (create responses just before registration)
            var payResponse1 = ResponseTestHelpers.CreatePayValueResponse(actionContext, _playerHandManager, target1.UserId, 2);
            _dialogManager.RegisterActionResponse(target1, payResponse1);

            var payResponse2 = ResponseTestHelpers.CreatePayValueResponse(actionContext, _playerHandManager, target2.UserId, 2);
            var result = _dialogManager.RegisterActionResponse(target2, payResponse2);

            // Assert
            Assert.True(result.ShouldClearPendingAction);

            // Verify money transferred
            var initiatorMoney = VerificationTestHelpers.GetPlayerMoneyTotal(_playerHandManager, initiator.UserId);
            Assert.Equal(4, initiatorMoney); // Received 4M total
        }

        [Fact]
        public void SystemWildCard_EndToEnd_PlacesCardWithSelectedColor()
        {
            // Arrange
            GameStateTestHelpers.PopulateTestDeck(_deckManager);
            var player = GameStateTestHelpers.SetupPlayerInGame(_playerManager, _playerHandManager, "user1", "Player1");
            var wildCard = CardTestHelpers.CreateSystemWildCard();
            GameStateTestHelpers.GivePlayerCards(_playerHandManager, player.UserId, [wildCard]);

            // Act - Step 1: Execute action
            var actionContext = _actionExecutionManager.ExecuteAction(player.UserId, wildCard, player, _playerManager.GetAllPlayers());

            Assert.NotNull(actionContext);
            Assert.Equal(DialogTypeEnum.WildcardColor, actionContext.DialogToOpen);

            // Act - Step 2: Select color
            var colorResponse = ResponseTestHelpers.CreateWildcardColorResponse(actionContext, PropertyCardColoursEnum.Red);
            var result = _dialogManager.RegisterActionResponse(player, colorResponse);

            // Assert
            Assert.True(result.ShouldClearPendingAction);

            // Verify card placed in correct property set
            var redProperties = _playerHandManager.GetPropertyGroupInPlayerTableHand(player.UserId, PropertyCardColoursEnum.Red);
            Assert.Contains(wildCard, redProperties);
        }

        #endregion

        #region Two Step Actions

        [Fact]
        public void SpaceStation_EndToEnd_PlacesOnPropertySet()
        {
            // Arrange
            var player = GameStateTestHelpers.SetupPlayerInGame(_playerManager, _playerHandManager, "user1", "Player1");
            var spaceStationCard = CardTestHelpers.CreateCommandCard(ActionTypes.SpaceStation);

            GameStateTestHelpers.PopulateTestDeck(_deckManager);
            GameStateTestHelpers.GivePlayerPropertySet(_playerHandManager, player.UserId, PropertyCardColoursEnum.Red, 3);
            GameStateTestHelpers.GivePlayerCards(_playerHandManager, player.UserId, [spaceStationCard]);

            // Act - Step 1: Execute action  
            var actionContext = _actionExecutionManager.ExecuteAction(player.UserId, spaceStationCard, player, _playerManager.GetAllPlayers());

            Assert.NotNull(actionContext);
            Assert.Equal(DialogTypeEnum.PropertySetSelection, actionContext.DialogToOpen);

            // Act - Step 2: Select property set
            var propertySetResponse = ResponseTestHelpers.CreatePropertySetResponse(actionContext, PropertyCardColoursEnum.Red);
            var result = _dialogManager.RegisterActionResponse(player, propertySetResponse);

            // Assert
            Assert.True(result.ShouldClearPendingAction);

            // Verify space station placed
            var redProperties = _playerHandManager.GetPropertyGroupInPlayerTableHand(player.UserId, PropertyCardColoursEnum.Red);
            Assert.Contains(spaceStationCard, redProperties);
        }

        [Fact]
        public void Tribute_EndToEnd_ChargesRentToAllPlayers()
        {
            // Arrange
            GameStateTestHelpers.PopulateTestDeck(_deckManager);
            var initiator = GameStateTestHelpers.SetupPlayerInGame(_playerManager, _playerHandManager, "user1", "Initiator");
            var target1 = GameStateTestHelpers.SetupPlayerInGame(_playerManager, _playerHandManager, "user2", "Target1");
            var target2 = GameStateTestHelpers.SetupPlayerInGame(_playerManager, _playerHandManager, "user3", "Target2");

            GameStateTestHelpers.GivePlayerPropertySet(_playerHandManager, initiator.UserId, PropertyCardColoursEnum.Cyan, 3); // Complete set
            var tributeCard = CardTestHelpers.CreateTributeCard();
            GameStateTestHelpers.GivePlayerCards(_playerHandManager, initiator.UserId, [tributeCard]);

            // Give targets money
            GameStateTestHelpers.GivePlayerMoney(_playerHandManager, target1.UserId, 10);
            GameStateTestHelpers.GivePlayerMoney(_playerHandManager, target2.UserId, 10);

            // Act - Step 1: Execute action
            var actionContext = _actionExecutionManager.ExecuteAction(initiator.UserId, tributeCard, initiator, _playerManager.GetAllPlayers());

            Assert.Equal(DialogTypeEnum.PropertySetSelection, actionContext.DialogToOpen);

            // Act - Step 2: Select property set
            var propertySetResponse = ResponseTestHelpers.CreatePropertySetResponse(actionContext, PropertyCardColoursEnum.Cyan);
            var step2Result = _dialogManager.RegisterActionResponse(initiator, propertySetResponse);

            Assert.False(step2Result.ShouldClearPendingAction);
            Assert.Single(step2Result.NewActionContexts);
            Assert.Equal(DialogTypeEnum.PayValue, step2Result.NewActionContexts[0].DialogToOpen);

            // Act - Step 3: Players pay rent
            var rentAmount = step2Result.NewActionContexts[0].PaymentAmount ?? 6;
            var payResponse1 = ResponseTestHelpers.CreatePayValueResponse(step2Result.NewActionContexts[0], _playerHandManager, target1.UserId, rentAmount);
            var payResponse2 = ResponseTestHelpers.CreatePayValueResponse(step2Result.NewActionContexts[0], _playerHandManager, target2.UserId, rentAmount);

            _dialogManager.RegisterActionResponse(target1, payResponse1);
            var finalResult = _dialogManager.RegisterActionResponse(target2, payResponse2);

            // Assert
            Assert.True(finalResult.ShouldClearPendingAction);

            // Verify rent collected
            var initiatorMoney = VerificationTestHelpers.GetPlayerMoneyTotal(_playerHandManager, initiator.UserId);
            Assert.Equal(rentAmount * 2, initiatorMoney); // Rent from both players
        }

        #endregion

        #region Three Step Actions

        [Fact]
        public void PirateRaid_EndToEnd_StealsProperty()
        {
            // Arrange
            var initiator = GameStateTestHelpers.SetupPlayerInGame(_playerManager, _playerHandManager, "user1", "Initiator");
            var target = GameStateTestHelpers.SetupPlayerInGame(_playerManager, _playerHandManager, "user2", "Target");

            var targetProperties = GameStateTestHelpers.GivePlayerPropertySet(_playerHandManager, target.UserId, PropertyCardColoursEnum.Green, 1);
            var targetProperty = targetProperties[0];
            var pirateRaidCard = CardTestHelpers.CreateCommandCard(ActionTypes.PirateRaid);

            // Act - Step 1: Execute action (Player Selection)
            var actionContext = _actionExecutionManager.ExecuteAction(initiator.UserId, pirateRaidCard, initiator, _playerManager.GetAllPlayers());

            Assert.Equal(DialogTypeEnum.PlayerSelection, actionContext.DialogToOpen);

            // Act - Step 2: Select target player
            var playerSelectionResponse = ResponseTestHelpers.CreatePlayerSelectionResponse(actionContext, target.UserId);
            var step2Result = _dialogManager.RegisterActionResponse(initiator, playerSelectionResponse);

            Assert.False(step2Result.ShouldClearPendingAction);
            Assert.Equal(DialogTypeEnum.TableHandSelector, step2Result.NewActionContexts[0].DialogToOpen);

            // Act - Step 3: Select target property
            var propertySelectionResponse = ResponseTestHelpers.CreateTableHandResponse(step2Result.NewActionContexts[0], targetProperty.CardGuid.ToString());
            var finalResult = _dialogManager.RegisterActionResponse(initiator, propertySelectionResponse);

            // Assert
            Assert.True(finalResult.ShouldClearPendingAction);

            // Verify property stolen
            var initiatorProperties = ResponseTestHelpers.GetPropertyGroupSafely(_playerHandManager, initiator.UserId, PropertyCardColoursEnum.Green);
            var targetPropertiesAfter = ResponseTestHelpers.GetPropertyGroupSafely(_playerHandManager, target.UserId, PropertyCardColoursEnum.Green);


            Assert.Contains(targetProperty, initiatorProperties);
            Assert.DoesNotContain(targetProperty, targetPropertiesAfter);
        }

        [Fact]
        public void HostileTakeover_EndToEnd_StealsCompletePropertySet()
        {
            // Arrange
            var initiator = GameStateTestHelpers.SetupPlayerInGame(_playerManager, _playerHandManager, "user1", "Initiator");
            var target = GameStateTestHelpers.SetupPlayerInGame(_playerManager, _playerHandManager, "user2", "Target");

            var targetProperties = GameStateTestHelpers.GivePlayerPropertySet(_playerHandManager, target.UserId, PropertyCardColoursEnum.Yellow, 3); // Complete set
            var hostileTakeoverCard = CardTestHelpers.CreateCommandCard(ActionTypes.HostileTakeover);

            // Act - Step 1: Player Selection
            var actionContext = _actionExecutionManager.ExecuteAction(initiator.UserId, hostileTakeoverCard, initiator, _playerManager.GetAllPlayers());

            var playerSelectionResponse = ResponseTestHelpers.CreatePlayerSelectionResponse(actionContext, target.UserId);
            var step2Result = _dialogManager.RegisterActionResponse(initiator, playerSelectionResponse);

            Assert.Equal(DialogTypeEnum.PropertySetSelection, step2Result.NewActionContexts[0].DialogToOpen);

            // Act - Step 2: Property Set Selection
            var propertySetResponse = ResponseTestHelpers.CreatePropertySetResponse(step2Result.NewActionContexts[0], PropertyCardColoursEnum.Yellow);
            var finalResult = _dialogManager.RegisterActionResponse(initiator, propertySetResponse);

            // Assert
            Assert.True(finalResult.ShouldClearPendingAction);

            // Verify entire property set stolen
            var initiatorProperties = ResponseTestHelpers.GetPropertyGroupSafely(_playerHandManager, initiator.UserId, PropertyCardColoursEnum.Yellow);
            var targetPropertiesAfter = ResponseTestHelpers.GetPropertyGroupSafely(_playerHandManager, target.UserId, PropertyCardColoursEnum.Yellow);

            Assert.Equal(3, initiatorProperties.Count);
            Assert.Empty(targetPropertiesAfter);
            foreach (var property in targetProperties)
            {
                Assert.Contains(property, initiatorProperties);
            }
        }

        [Fact]
        public void BountyHunter_EndToEnd_CollectsDebtFromPlayer()
        {
            // Arrange
            var initiator = GameStateTestHelpers.SetupPlayerInGame(_playerManager, _playerHandManager, "user1", "Initiator");
            var target = GameStateTestHelpers.SetupPlayerInGame(_playerManager, _playerHandManager, "user2", "Target");

            GameStateTestHelpers.GivePlayerMoney(_playerHandManager, target.UserId, 10);
            var bountyHunterCard = CardTestHelpers.CreateCommandCard(ActionTypes.BountyHunter);

            // Act - Step 1: Player Selection
            var actionContext = _actionExecutionManager.ExecuteAction(initiator.UserId, bountyHunterCard, initiator, _playerManager.GetAllPlayers());

            var playerSelectionResponse = ResponseTestHelpers.CreatePlayerSelectionResponse(actionContext, target.UserId);
            var step2Result = _dialogManager.RegisterActionResponse(initiator, playerSelectionResponse);

            Assert.Equal(DialogTypeEnum.PayValue, step2Result.NewActionContexts[0].DialogToOpen);

            // Act - Step 2: Target pays debt
            var debtAmount = step2Result.NewActionContexts[0].PaymentAmount ?? 5;
            var payResponse = ResponseTestHelpers.CreatePayValueResponse(step2Result.NewActionContexts[0], _playerHandManager, target.UserId, debtAmount);
            var finalResult = _dialogManager.RegisterActionResponse(target, payResponse);

            // Assert
            Assert.True(finalResult.ShouldClearPendingAction);

            // Verify debt collected
            var initiatorMoney = VerificationTestHelpers.GetPlayerMoneyTotal(_playerHandManager, initiator.UserId);
            Assert.Equal(debtAmount, initiatorMoney);
        }

        [Fact]
        public void ForcedTrade_EndToEnd_SwapsProperties()
        {
            // Arrange
            GameStateTestHelpers.PopulateTestDeck(_deckManager);

            var initiator = GameStateTestHelpers.SetupPlayerInGame(_playerManager, _playerHandManager, "user1", "Initiator");
            var target = GameStateTestHelpers.SetupPlayerInGame(_playerManager, _playerHandManager, "user2", "Target");

            var initiatorProperties = GameStateTestHelpers.GivePlayerPropertySet(_playerHandManager, initiator.UserId, PropertyCardColoursEnum.Red, 1);
            var targetProperties = GameStateTestHelpers.GivePlayerPropertySet(_playerHandManager, target.UserId, PropertyCardColoursEnum.Cyan, 1);

            var initiatorProperty = initiatorProperties[0];
            var targetProperty = targetProperties[0];
            var forcedTradeCard = CardTestHelpers.CreateCommandCard(ActionTypes.ForcedTrade);
            GameStateTestHelpers.GivePlayerCards(_playerHandManager, initiator.UserId, [forcedTradeCard]);

            // Act - Step 1: Player Selection
            var actionContext = _actionExecutionManager.ExecuteAction(initiator.UserId, forcedTradeCard, initiator, _playerManager.GetAllPlayers());

            var playerSelectionResponse = ResponseTestHelpers.CreatePlayerSelectionResponse(actionContext!, target.UserId);
            var step2Result = _dialogManager.RegisterActionResponse(initiator, playerSelectionResponse);

            Assert.Equal(DialogTypeEnum.TableHandSelector, step2Result.NewActionContexts![0].DialogToOpen);

            // Act - Step 2: Select target's property and own property
            var tradeResponse = ResponseTestHelpers.CreateTableHandResponse(step2Result.NewActionContexts[0], targetProperty.CardGuid.ToString());
            tradeResponse.OwnTargetCardId = [initiatorProperty.CardGuid.ToString()];

            var finalResult = _dialogManager.RegisterActionResponse(initiator, tradeResponse);

            // Assert
            Assert.True(finalResult.ShouldClearPendingAction);

            // Verify properties swapped
            var initiatorRedProperties = ResponseTestHelpers.GetPropertyGroupSafely(_playerHandManager, initiator.UserId, PropertyCardColoursEnum.Red);
            var initiatorCyanProperties = ResponseTestHelpers.GetPropertyGroupSafely(_playerHandManager, initiator.UserId, PropertyCardColoursEnum.Cyan);
            var targetRedProperties = ResponseTestHelpers.GetPropertyGroupSafely(_playerHandManager, target.UserId, PropertyCardColoursEnum.Red);
            var targetCyanProperties = ResponseTestHelpers.GetPropertyGroupSafely(_playerHandManager, target.UserId, PropertyCardColoursEnum.Cyan);


            // Initiator should now have target's cyan property
            Assert.Contains(targetProperty, initiatorCyanProperties);
            Assert.DoesNotContain(initiatorProperty, initiatorRedProperties);

            // Target should now have initiator's red property
            Assert.Contains(initiatorProperty, targetRedProperties);
            Assert.DoesNotContain(targetProperty, targetCyanProperties);
        }

        #endregion

        #region Four Step Actions

        [Fact]
        public void TradeEmbargo_EndToEnd_DoublesRentCharge()
        {
            // Arrange
            var initiator = GameStateTestHelpers.SetupPlayerInGame(_playerManager, _playerHandManager, "user1", "Initiator");
            var target = GameStateTestHelpers.SetupPlayerInGame(_playerManager, _playerHandManager, "user2", "Target");

            var rentCard = CardTestHelpers.CreateTributeCard(); // Rent card to double
            GameStateTestHelpers.GivePlayerPropertySet(_playerHandManager, initiator.UserId, PropertyCardColoursEnum.Red, 2);
            GameStateTestHelpers.GivePlayerMoney(_playerHandManager, target.UserId, 20);
            var tradeEmbargoCard = CardTestHelpers.CreateCommandCard(ActionTypes.TradeEmbargo);

            GameStateTestHelpers.GivePlayerCards(_playerHandManager, initiator.UserId, [tradeEmbargoCard, rentCard]);

            // Act - Step 1: Own Hand Selection (select rent card)
            var actionContext = _actionExecutionManager.ExecuteAction(initiator.UserId, tradeEmbargoCard, initiator, _playerManager.GetAllPlayers());

            Assert.Equal(DialogTypeEnum.OwnHandSelection, actionContext.DialogToOpen);

            var ownHandResponse = ResponseTestHelpers.CreateOwnHandResponse(actionContext, rentCard.CardGuid.ToString());
            var step2Result = _dialogManager.RegisterActionResponse(initiator, ownHandResponse);

            Assert.Equal(DialogTypeEnum.PropertySetSelection, step2Result.NewActionContexts[0].DialogToOpen);

            // Act - Step 2: Property Set Selection
            var propertySetResponse = ResponseTestHelpers.CreatePropertySetResponse(step2Result.NewActionContexts[0], PropertyCardColoursEnum.Red);
            var step3Result = _dialogManager.RegisterActionResponse(initiator, propertySetResponse);

            Assert.Equal(DialogTypeEnum.PlayerSelection, step3Result.NewActionContexts[0].DialogToOpen);

            // Act - Step 3: Player Selection
            var playerSelectionResponse = ResponseTestHelpers.CreatePlayerSelectionResponse(step3Result.NewActionContexts[0], target.UserId);
            var step4Result = _dialogManager.RegisterActionResponse(initiator, playerSelectionResponse);

            Assert.Equal(DialogTypeEnum.PayValue, step4Result.NewActionContexts[0].DialogToOpen);

            // Verify doubled rent amount is calculated
            var expectedBaseRent = _rulesManager.CalculateRentAmount(PropertyCardColoursEnum.Red, _playerHandManager.GetPropertyGroupInPlayerTableHand(initiator.UserId, PropertyCardColoursEnum.Red));
            Assert.Equal(expectedBaseRent * 2, step4Result.NewActionContexts[0].PaymentAmount);

            // Act - Step 4: Target pays doubled rent
            var doubledRent = step4Result.NewActionContexts[0].PaymentAmount ?? 0;
            var payResponse = ResponseTestHelpers.CreatePayValueResponse(step4Result.NewActionContexts[0], _playerHandManager, target.UserId, doubledRent);
            var finalResult = _dialogManager.RegisterActionResponse(target, payResponse);

            // Assert
            Assert.True(finalResult.ShouldClearPendingAction);

            // Verify doubled rent collected
            var initiatorMoney = VerificationTestHelpers.GetPlayerMoneyTotal(_playerHandManager, initiator.UserId);
            Assert.Equal(doubledRent, initiatorMoney);
        }

        #endregion

        #region ShieldsUp Response Tests

        [Fact]
        public void HostileTakeover_WithShieldsUpResponse_ActionBlocked()
        {
            // Arrange
            GameStateTestHelpers.PopulateTestDeck(_deckManager);
            var initiator = GameStateTestHelpers.SetupPlayerInGame(_playerManager, _playerHandManager, "user1", "Initiator");
            var target = GameStateTestHelpers.SetupPlayerInGame(_playerManager, _playerHandManager, "user2", "Target");

            GameStateTestHelpers.GivePlayerPropertySet(_playerHandManager, target.UserId, PropertyCardColoursEnum.OmniSector, 3);
            var shieldsUpCard = CardTestHelpers.CreateCommandCard(ActionTypes.ShieldsUp);
            GameStateTestHelpers.GivePlayerCards(_playerHandManager, target.UserId, [shieldsUpCard]);

            var hostileTakeoverCard = CardTestHelpers.CreateCommandCard(ActionTypes.HostileTakeover);

            // Debug: Check initial state
            var initialTargetHand = _playerHandManager.GetPlayerHand(target.UserId);
            Console.WriteLine($"Initial target hand count: {initialTargetHand.Count}");
            Console.WriteLine($"Initial target has ShieldsUp: {VerificationTestHelpers.PlayerHasCard(_playerHandManager, target.UserId, shieldsUpCard)}");

            // Act - Execute the full flow until ShieldsUp decision
            var actionContext = _actionExecutionManager.ExecuteAction(initiator.UserId, hostileTakeoverCard, initiator, _playerManager.GetAllPlayers());

            var playerSelectionResponse = ResponseTestHelpers.CreatePlayerSelectionResponse(actionContext, target.UserId);
            var step2Result = _dialogManager.RegisterActionResponse(initiator, playerSelectionResponse);

            var propertySetResponse = ResponseTestHelpers.CreatePropertySetResponse(step2Result.NewActionContexts[0], PropertyCardColoursEnum.OmniSector);
            var step3Result = _dialogManager.RegisterActionResponse(initiator, propertySetResponse);

            // Debug: Check state before ShieldsUp
            var beforeShieldsUpHand = _playerHandManager.GetPlayerHand(target.UserId);
            Console.WriteLine($"Before ShieldsUp hand count: {beforeShieldsUpHand.Count}");
            Console.WriteLine($"Before ShieldsUp has card: {VerificationTestHelpers.PlayerHasCard(_playerHandManager, target.UserId, shieldsUpCard)}");

            // Act - Target uses ShieldsUp
            var shieldsUpResponse = ResponseTestHelpers.CreateShieldsUpResponse(step3Result.NewActionContexts[0]);

            try
            {
                var finalResult = _dialogManager.RegisterActionResponse(target, shieldsUpResponse);
                Console.WriteLine($"RegisterActionResponse succeeded, ShouldClearPendingAction: {finalResult.ShouldClearPendingAction}");

                // Debug: Check state immediately after
                var afterShieldsUpHand = _playerHandManager.GetPlayerHand(target.UserId);
                Console.WriteLine($"After ShieldsUp hand count: {afterShieldsUpHand.Count}");
                Console.WriteLine($"After ShieldsUp has card: {VerificationTestHelpers.PlayerHasCard(_playerHandManager, target.UserId, shieldsUpCard)}");

                Assert.True(finalResult.ShouldClearPendingAction);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception during RegisterActionResponse: {ex.Message}");
                Console.WriteLine($"Exception type: {ex.GetType().Name}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }

            // Final verification
            var finalTargetHand = _playerHandManager.GetPlayerHand(target.UserId);
            Console.WriteLine($"Final target hand count: {finalTargetHand.Count}");
            Console.WriteLine($"Final target has ShieldsUp: {VerificationTestHelpers.PlayerHasCard(_playerHandManager, target.UserId, shieldsUpCard)}");

            // Verify action was blocked - properties remain with target
            var targetProperties = ResponseTestHelpers.GetPropertyGroupSafely(_playerHandManager, target.UserId, PropertyCardColoursEnum.OmniSector);
            var initiatorProperties = ResponseTestHelpers.GetPropertyGroupSafely(_playerHandManager, initiator.UserId, PropertyCardColoursEnum.OmniSector);

            Assert.Equal(3, targetProperties.Count); // Properties stay with target
            Assert.Empty(initiatorProperties); // Initiator gets nothing

            // Verify ShieldsUp card was discarded from hand
            Assert.False(VerificationTestHelpers.PlayerHasCard(_playerHandManager, target.UserId, shieldsUpCard));
        }

        [Fact]
        public void PirateRaid_WithShieldsUpResponse_ActionBlocked()
        {
            // Arrange
            GameStateTestHelpers.PopulateTestDeck(_deckManager);
            var initiator = GameStateTestHelpers.SetupPlayerInGame(_playerManager, _playerHandManager, "user1", "Initiator");
            var target = GameStateTestHelpers.SetupPlayerInGame(_playerManager, _playerHandManager, "user2", "Target");

            var targetProperties = GameStateTestHelpers.GivePlayerPropertySet(_playerHandManager, target.UserId, PropertyCardColoursEnum.Green, 1);
            var shieldsUpCard = CardTestHelpers.CreateCommandCard(ActionTypes.ShieldsUp);
            GameStateTestHelpers.GivePlayerCards(_playerHandManager, target.UserId, [shieldsUpCard]);

            var pirateRaidCard = CardTestHelpers.CreateCommandCard(ActionTypes.PirateRaid);

            // Act - Execute flow until ShieldsUp
            var actionContext = _actionExecutionManager.ExecuteAction(initiator.UserId, pirateRaidCard, initiator, _playerManager.GetAllPlayers());

            var playerSelectionResponse = ResponseTestHelpers.CreatePlayerSelectionResponse(actionContext, target.UserId);
            var step2Result = _dialogManager.RegisterActionResponse(initiator, playerSelectionResponse);

            var tableHandResponse = ResponseTestHelpers.CreateTableHandResponse(step2Result.NewActionContexts[0], targetProperties[0].CardGuid.ToString());
            var step3Result = _dialogManager.RegisterActionResponse(initiator, tableHandResponse);

            // Should trigger ShieldsUp dialog
            Assert.Equal(DialogTypeEnum.ShieldsUp, step3Result.NewActionContexts[0].DialogToOpen);

            // Act - Target uses ShieldsUp 
            var shieldsUpResponse = ResponseTestHelpers.CreateShieldsUpResponse(step3Result.NewActionContexts[0]);
            var finalResult = _dialogManager.RegisterActionResponse(target, shieldsUpResponse);

            // Assert
            Assert.True(finalResult.ShouldClearPendingAction);

            // Verify property raid was blocked
            var targetPropertiesAfter = ResponseTestHelpers.GetPropertyGroupSafely(_playerHandManager, target.UserId, PropertyCardColoursEnum.Green);
            var initiatorProperties = ResponseTestHelpers.GetPropertyGroupSafely(_playerHandManager, initiator.UserId, PropertyCardColoursEnum.Green);

            Assert.Single(targetPropertiesAfter); // Target keeps property
            Assert.Empty(initiatorProperties); // Initiator gets nothing
        }

        #endregion
    }
}