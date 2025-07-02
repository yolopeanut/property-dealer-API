using property_dealer_API.Models.Enums.Cards;

namespace property_dealer_API.Models.Cards
{
    public abstract class Card
    {
        public CardTypesEnum CardType { get; set; }
        public string Name { get; set; }
        public int? BankValue { get; set; }
        public string? Description { get; set; }

        public Card(CardTypesEnum cardType, string? name, int? bankValue, string? description)
        {
            this.CardType = cardType;
            this.Name = name ?? "No Name";
            this.BankValue = bankValue;
            this.Description = description;
        }

        public override string ToString() => $"{Name} (Bank Value: {BankValue}M Credits)";

    }
}
