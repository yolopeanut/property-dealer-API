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
                ActionTypes.HostileTakeover =>
                    this._serviceProvider.GetRequiredService<HostileTakeoverHandler>(),
                ActionTypes.ForcedTrade =>
                    this._serviceProvider.GetRequiredService<ForcedTradeHandler>(),
                ActionTypes.ExploreNewSector =>
                    this._serviceProvider.GetRequiredService<ExploreNewSectorHandler>(),
                //ActionTypes.ShieldsUp => _serviceProvider.GetRequiredService<ShieldsUpHandler>(),
                ActionTypes.PirateRaid =>
                    this._serviceProvider.GetRequiredService<PirateRaidHandler>(),
                ActionTypes.BountyHunter =>
                    this._serviceProvider.GetRequiredService<BountyHunterHandler>(),
                ActionTypes.TradeDividend =>
                    this._serviceProvider.GetRequiredService<TradeDividendHandler>(),
                ActionTypes.SpaceStation =>
                    this._serviceProvider.GetRequiredService<SpaceStationHandler>(),
                ActionTypes.Starbase => this._serviceProvider.GetRequiredService<StarbaseHandler>(),
                ActionTypes.TradeEmbargo =>
                    this._serviceProvider.GetRequiredService<TradeEmbargoHandler>(),
                ActionTypes.SystemWildCard =>
                    this._serviceProvider.GetRequiredService<SystemWildCardHandler>(),
                ActionTypes.TributeWildCard =>
                    this._serviceProvider.GetRequiredService<WildCardTributeHandler>(),
                ActionTypes.Tribute =>
                    this._serviceProvider.GetRequiredService<TributeCardHandler>(),
                _ => throw new InvalidOperationException(
                    $"No handler found for action type: {actionType}"
                ),
            };
        }
    }
}
