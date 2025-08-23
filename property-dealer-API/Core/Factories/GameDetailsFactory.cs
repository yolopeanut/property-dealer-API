using property_dealer_API.Core.Entities;
using property_dealer_API.Core.Logic.ActionExecution.ActionHandlerResolvers;
using property_dealer_API.Core.Logic.DebuggingManager;
using property_dealer_API.Core.Logic.DecksManager;
using property_dealer_API.Core.Logic.DialogsManager;
using property_dealer_API.Core.Logic.GameRulesManager;
using property_dealer_API.Core.Logic.GameStateMapper;
using property_dealer_API.Core.Logic.PlayerHandsManager;
using property_dealer_API.Core.Logic.PlayersManager;
using property_dealer_API.Core.Logic.TurnExecutionsManager;
using property_dealer_API.Core.Logic.TurnManager;

namespace property_dealer_API.Core.Factories
{
    public class GameDetailsFactory : IGameDetailsFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public GameDetailsFactory(IServiceProvider serviceProvider)
        {
            this._serviceProvider = serviceProvider;
        }

        public GameDetails CreateGameDetails(string roomId, string roomName, GameConfig config)
        {
            var scope = this._serviceProvider.CreateScope();
            var scopedProvider = scope.ServiceProvider;

            try
            {
                // Create all dependencies
                var deckManager = scopedProvider.GetRequiredService<IDeckManager>();
                var playerManager = scopedProvider.GetRequiredService<IPlayerManager>();
                var playerHandManager = scopedProvider.GetRequiredService<IPlayerHandManager>();
                var gameStateMapper = scopedProvider.GetRequiredService<IGameStateMapper>();
                var rulesManager = scopedProvider.GetRequiredService<IGameRuleManager>();
                var debugManager = scopedProvider.GetRequiredService<IDebugManager>();
                var turnExecutionManager =
                    scopedProvider.GetRequiredService<ITurnExecutionManager>();
                var dialogManager = scopedProvider.GetRequiredService<IDialogManager>();
                var actionHandlerResolver =
                    scopedProvider.GetRequiredService<IActionHandlerResolver>();
                var turnManager = scopedProvider.GetRequiredService<ITurnManager>();

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
                    dialogManager,
                    scope
                );
            }
            catch
            {
                // If creation fails, dispose the scope
                scope.Dispose();
                throw;
            }
        }
    }
}
