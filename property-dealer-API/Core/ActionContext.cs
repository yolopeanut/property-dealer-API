using property_dealer_API.Application.Enums;
using property_dealer_API.Core.Entities;
using property_dealer_API.Models.Enums.Cards;

namespace property_dealer_API.Core
{
    // Not in use yet, might need to use when command responses are implmented.
    public class ActionContext
    {
        // Variables the initiator sets
        public required string CardId { get; set; }
        public required string ActionInitiatingPlayerId { get; set; }
        public required ActionTypes ActionType { get; set; }

        // Variable responses 
        // E.g P1 sends Hostile Takeover, dialog target will be P1 (select player)
        // P1 then selects P2.
        // P2 will be new dialog target (notification/shields up)
        public required List<Player> DialogTargetList { get; set; }
        public required DialogTypeEnum DialogToOpen { get; set; }

        // Response variables 
        public string? TargetPlayerId { get; set; }
        public CommandResponseEnum? DialogResponse { get; set; }
        public PropertyCardColoursEnum? TargetSetColor { get; set; }
        public int? PaymentAmount { get; set; }

        // Used in forced trade
        public string? TargetCardId { get; set; }

        // Used in forced trade, also in pay rent (selecting multiple rent cards)
        public List<string>? OwnTargetCardId { get; set; }

        public ActionContext Clone()
        {
            return (ActionContext)this.MemberwiseClone();
        }
    }
}
