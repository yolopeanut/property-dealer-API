using System.Text.Json.Serialization;

namespace property_dealer_API.Models.Cards.BaseJsonDeckDefinitions
{
    public class MoneyCardDefinition
    {
        [JsonPropertyName("value")]
        public int Value { get; set; }

        [JsonPropertyName("count")]
        public int Count { get; set; }
    }
}
