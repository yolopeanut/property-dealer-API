namespace property_dealer_API.Models.Enums.Cards
{
    public enum ActionTypes
    {
        // Standard Commands
        HostileTakeover,  // Replaces DealBreaker
        ForcedTrade,      // Replaces ForcedDeal
        PirateRaid,       // Replaces SlyDeal
        BountyHunter,     // Replaces DebtCollector
        TradeDividend,    // Replaces ItsMyBirthday
        ExploreNewSector, // Replaces PassGo

        // Construction Commands
        SpaceStation,     // Replaces House
        Starbase,         // Replaces Hotel

        // Modifiers & Responses
        TradeEmbargo,     // Replaces DoubleTheRent
        ShieldsUp         // Replaces JustSayNo
    }
}
