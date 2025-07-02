using System.Text.Json.Serialization;

namespace property_dealer_API.Models.Cards.BaseJsonDeckDefinitions
{
    public class DeckConfiguration
    {
        [JsonPropertyName("totalCards")]
        public int TotalCards { get; set; }

        [JsonPropertyName("money")]
        public List<MoneyCardDefinition> Money { get; set; } = [];

        [JsonPropertyName("systems")]
        public List<SystemCardDefinition> Systems { get; set; } = [];

        [JsonPropertyName("systemWilds")]
        public List<SystemWildCardDefinition> SystemWilds { get; set; } = [];

        [JsonPropertyName("commands")]
        public List<CommandCardDefinition> Commands { get; set; } = [];

        [JsonPropertyName("tributes")]
        public List<TributeCardDefinition> Tributes { get; set; } = [];
    }
}
