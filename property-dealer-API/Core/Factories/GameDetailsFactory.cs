using property_dealer_API.Core.Entities;
using property_dealer_API.Core.Logic.ActionExecution;
using property_dealer_API.Core.Logic.ActionExecution.ActionsContextBuilder;
using property_dealer_API.Core.Logic.DebuggingManager;
using property_dealer_API.Core.Logic.DecksManager;
using property_dealer_API.Core.Logic.DialogsManager;
using property_dealer_API.Core.Logic.GameRulesManager;
using property_dealer_API.Core.Logic.GameStateMapper;
using property_dealer_API.Core.Logic.PendingActionsManager;
using property_dealer_API.Core.Logic.PlayerHandsManager;
using property_dealer_API.Core.Logic.PlayersManager;
using property_dealer_API.Core.Logic.TurnExecutionsManager;
using property_dealer_API.Core.Logic.TurnManager;

namespace property_dealer_API.Core.Factories
{
    public class GameDetailsFactory : IGameDetailsFactory
    {
        public GameDetails CreateGameDetails(string roomId, string roomName, GameConfig config)
        {
            // Create all dependencies
            var deckManager = new DeckManager();
            var playerManager = new PlayerManager();
            var playerHandManager = new PlayersHandManager();
            var gameStateMapper = new GameStateMapper(playerHandManager, playerManager);
            var rulesManager = new GameRuleManager();
            var turnManager = new TurnManager(roomId);
            var pendingActionManager = new PendingActionManager();

            var actionContextBuilder = new ActionContextBuilder(pendingActionManager, rulesManager, deckManager, playerHandManager);
            var actionExecutor = new ActionExecutor(playerHandManager, deckManager, rulesManager);
            var dialogProcessor = new DialogResponseProcessor(playerHandManager, playerManager, rulesManager, pendingActionManager, actionExecutor);

            var actionExecutionManager = new ActionExecutionManager(actionContextBuilder, dialogProcessor);

            var debugManager = new DebugManager(
                playerHandManager,
                playerManager,
                rulesManager,
                pendingActionManager,
                deckManager);

            var turnExecutionManager = new TurnExecutionManager(
                playerHandManager,
                playerManager,
                rulesManager,
                actionExecutionManager);

            var dialogManager = new DialogManager(
                actionExecutionManager,
                pendingActionManager);


            return new GameDetails(
                roomId,
                roomName,
                config,
                deckManager,
                playerManager,
                playerHandManager,
                gameStateMapper,
                rulesManager,
                turnManager,
                debugManager,
                turnExecutionManager,
                dialogManager
                );
        }
    }
}