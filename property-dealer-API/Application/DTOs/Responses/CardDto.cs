using property_dealer_API.Models.Enums.Cards;

namespace property_dealer_API.Application.DTOs.Responses
{
    public class CardDto
    {
        // Base Card properties
        public Guid CardGuid { get; set; }
        public CardTypesEnum CardType { get; set; }
        public string? Name { get; set; }
        public int? BankValue { get; set; }
        public string? Description { get; set; }

        // CommandCard properties
        public ActionTypes? Command { get; set; }

        // TributeCard properties
        public List<PropertyCardColoursEnum>? TargetColorsToApplyRent { get; set; }

        // StandardSystemCard properties
        public PropertyCardColoursEnum? CardColoursEnum { get; set; }
        public List<int>? RentalValues { get; set; }
        public int? MaxCards { get; set; }
    }
}
