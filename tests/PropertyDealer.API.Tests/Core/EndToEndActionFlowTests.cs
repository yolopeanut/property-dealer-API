using Microsoft.Extensions.DependencyInjection;
using property_dealer_API.Application.Enums;
using property_dealer_API.Core.Logic.ActionExecution;
using property_dealer_API.Core.Logic.DecksManager;
using property_dealer_API.Core.Logic.DialogsManager;
using property_dealer_API.Core.Logic.GameRulesManager;
using property_dealer_API.Core.Logic.PendingActionsManager;
using property_dealer_API.Core.Logic.PlayerHandsManager;
using property_dealer_API.Core.Logic.PlayersManager;
using property_dealer_API.Models.Enums.Cards;
using PropertyDealer.API.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc.Testing;
using property_dealer_API.Core.Logic.ActionExecution.ActionHandlerResolvers;

namespace PropertyDealer.API.Tests.Core.Logic.Integration
{
    public class EndToEndActionFlowTests : IntegrationTestBase
    {
        private readonly IPlayerHandManager _playerHandManager;
        private readonly IPlayerManager _playerManager;
        private readonly IGameRuleManager _rulesManager;
        private readonly IPendingActionManager _pendingActionManager;
        private readonly IDeckManager _deckManager;
        private readonly IDialogManager _dialogManager;
        private readonly IActionExecutionManager _actionExecutionManager;
        private readonly IActionHandlerResolver _actionHandlerResolver;

        public EndToEndActionFlowTests(TestWebApplicationFactory factory)
            : base(factory)
        {
            this._playerHandManager = base.ServiceProvider.GetRequiredService<IPlayerHandManager>();
            this._playerManager = base.ServiceProvider.GetRequiredService<IPlayerManager>();
            this._rulesManager = base.ServiceProvider.GetRequiredService<IGameRuleManager>();
            this._pendingActionManager = base.ServiceProvider.GetRequiredService<IPendingActionManager>();
            this._deckManager = base.ServiceProvider.GetRequiredService<IDeckManager>();
            this._actionHandlerResolver = base.ServiceProvider.GetRequiredService<IActionHandlerResolver>();
            this._actionExecutionManager = base.ServiceProvider.GetRequiredService<IActionExecutionManager>();
            this._dialogManager = base.ServiceProvider.GetRequiredService<IDialogManager>();
        }

        [Fact]
        public async Task Should_Initialize_Game_Components_Successfully()
        {
            // Arrange & Act
            // Verify that all required services are properly injected and available

            // Assert
            Assert.NotNull(_playerHandManager);
            Assert.NotNull(_playerManager);
            Assert.NotNull(_rulesManager);
            Assert.NotNull(_pendingActionManager);
            Assert.NotNull(_deckManager);
            Assert.NotNull(_dialogManager);
            Assert.NotNull(_actionExecutionManager);
            Assert.NotNull(_actionHandlerResolver);
        }

        #region Immediate Actions

        [Fact]
        public void ExploreNewSector_EndToEnd_DrawsTwoCards()
        {
            // Arrange
            GameStateTestHelpers.PopulateTestDeck(this._deckManager);

            var player = GameStateTestHelpers.SetupPlayerInGame(this._playerManager, this._playerHandManager, "user1", "Player1");
            var exploreCard = CardTestHelpers.CreateCommandCard(ActionTypes.ExploreNewSector);
            GameStateTestHelpers.GivePlayerCards(this._playerHandManager, player.UserId, [exploreCard]);

            var initialHandSize = this._playerHandManager.GetPlayerHand(player.UserId).Count;

            // Act
            var result = this._actionExecutionManager.ExecuteAction(player.UserId, exploreCard, player, this._playerManager.GetAllPlayers());

            // Assert
            Assert.Null(result); // Immediate action returns null
            var finalHandSize = this._playerHandManager.GetPlayerHand(player.UserId).Count;
            Assert.Equal(initialHandSize + 2, finalHandSize); // Should have drawn 2 more cards
        }
        #endregion

        #region Single Step Actions

        [Fact]
        public void TradeDividend_EndToEnd_AllPlayersPayInitiator()
        {
            // Arrange
            GameStateTestHelpers.PopulateTestDeck(this._deckManager);
            var initiator = GameStateTestHelpers.SetupPlayerInGame(this._playerManager, this._playerHandManager, "user1", "Initiator");
            var target1 = GameStateTestHelpers.SetupPlayerInGame(this._playerManager, this._playerHandManager, "user2", "Target1");
            var target2 = GameStateTestHelpers.SetupPlayerInGame(this._playerManager, this._playerHandManager, "user3", "Target2");

            var tradeDividendCard = CardTestHelpers.CreateCommandCard(ActionTypes.TradeDividend);
            GameStateTestHelpers.GivePlayerCards(this._playerHandManager, initiator.UserId, [tradeDividendCard]);

            // Give targets money to pay
            GameStateTestHelpers.GivePlayerMoney(this._playerHandManager, target1.UserId, 5);
            GameStateTestHelpers.GivePlayerMoney(this._playerHandManager, target2.UserId, 5);

            // Act - Step 1: Execute action
            var actionContext = this._actionExecutionManager.ExecuteAction(initiator.UserId, tradeDividendCard, initiator, this._playerManager.GetAllPlayers());

            Assert.NotNull(actionContext);
            Assert.Equal(DialogTypeEnum.PayValue, actionContext.DialogToOpen);

            // Act - Step 2: Players pay (create responses just before registration)
            var payResponse1 = ResponseTestHelpers.CreatePayValueResponse(actionContext, this._playerHandManager, target1.UserId, 2);
            this._dialogManager.RegisterActionResponse(target1, payResponse1);

            var payResponse2 = ResponseTestHelpers.CreatePayValueResponse(actionContext, this._playerHandManager, target2.UserId, 2);
            var result = this._dialogManager.RegisterActionResponse(target2, payResponse2);

            // Assert
            Assert.True(result.ShouldClearPendingAction);

            // Verify money transferred
            var initiatorMoney = VerificationTestHelpers.GetPlayerMoneyTotal(this._playerHandManager, initiator.UserId);
            Assert.Equal(4, initiatorMoney); // Received 4M total
        }

        [Fact]
        public void SystemWildCard_EndToEnd_PlacesCardWithSelectedColor()
        {
            // Arrange
            GameStateTestHelpers.PopulateTestDeck(this._deckManager);
            var player = GameStateTestHelpers.SetupPlayerInGame(this._playerManager, this._playerHandManager, "user1", "Player1");
            var wildCard = CardTestHelpers.CreateSystemWildCard();
            GameStateTestHelpers.GivePlayerCards(this._playerHandManager, player.UserId, [wildCard]);

            // Act - Step 1: Execute action
            var actionContext = this._actionExecutionManager.ExecuteAction(player.UserId, wildCard, player, this._playerManager.GetAllPlayers());

            Assert.NotNull(actionContext);
            Assert.Equal(DialogTypeEnum.WildcardColor, actionContext.DialogToOpen);

            // Act - Step 2: Select color
            var colorResponse = ResponseTestHelpers.CreateWildcardColorResponse(actionContext, PropertyCardColoursEnum.Red);
            var result = this._dialogManager.RegisterActionResponse(player, colorResponse);

            // Assert
            Assert.True(result.ShouldClearPendingAction);

            // Verify card placed in correct property set
            var redProperties = this._playerHandManager.GetPropertyGroupInPlayerTableHand(player.UserId, PropertyCardColoursEnum.Red);
            Assert.Contains(wildCard, redProperties);
        }

        #endregion

        #region Two Step Actions

        [Fact]
        public void SpaceStation_EndToEnd_PlacesOnPropertySet()
        {
            // Arrange
            var player = GameStateTestHelpers.SetupPlayerInGame(this._playerManager, this._playerHandManager, "user1", "Player1");
            var spaceStationCard = CardTestHelpers.CreateCommandCard(ActionTypes.SpaceStation);

            GameStateTestHelpers.PopulateTestDeck(this._deckManager);
            GameStateTestHelpers.GivePlayerPropertySet(this._playerHandManager, player.UserId, PropertyCardColoursEnum.Red, 3);
            GameStateTestHelpers.GivePlayerCards(this._playerHandManager, player.UserId, [spaceStationCard]);

            // Act - Step 1: Execute action  
            var actionContext = this._actionExecutionManager.ExecuteAction(player.UserId, spaceStationCard, player, this._playerManager.GetAllPlayers());

            Assert.NotNull(actionContext);
            Assert.Equal(DialogTypeEnum.PropertySetSelection, actionContext.DialogToOpen);

            // Act - Step 2: Select property set
            var propertySetResponse = ResponseTestHelpers.CreatePropertySetResponse(actionContext, PropertyCardColoursEnum.Red);
            var result = this._dialogManager.RegisterActionResponse(player, propertySetResponse);

            // Assert
            Assert.True(result.ShouldClearPendingAction);

            // Verify space station placed
            var redProperties = this._playerHandManager.GetPropertyGroupInPlayerTableHand(player.UserId, PropertyCardColoursEnum.Red);
            Assert.Contains(spaceStationCard, redProperties);
        }

        [Fact]
        public void Tribute_EndToEnd_ChargesRentToAllPlayers()
        {
            // Arrange
            GameStateTestHelpers.PopulateTestDeck(this._deckManager);
            var initiator = GameStateTestHelpers.SetupPlayerInGame(this._playerManager, this._playerHandManager, "user1", "Initiator");
            var target1 = GameStateTestHelpers.SetupPlayerInGame(this._playerManager, this._playerHandManager, "user2", "Target1");
            var target2 = GameStateTestHelpers.SetupPlayerInGame(this._playerManager, this._playerHandManager, "user3", "Target2");

            GameStateTestHelpers.GivePlayerPropertySet(this._playerHandManager, initiator.UserId, PropertyCardColoursEnum.Cyan, 3); // Complete set
            var tributeCard = CardTestHelpers.CreateTributeCard();
            GameStateTestHelpers.GivePlayerCards(this._playerHandManager, initiator.UserId, [tributeCard]);

            // Give targets money
            GameStateTestHelpers.GivePlayerMoney(this._playerHandManager, target1.UserId, 10);
            GameStateTestHelpers.GivePlayerMoney(this._playerHandManager, target2.UserId, 10);

            // Act - Step 1: Execute action
            var actionContext = this._actionExecutionManager.ExecuteAction(initiator.UserId, tributeCard, initiator, this._playerManager.GetAllPlayers());

            Assert.Equal(DialogTypeEnum.PropertySetSelection, actionContext.DialogToOpen);

            // Act - Step 2: Select property set
            var propertySetResponse = ResponseTestHelpers.CreatePropertySetResponse(actionContext, PropertyCardColoursEnum.Cyan);
            var step2Result = this._dialogManager.RegisterActionResponse(initiator, propertySetResponse);

            Assert.False(step2Result.ShouldClearPendingAction);
            Assert.Single(step2Result.NewActionContexts);
            Assert.Equal(DialogTypeEnum.PayValue, step2Result.NewActionContexts[0].DialogToOpen);

            // Act - Step 3: Players pay rent
            var rentAmount = step2Result.NewActionContexts[0].PaymentAmount ?? 6;
            var payResponse1 = ResponseTestHelpers.CreatePayValueResponse(step2Result.NewActionContexts[0], this._playerHandManager, target1.UserId, rentAmount);
            var payResponse2 = ResponseTestHelpers.CreatePayValueResponse(step2Result.NewActionContexts[0], this._playerHandManager, target2.UserId, rentAmount);

            this._dialogManager.RegisterActionResponse(target1, payResponse1);
            var finalResult = this._dialogManager.RegisterActionResponse(target2, payResponse2);

            // Assert
            Assert.True(finalResult.ShouldClearPendingAction);

            // Verify rent collected
            var initiatorMoney = VerificationTestHelpers.GetPlayerMoneyTotal(this._playerHandManager, initiator.UserId);
            Assert.Equal(rentAmount * 2, initiatorMoney); // Rent from both players
        }

        #endregion

        #region Three Step Actions

        [Fact]
        public void PirateRaid_EndToEnd_StealsProperty()
        {
            // Arrange
            var initiator = GameStateTestHelpers.SetupPlayerInGame(this._playerManager, this._playerHandManager, "user1", "Initiator");
            var target = GameStateTestHelpers.SetupPlayerInGame(this._playerManager, this._playerHandManager, "user2", "Target");

            var targetProperties = GameStateTestHelpers.GivePlayerPropertySet(this._playerHandManager, target.UserId, PropertyCardColoursEnum.Green, 1);
            var targetProperty = targetProperties[0];
            var pirateRaidCard = CardTestHelpers.CreateCommandCard(ActionTypes.PirateRaid);

            // Act - Step 1: Execute action (Player Selection)
            var actionContext = this._actionExecutionManager.ExecuteAction(initiator.UserId, pirateRaidCard, initiator, this._playerManager.GetAllPlayers());

            Assert.Equal(DialogTypeEnum.PlayerSelection, actionContext.DialogToOpen);

            // Act - Step 2: Select target player
            var playerSelectionResponse = ResponseTestHelpers.CreatePlayerSelectionResponse(actionContext, target.UserId);
            var step2Result = this._dialogManager.RegisterActionResponse(initiator, playerSelectionResponse);

            Assert.False(step2Result.ShouldClearPendingAction);
            Assert.Equal(DialogTypeEnum.TableHandSelector, step2Result.NewActionContexts[0].DialogToOpen);

            // Act - Step 3: Select target property
            var propertySelectionResponse = ResponseTestHelpers.CreateTableHandResponse(step2Result.NewActionContexts[0], targetProperty.CardGuid.ToString());
            var finalResult = this._dialogManager.RegisterActionResponse(initiator, propertySelectionResponse);

            // Assert
            Assert.True(finalResult.ShouldClearPendingAction);

            // Verify property stolen
            var initiatorProperties = ResponseTestHelpers.GetPropertyGroupSafely(this._playerHandManager, initiator.UserId, PropertyCardColoursEnum.Green);
            var targetPropertiesAfter = ResponseTestHelpers.GetPropertyGroupSafely(this._playerHandManager, target.UserId, PropertyCardColoursEnum.Green);


            Assert.Contains(targetProperty, initiatorProperties);
            Assert.DoesNotContain(targetProperty, targetPropertiesAfter);
        }



        [Fact]
        public void HostileTakeover_EndToEnd_StealsCompletePropertySet()
        {
            // Arrange
            var initiator = GameStateTestHelpers.SetupPlayerInGame(this._playerManager, this._playerHandManager, "user1", "Initiator");
            var target = GameStateTestHelpers.SetupPlayerInGame(this._playerManager, this._playerHandManager, "user2", "Target");

            var targetProperties = GameStateTestHelpers.GivePlayerPropertySet(this._playerHandManager, target.UserId, PropertyCardColoursEnum.Yellow, 3); // Complete set
            var hostileTakeoverCard = CardTestHelpers.CreateCommandCard(ActionTypes.HostileTakeover);

            // Act - Step 1: Player Selection
            var actionContext = this._actionExecutionManager.ExecuteAction(initiator.UserId, hostileTakeoverCard, initiator, this._playerManager.GetAllPlayers());

            var playerSelectionResponse = ResponseTestHelpers.CreatePlayerSelectionResponse(actionContext, target.UserId);
            var step2Result = this._dialogManager.RegisterActionResponse(initiator, playerSelectionResponse);

            Assert.Equal(DialogTypeEnum.PropertySetSelection, step2Result.NewActionContexts[0].DialogToOpen);

            // Act - Step 2: Property Set Selection
            var propertySetResponse = ResponseTestHelpers.CreatePropertySetResponse(step2Result.NewActionContexts[0], PropertyCardColoursEnum.Yellow);
            var finalResult = this._dialogManager.RegisterActionResponse(initiator, propertySetResponse);

            // Assert
            Assert.True(finalResult.ShouldClearPendingAction);

            // Verify entire property set stolen
            var initiatorProperties = ResponseTestHelpers.GetPropertyGroupSafely(this._playerHandManager, initiator.UserId, PropertyCardColoursEnum.Yellow);
            var targetPropertiesAfter = ResponseTestHelpers.GetPropertyGroupSafely(this._playerHandManager, target.UserId, PropertyCardColoursEnum.Yellow);

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
            var initiator = GameStateTestHelpers.SetupPlayerInGame(this._playerManager, this._playerHandManager, "user1", "Initiator");
            var target = GameStateTestHelpers.SetupPlayerInGame(this._playerManager, this._playerHandManager, "user2", "Target");

            GameStateTestHelpers.GivePlayerMoney(this._playerHandManager, target.UserId, 10);
            var bountyHunterCard = CardTestHelpers.CreateCommandCard(ActionTypes.BountyHunter);

            // Act - Step 1: Player Selection
            var actionContext = this._actionExecutionManager.ExecuteAction(initiator.UserId, bountyHunterCard, initiator, this._playerManager.GetAllPlayers());

            var playerSelectionResponse = ResponseTestHelpers.CreatePlayerSelectionResponse(actionContext, target.UserId);
            var step2Result = this._dialogManager.RegisterActionResponse(initiator, playerSelectionResponse);

            Assert.Equal(DialogTypeEnum.PayValue, step2Result.NewActionContexts[0].DialogToOpen);

            // Act - Step 2: Target pays debt
            var debtAmount = step2Result.NewActionContexts[0].PaymentAmount ?? 5;
            var payResponse = ResponseTestHelpers.CreatePayValueResponse(step2Result.NewActionContexts[0], this._playerHandManager, target.UserId, debtAmount);
            var finalResult = this._dialogManager.RegisterActionResponse(target, payResponse);

            // Assert
            Assert.True(finalResult.ShouldClearPendingAction);

            // Verify debt collected
            var initiatorMoney = VerificationTestHelpers.GetPlayerMoneyTotal(this._playerHandManager, initiator.UserId);
            Assert.Equal(debtAmount, initiatorMoney);
        }

        [Fact]
        public void ForcedTrade_EndToEnd_SwapsProperties()
        {
            // Arrange
            GameStateTestHelpers.PopulateTestDeck(this._deckManager);

            var initiator = GameStateTestHelpers.SetupPlayerInGame(this._playerManager, this._playerHandManager, "user1", "Initiator");
            var target = GameStateTestHelpers.SetupPlayerInGame(this._playerManager, this._playerHandManager, "user2", "Target");

            var initiatorProperties = GameStateTestHelpers.GivePlayerPropertySet(this._playerHandManager, initiator.UserId, PropertyCardColoursEnum.Red, 1);
            var targetProperties = GameStateTestHelpers.GivePlayerPropertySet(this._playerHandManager, target.UserId, PropertyCardColoursEnum.Cyan, 1);

            var initiatorProperty = initiatorProperties[0];
            var targetProperty = targetProperties[0];
            var forcedTradeCard = CardTestHelpers.CreateCommandCard(ActionTypes.ForcedTrade);
            GameStateTestHelpers.GivePlayerCards(this._playerHandManager, initiator.UserId, [forcedTradeCard]);

            // Act - Step 1: Player Selection
            var actionContext = this._actionExecutionManager.ExecuteAction(initiator.UserId, forcedTradeCard, initiator, this._playerManager.GetAllPlayers());

            var playerSelectionResponse = ResponseTestHelpers.CreatePlayerSelectionResponse(actionContext!, target.UserId);
            var step2Result = this._dialogManager.RegisterActionResponse(initiator, playerSelectionResponse);

            Assert.Equal(DialogTypeEnum.TableHandSelector, step2Result.NewActionContexts![0].DialogToOpen);

            // Act - Step 2: Select target's property and own property
            var tradeResponse = ResponseTestHelpers.CreateTableHandResponse(step2Result.NewActionContexts[0], targetProperty.CardGuid.ToString());
            tradeResponse.OwnTargetCardId = [initiatorProperty.CardGuid.ToString()];

            var finalResult = this._dialogManager.RegisterActionResponse(initiator, tradeResponse);

            // Assert
            Assert.True(finalResult.ShouldClearPendingAction);

            // Verify properties swapped
            var initiatorRedProperties = ResponseTestHelpers.GetPropertyGroupSafely(this._playerHandManager, initiator.UserId, PropertyCardColoursEnum.Red);
            var initiatorCyanProperties = ResponseTestHelpers.GetPropertyGroupSafely(this._playerHandManager, initiator.UserId, PropertyCardColoursEnum.Cyan);
            var targetRedProperties = ResponseTestHelpers.GetPropertyGroupSafely(this._playerHandManager, target.UserId, PropertyCardColoursEnum.Red);
            var targetCyanProperties = ResponseTestHelpers.GetPropertyGroupSafely(this._playerHandManager, target.UserId, PropertyCardColoursEnum.Cyan);


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
            var initiator = GameStateTestHelpers.SetupPlayerInGame(this._playerManager, this._playerHandManager, "user1", "Initiator");
            var target = GameStateTestHelpers.SetupPlayerInGame(this._playerManager, this._playerHandManager, "user2", "Target");

            var rentCard = CardTestHelpers.CreateTributeCard(); // Rent card to double
            GameStateTestHelpers.GivePlayerPropertySet(this._playerHandManager, initiator.UserId, PropertyCardColoursEnum.Red, 2);
            GameStateTestHelpers.GivePlayerMoney(this._playerHandManager, target.UserId, 20);
            var tradeEmbargoCard = CardTestHelpers.CreateCommandCard(ActionTypes.TradeEmbargo);

            GameStateTestHelpers.GivePlayerCards(this._playerHandManager, initiator.UserId, [tradeEmbargoCard, rentCard]);

            // Act - Step 1: Own Hand Selection (select rent card)
            var actionContext = this._actionExecutionManager.ExecuteAction(initiator.UserId, tradeEmbargoCard, initiator, this._playerManager.GetAllPlayers());

            Assert.Equal(DialogTypeEnum.OwnHandSelection, actionContext.DialogToOpen);

            var ownHandResponse = ResponseTestHelpers.CreateOwnHandResponse(actionContext, rentCard.CardGuid.ToString());
            var step2Result = this._dialogManager.RegisterActionResponse(initiator, ownHandResponse);

            Assert.Equal(DialogTypeEnum.PropertySetSelection, step2Result.NewActionContexts[0].DialogToOpen);

            // Act - Step 2: Property Set Selection
            var propertySetResponse = ResponseTestHelpers.CreatePropertySetResponse(step2Result.NewActionContexts[0], PropertyCardColoursEnum.Red);
            var step3Result = this._dialogManager.RegisterActionResponse(initiator, propertySetResponse);

            Assert.Equal(DialogTypeEnum.PlayerSelection, step3Result.NewActionContexts[0].DialogToOpen);

            // Act - Step 3: Player Selection
            var playerSelectionResponse = ResponseTestHelpers.CreatePlayerSelectionResponse(step3Result.NewActionContexts[0], target.UserId);
            var step4Result = this._dialogManager.RegisterActionResponse(initiator, playerSelectionResponse);

            Assert.Equal(DialogTypeEnum.PayValue, step4Result.NewActionContexts[0].DialogToOpen);

            // Verify doubled rent amount is calculated
            var expectedBaseRent = this._rulesManager.CalculateRentAmount(PropertyCardColoursEnum.Red, this._playerHandManager.GetPropertyGroupInPlayerTableHand(initiator.UserId, PropertyCardColoursEnum.Red));
            Assert.Equal(expectedBaseRent * 2, step4Result.NewActionContexts[0].PaymentAmount);

            // Act - Step 4: Target pays doubled rent
            var doubledRent = step4Result.NewActionContexts[0].PaymentAmount ?? 0;
            var payResponse = ResponseTestHelpers.CreatePayValueResponse(step4Result.NewActionContexts[0], this._playerHandManager, target.UserId, doubledRent);
            var finalResult = this._dialogManager.RegisterActionResponse(target, payResponse);

            // Assert
            Assert.True(finalResult.ShouldClearPendingAction);

            // Verify doubled rent collected
            var initiatorMoney = VerificationTestHelpers.GetPlayerMoneyTotal(this._playerHandManager, initiator.UserId);
            Assert.Equal(doubledRent, initiatorMoney);
        }

        [Fact]
        public void PirateRaid_EndToEnd_StealsWildCardProperty()
        {
            // Arrange
            var initiator = GameStateTestHelpers.SetupPlayerInGame(this._playerManager, this._playerHandManager, "user1", "Initiator");
            var target = GameStateTestHelpers.SetupPlayerInGame(this._playerManager, this._playerHandManager, "user2", "Target");

            var systemWildCard = CardTestHelpers.CreateSystemWildCard();
            var targetProperties = GameStateTestHelpers.GivePlayerPropertySet(this._playerHandManager, target.UserId, PropertyCardColoursEnum.Brown, systemWildCard);
            var targetProperty = targetProperties[0];
            var pirateRaidCard = CardTestHelpers.CreateCommandCard(ActionTypes.PirateRaid);

            // Act - Step 1: Execute action (Player Selection)
            var actionContext = this._actionExecutionManager.ExecuteAction(initiator.UserId, pirateRaidCard, initiator, this._playerManager.GetAllPlayers());

            Assert.Equal(DialogTypeEnum.PlayerSelection, actionContext.DialogToOpen);

            // Act - Step 2: Select target player
            var playerSelectionResponse = ResponseTestHelpers.CreatePlayerSelectionResponse(actionContext, target.UserId);
            var step2Result = this._dialogManager.RegisterActionResponse(initiator, playerSelectionResponse);

            Assert.False(step2Result.ShouldClearPendingAction);
            Assert.Equal(DialogTypeEnum.TableHandSelector, step2Result.NewActionContexts[0].DialogToOpen);

            // Act - Step 3: Select target property
            var propertySelectionResponse = ResponseTestHelpers.CreateTableHandResponse(step2Result.NewActionContexts[0], targetProperty.CardGuid.ToString());
            var step3Result = this._dialogManager.RegisterActionResponse(initiator, propertySelectionResponse);

            // Act - Step 4: WildCard dialog selection
            var wildcardResponse = ResponseTestHelpers.CreateWildcardColorResponse(step3Result.NewActionContexts[0], PropertyCardColoursEnum.Cyan);

            var finalResult = this._dialogManager.RegisterActionResponse(initiator, wildcardResponse);

            // Assert
            Assert.True(finalResult.ShouldClearPendingAction);

            // Verify property stolen
            var initiatorProperties = ResponseTestHelpers.GetPropertyGroupSafely(this._playerHandManager, initiator.UserId, PropertyCardColoursEnum.Cyan);
            var targetPropertiesAfter = ResponseTestHelpers.GetPropertyGroupSafely(this._playerHandManager, target.UserId, PropertyCardColoursEnum.Green);

            Assert.Contains(targetProperty, initiatorProperties);
            Assert.DoesNotContain(targetProperty, targetPropertiesAfter);
        }

        [Fact]
        public void ForcedTrade_EndToEnd_SwapsWildCardProperties()
        {
            // Arrange
            GameStateTestHelpers.PopulateTestDeck(this._deckManager);

            var initiator = GameStateTestHelpers.SetupPlayerInGame(this._playerManager, this._playerHandManager, "user1", "Initiator");
            var target = GameStateTestHelpers.SetupPlayerInGame(this._playerManager, this._playerHandManager, "user2", "Target");

            var initiatorProperties = GameStateTestHelpers.GivePlayerPropertySet(this._playerHandManager, initiator.UserId, PropertyCardColoursEnum.Red, 1);

            var systemWildCard = CardTestHelpers.CreateSystemWildCard();
            var targetProperties = GameStateTestHelpers.GivePlayerPropertySet(this._playerHandManager, target.UserId, PropertyCardColoursEnum.Brown, systemWildCard);

            var initiatorProperty = initiatorProperties[0];
            var targetProperty = targetProperties[0];
            var forcedTradeCard = CardTestHelpers.CreateCommandCard(ActionTypes.ForcedTrade);
            GameStateTestHelpers.GivePlayerCards(this._playerHandManager, initiator.UserId, [forcedTradeCard]);

            // Act - Step 1: Player Selection
            var actionContext = this._actionExecutionManager.ExecuteAction(initiator.UserId, forcedTradeCard, initiator, this._playerManager.GetAllPlayers());

            var playerSelectionResponse = ResponseTestHelpers.CreatePlayerSelectionResponse(actionContext!, target.UserId);
            var step2Result = this._dialogManager.RegisterActionResponse(initiator, playerSelectionResponse);

            Assert.Equal(DialogTypeEnum.TableHandSelector, step2Result.NewActionContexts![0].DialogToOpen);

            // Act - Step 2: Select target's property and own property
            var tradeResponse = ResponseTestHelpers.CreateTableHandResponse(step2Result.NewActionContexts[0], targetProperty.CardGuid.ToString());
            tradeResponse.OwnTargetCardId = [initiatorProperty.CardGuid.ToString()];

            var step3Result = this._dialogManager.RegisterActionResponse(initiator, tradeResponse);
            Assert.Equal(DialogTypeEnum.WildcardColor, step3Result.NewActionContexts![0].DialogToOpen);

            // Act - Step 3: WildCard dialog selection
            var wildcardResponse = ResponseTestHelpers.CreateWildcardColorResponse(step3Result.NewActionContexts[0], PropertyCardColoursEnum.Cyan);

            var finalResult = this._dialogManager.RegisterActionResponse(initiator, wildcardResponse);

            // Assert
            Assert.True(finalResult.ShouldClearPendingAction);

            // Verify properties swapped
            var initiatorRedProperties = ResponseTestHelpers.GetPropertyGroupSafely(this._playerHandManager, initiator.UserId, PropertyCardColoursEnum.Red);
            var initiatorCyanProperties = ResponseTestHelpers.GetPropertyGroupSafely(this._playerHandManager, initiator.UserId, PropertyCardColoursEnum.Cyan);
            var targetBrownProperties = ResponseTestHelpers.GetPropertyGroupSafely(this._playerHandManager, target.UserId, PropertyCardColoursEnum.Brown);
            var targetRedProperties = ResponseTestHelpers.GetPropertyGroupSafely(this._playerHandManager, target.UserId, PropertyCardColoursEnum.Red);


            // Initiator should now have target's cyan property
            Assert.Contains(targetProperty, initiatorCyanProperties);
            Assert.DoesNotContain(initiatorProperty, initiatorRedProperties);

            // Target should now have initiator's red property
            Assert.Contains(initiatorProperty, targetRedProperties);
            Assert.DoesNotContain(targetProperty, targetBrownProperties);
        }
        #endregion

        #region ShieldsUp Response Tests

        [Fact]
        public void HostileTakeover_WithShieldsUpResponse_ActionBlocked()
        {
            // Arrange
            GameStateTestHelpers.PopulateTestDeck(this._deckManager);
            var initiator = GameStateTestHelpers.SetupPlayerInGame(this._playerManager, this._playerHandManager, "user1", "Initiator");
            var target = GameStateTestHelpers.SetupPlayerInGame(this._playerManager, this._playerHandManager, "user2", "Target");

            GameStateTestHelpers.GivePlayerPropertySet(this._playerHandManager, target.UserId, PropertyCardColoursEnum.OmniSector, 3);
            var shieldsUpCard = CardTestHelpers.CreateCommandCard(ActionTypes.ShieldsUp);
            GameStateTestHelpers.GivePlayerCards(this._playerHandManager, target.UserId, [shieldsUpCard]);

            var hostileTakeoverCard = CardTestHelpers.CreateCommandCard(ActionTypes.HostileTakeover);

            // Debug: Check initial state
            var initialTargetHand = this._playerHandManager.GetPlayerHand(target.UserId);
            Console.WriteLine($"Initial target hand count: {initialTargetHand.Count}");
            Console.WriteLine($"Initial target has ShieldsUp: {VerificationTestHelpers.PlayerHasCard(this._playerHandManager, target.UserId, shieldsUpCard)}");

            // Act - Execute the full flow until ShieldsUp decision
            var actionContext = this._actionExecutionManager.ExecuteAction(initiator.UserId, hostileTakeoverCard, initiator, this._playerManager.GetAllPlayers());

            var playerSelectionResponse = ResponseTestHelpers.CreatePlayerSelectionResponse(actionContext, target.UserId);
            var step2Result = this._dialogManager.RegisterActionResponse(initiator, playerSelectionResponse);

            var propertySetResponse = ResponseTestHelpers.CreatePropertySetResponse(step2Result.NewActionContexts[0], PropertyCardColoursEnum.OmniSector);
            var step3Result = this._dialogManager.RegisterActionResponse(initiator, propertySetResponse);

            // Debug: Check state before ShieldsUp
            var beforeShieldsUpHand = this._playerHandManager.GetPlayerHand(target.UserId);
            Console.WriteLine($"Before ShieldsUp hand count: {beforeShieldsUpHand.Count}");
            Console.WriteLine($"Before ShieldsUp has card: {VerificationTestHelpers.PlayerHasCard(this._playerHandManager, target.UserId, shieldsUpCard)}");

            // Act - Target uses ShieldsUp
            var shieldsUpResponse = ResponseTestHelpers.CreateShieldsUpResponse(step3Result.NewActionContexts[0]);

            try
            {
                var finalResult = this._dialogManager.RegisterActionResponse(target, shieldsUpResponse);
                Console.WriteLine($"RegisterActionResponse succeeded, ShouldClearPendingAction: {finalResult.ShouldClearPendingAction}");

                // Debug: Check state immediately after
                var afterShieldsUpHand = this._playerHandManager.GetPlayerHand(target.UserId);
                Console.WriteLine($"After ShieldsUp hand count: {afterShieldsUpHand.Count}");
                Console.WriteLine($"After ShieldsUp has card: {VerificationTestHelpers.PlayerHasCard(this._playerHandManager, target.UserId, shieldsUpCard)}");

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
            var finalTargetHand = this._playerHandManager.GetPlayerHand(target.UserId);
            Console.WriteLine($"Final target hand count: {finalTargetHand.Count}");
            Console.WriteLine($"Final target has ShieldsUp: {VerificationTestHelpers.PlayerHasCard(this._playerHandManager, target.UserId, shieldsUpCard)}");

            // Verify action was blocked - properties remain with target
            var targetProperties = ResponseTestHelpers.GetPropertyGroupSafely(this._playerHandManager, target.UserId, PropertyCardColoursEnum.OmniSector);
            var initiatorProperties = ResponseTestHelpers.GetPropertyGroupSafely(this._playerHandManager, initiator.UserId, PropertyCardColoursEnum.OmniSector);

            Assert.Equal(3, targetProperties.Count); // Properties stay with target
            Assert.Empty(initiatorProperties); // Initiator gets nothing

            // Verify ShieldsUp card was discarded from hand
            Assert.False(VerificationTestHelpers.PlayerHasCard(this._playerHandManager, target.UserId, shieldsUpCard));
        }

        [Fact]
        public void PirateRaid_WithShieldsUpResponse_ActionBlocked()
        {
            // Arrange
            GameStateTestHelpers.PopulateTestDeck(this._deckManager);
            var initiator = GameStateTestHelpers.SetupPlayerInGame(this._playerManager, this._playerHandManager, "user1", "Initiator");
            var target = GameStateTestHelpers.SetupPlayerInGame(this._playerManager, this._playerHandManager, "user2", "Target");

            var targetProperties = GameStateTestHelpers.GivePlayerPropertySet(this._playerHandManager, target.UserId, PropertyCardColoursEnum.Green, 1);
            var shieldsUpCard = CardTestHelpers.CreateCommandCard(ActionTypes.ShieldsUp);
            GameStateTestHelpers.GivePlayerCards(this._playerHandManager, target.UserId, [shieldsUpCard]);

            var pirateRaidCard = CardTestHelpers.CreateCommandCard(ActionTypes.PirateRaid);

            // Act - Execute flow until ShieldsUp
            var actionContext = this._actionExecutionManager.ExecuteAction(initiator.UserId, pirateRaidCard, initiator, this._playerManager.GetAllPlayers());

            var playerSelectionResponse = ResponseTestHelpers.CreatePlayerSelectionResponse(actionContext, target.UserId);
            var step2Result = this._dialogManager.RegisterActionResponse(initiator, playerSelectionResponse);

            var tableHandResponse = ResponseTestHelpers.CreateTableHandResponse(step2Result.NewActionContexts[0], targetProperties[0].CardGuid.ToString());
            var step3Result = this._dialogManager.RegisterActionResponse(initiator, tableHandResponse);

            // Should trigger ShieldsUp dialog
            Assert.Equal(DialogTypeEnum.ShieldsUp, step3Result.NewActionContexts[0].DialogToOpen);

            // Act - Target uses ShieldsUp 
            var shieldsUpResponse = ResponseTestHelpers.CreateShieldsUpResponse(step3Result.NewActionContexts[0]);
            var finalResult = this._dialogManager.RegisterActionResponse(target, shieldsUpResponse);

            // Assert
            Assert.True(finalResult.ShouldClearPendingAction);

            // Verify property raid was blocked
            var targetPropertiesAfter = ResponseTestHelpers.GetPropertyGroupSafely(this._playerHandManager, target.UserId, PropertyCardColoursEnum.Green);
            var initiatorProperties = ResponseTestHelpers.GetPropertyGroupSafely(this._playerHandManager, initiator.UserId, PropertyCardColoursEnum.Green);

            Assert.Single(targetPropertiesAfter); // Target keeps property
            Assert.Empty(initiatorProperties); // Initiator gets nothing
        }

        #endregion
    }
}