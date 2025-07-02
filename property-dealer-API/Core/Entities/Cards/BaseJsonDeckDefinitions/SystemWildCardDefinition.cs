using System.Text.Json.Serialization;

namespace property_dealer_API.Models.Cards.BaseJsonDeckDefinitions
{
    public class SystemWildCardDefinition
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("value")]
        public int Value { get; set; }

        [JsonPropertyName("colors")]
        public List<string> Colors { get; set; } = [];

        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;
    }
}