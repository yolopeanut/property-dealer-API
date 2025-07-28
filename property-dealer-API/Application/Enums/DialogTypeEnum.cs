namespace property_dealer_API.Application.Enums
{
    /// <summary>
    /// Defines the type of UI interaction required from a player.
    /// </summary>
    public enum DialogTypeEnum
    {
        /// <summary>
        /// Informs players they must pay a specified amount.
        /// <br/>Used By: TradeDividend(1), Tribute(2)
        /// </summary>
        PayValue,

        /// <summary>
        /// Prompts the current player to select one or more target players.
        /// <br/>Used By: HostileTakeover(1), PirateRaid(1), ForcedTrade(1), BountyHunter(1)
        /// </summary>
        PlayerSelection,

        /// <summary>
        /// Prompts the selection of a complete property set from the table.
        /// <br/>Used By: TradeEmbargo(1), SpaceStation(1), Starbase(1), Tribute(1), HostileTakeover(2)
        /// </summary>
        PropertySetSelection,

        /// <summary>
        /// Asks a targeted player if they want to play their "ShieldsUp" card.
        /// <br/>Used By: As a response to many actions (e.g., HostileTakeover, PirateRaid, etc.)
        /// </summary>
        ShieldsUp,

        /// <summary>
        /// Prompts the selection of individual properties from the table.
        /// <br/>Used By: ForcedTrade(2), PirateRaid(2)
        /// </summary>
        TableHandSelector,

        /// <summary>
        /// Prompts the player to select a color for a wildcard.
        /// <br/>Used By: Wildcard cards
        /// </summary>
        WildcardColor,

        // Debug enums
        SpawnCard

    }
}
