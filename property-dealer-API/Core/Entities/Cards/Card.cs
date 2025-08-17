using property_dealer_API.Application.DTOs.Responses;
using property_dealer_API.Models.Enums.Cards;

namespace property_dealer_API.Models.Cards
{
    public abstract class Card
    {
        public Guid CardGuid { get; set; }
        public CardTypesEnum CardType { get; set; }
        public string Name { get; set; }
        public int? BankValue { get; set; }
        public string? Description { get; set; }

        protected Card(CardTypesEnum cardType, string? name, int? bankValue, string? description)
        {
            this.CardGuid = Guid.NewGuid();
            this.CardType = cardType;
            this.Name = name ?? "No Name";
            this.BankValue = bankValue;
            this.Description = description;
        }

        public override string ToString() => $"{this.Name} (Bank Value: {this.BankValue}M Credits)";

        public virtual CardDto ToDto()
        {
            return new CardDto
            {
                // Map all the properties that EVERY card has
                CardGuid = this.CardGuid,
                CardType = this.CardType,
                Name = this.Name,
                BankValue = this.BankValue,
                Description = this.Description,

                // Set defaults for properties that are not common
                Command = null,
                TargetColorsToApplyRent = null,
                RentalValues = null,
                MaxCards = null,
                CardColoursEnum = null
            };
        }

    }
}
