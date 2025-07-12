namespace property_dealer_API.Application.Consts
{
    public static class StoredDataKeys
    {
        /// <summary>
        /// The unique identifier (string) of the primary target player.
        /// </summary>
        public const string TargetPlayers = "TargetPlayers";

        /// <summary>
        /// The unique identifier (string) of the property selected by the initiator.
        /// </summary>
        public const string MyPropertyToTradeId = "MyPropertyToTradeId";

        /// <summary>
        /// The unique identifier (string) of the full property set selected.
        /// </summary>
        public const string SelectedPropertySetId = "SelectedPropertySetId";

        /// <summary>
        /// The color (PropertyCardColoursEnum) selected for a wildcard.
        /// </summary>
        public const string SelectedWildcardColor = "SelectedWildcardColor";
    }
}
