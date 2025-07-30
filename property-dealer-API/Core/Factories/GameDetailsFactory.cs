using property_dealer_API.Core.Entities;
using property_dealer_API.Core.Logic.ActionExecution;
using property_dealer_API.Core.Logic.DebuggingManager;
using property_dealer_API.Core.Logic.DecksManager;
using property_dealer_API.Core.Logic.GameRulesManager;
using property_dealer_API.Core.Logic.GameStateMapper;
using property_dealer_API.Core.Logic.PendingActionsManager;
using property_dealer_API.Core.Logic.PlayerHandsManager;
using property_dealer_API.Core.Logic.PlayersManager;
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

            var actionExecutionManager = new ActionExecutionManager(
                playerHandManager,  // IPlayerHandManager
                playerManager,      // IPlayerManager
                rulesManager,       // IGameRuleManager
                pendingActionManager, // IPendingActionManager
                deckManager);       // IDeckManager

            var debugManager = new DebugManager(
                playerHandManager,  // IPlayerHandManager
                playerManager,      // IPlayerManager
                rulesManager,       // IGameRuleManager
                pendingActionManager, // IPendingActionManager
                deckManager);       // IDeckManager

            return new GameDetails(
                roomId,
                roomName,
                config,
                deckManager,          // IDeckManager
                playerManager,        // IPlayerManager
                playerHandManager,    // IPlayerHandManager
                gameStateMapper,      // IGameStateMapper
                rulesManager,         // IGameRuleManager
                turnManager,          // ITurnManager
                pendingActionManager, // IPendingActionManager
                actionExecutionManager, // IActionExecutionManager
                debugManager);        // IDebugManager
        }
    }
}