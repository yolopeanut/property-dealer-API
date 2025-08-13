using property_dealer_API.Core.Logic.ActionExecution.ActionHandlers;
using property_dealer_API.Models.Enums.Cards;

namespace property_dealer_API.Core.Logic.ActionExecution.ActionHandlerResolvers
{
    public class ActionHandlerResolver : IActionHandlerResolver
    {
        private readonly IServiceProvider _serviceProvider;

        public ActionHandlerResolver(IServiceProvider serviceProvider)
        {
            this._serviceProvider = serviceProvider;
        }

        public IActionHandler GetHandler(ActionTypes actionType)
        {
            return actionType switch
            {
                ActionTypes.HostileTakeover => _serviceProvider.GetRequiredService<HostileTakeoverHandler>(),
                ActionTypes.ForcedTrade => _serviceProvider.GetRequiredService<ForcedTradeHandler>(),
                ActionTypes.ExploreNewSector => _serviceProvider.GetRequiredService<ExploreNewSectorHandler>(),
                //ActionTypes.ShieldsUp => _serviceProvider.GetRequiredService<ShieldsUpHandler>(),
                ActionTypes.PirateRaid => _serviceProvider.GetRequiredService<PirateRaidHandler>(),
                ActionTypes.BountyHunter => _serviceProvider.GetRequiredService<BountyHunterHandler>(),
                ActionTypes.TradeDividend => _serviceProvider.GetRequiredService<TradeDividendHandler>(),
                ActionTypes.SpaceStation => _serviceProvider.GetRequiredService<SpaceStationHandler>(),
                ActionTypes.Starbase => _serviceProvider.GetRequiredService<StarbaseHandler>(),
                ActionTypes.TradeEmbargo => _serviceProvider.GetRequiredService<TradeEmbargoHandler>(),
                ActionTypes.SystemWildCard => _serviceProvider.GetRequiredService<SystemWildCardHandler>(),
                ActionTypes.TributeWildCard => _serviceProvider.GetRequiredService<WildCardTributeHandler>(),
                ActionTypes.Tribute => _serviceProvider.GetRequiredService<TributeCardHandler>(),
                _ => throw new InvalidOperationException($"No handler found for action type: {actionType}")
            };
        }
    }
}