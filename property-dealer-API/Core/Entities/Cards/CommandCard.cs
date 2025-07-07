using property_dealer_API.Application.DTOs.Responses;
using property_dealer_API.Models.Enums.Cards;

namespace property_dealer_API.Models.Cards
{
    public class CommandCard : Card
    {
        public ActionTypes Command { get; set; }

        public CommandCard(CardTypesEnum cardType, ActionTypes command, string name, int value, string description) : base(cardType, name, value, description)
        {
            this.Command = command;
        }

        public override CardDto ToDto()
        {
            var dto = base.ToDto();
            dto.Command = this.Command;

            return dto;
        }
    }
}
