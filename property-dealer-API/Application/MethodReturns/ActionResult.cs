using property_dealer_API.Application.DTOs.Responses;
using property_dealer_API.Models.Enums.Cards;

namespace property_dealer_API.Application.MethodReturns
{
    public class ActionResult
    {
        public required string ActionInitiatingPlayerId { get; set; }
        public required string AffectedPlayerId { get; set; }
        public ActionTypes ActionType { get; set; }
        public PropertyCardColoursEnum? TakenPropertySet { get; set; }
        public CardDto? TakenCard { get; set; }
        public CardDto? GivenCard { get; set; }

        public override string ToString()
        {
            return $"ActionInitiatingPlayerId: {this.ActionInitiatingPlayerId}, AffectedPlayerId: {this.AffectedPlayerId}, ActionType: {this.ActionType}, TakenPropertySet:{this.TakenPropertySet}, TakenCard:{this.TakenCard}, GivenCard:{this.GivenCard}";
        }
    }
}
