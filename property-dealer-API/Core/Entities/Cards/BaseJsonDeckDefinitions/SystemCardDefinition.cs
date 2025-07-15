using System.Text.Json.Serialization;

namespace property_dealer_API.Models.Cards.BaseJsonDeckDefinitions
{
    public class SystemCardDefinition
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("value")]
        public int Value { get; set; }

        [JsonPropertyName("color")]
        public string Color { get; set; } = string.Empty; // We'll map this string to an enum

        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("maxCards")]
        public int MaxCards { get; set; }

        [JsonPropertyName("rentalValues")]
        public List<int> RentalValues { get; set; } = [];

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;
    }
}