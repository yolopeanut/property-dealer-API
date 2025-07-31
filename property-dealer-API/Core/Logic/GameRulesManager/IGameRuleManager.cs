using property_dealer_API.Application.Enums;
using property_dealer_API.Core.Entities;
using property_dealer_API.Core.Entities.Cards.CardRelatedEntities;
using property_dealer_API.Models.Cards;
using property_dealer_API.Models.Enums;
using property_dealer_API.Models.Enums.Cards;

namespace property_dealer_API.Core.Logic.GameRulesManager
{
    public interface IGameRuleManager
    {
        // ===== JOINING & GAME STATE VALIDATION =====
        JoinGameResponseEnum? ValidatePlayerJoining(GameStateEnum gameState, List<Player> players, string? maxNumPlayers);
        void ValidatePlayerCanPlayCard(GameStateEnum gameState, string playerId, string currentTurnPlayerId, int noOfActionsPlayed);

        // ===== CARD PLACEMENT VALIDATION =====
        void ValidatePropertyPileCardType(Card cardRemoved);
        PropertyCardColoursEnum ValidateStandardPropertyCardDestination(PropertyCardColoursEnum? cardColoursDestinationEnum);

        // ===== CARD-SPECIFIC RULE VALIDATION =====
        void ValidateHostileTakeoverTarget(List<PropertyCardGroup> targetPlayerTableHand, PropertyCardColoursEnum targetColor);
        void ValidatePirateRaidTarget(List<PropertyCardGroup> targetPlayerTableHand, PropertyCardColoursEnum targetColor);
        void ValidateForcedTradeTarget(List<PropertyCardGroup> targetPlayerTableHand, PropertyCardColoursEnum targetColor);
        void ValidateSpaceStationPlacement(List<PropertyCardGroup> playerTableHand, PropertyCardColoursEnum targetColor);
        void ValidateStarbasePlacement(List<PropertyCardGroup> playerTableHand, PropertyCardColoursEnum targetColor);
        void ValidateRentTarget(PropertyCardColoursEnum targetColor, List<Card> targetPlayerProperties);
        void ValidateEndOfTurnCardLimit(List<Card> playerHand);
        void ValidateTradeEmbargoTarget(List<PropertyCardGroup> targetPlayerTableHand, PropertyCardColoursEnum targetColor);
        void ValidateRentCardColors(PropertyCardColoursEnum rentCardColor, PropertyCardColoursEnum targetColor);
        void ValidateWildcardRentTarget(List<PropertyCardColoursEnum> availableColors, PropertyCardColoursEnum selectedColor);

        // ===== QUERIES =====
        bool DoesPlayerHaveShieldsUp(Player player, List<Card> playerHand);
        bool IsPlayerHandEmpty(List<Card> cards);
        bool CheckIfPlayerWon(List<PropertyCardGroup> tableHand);
        bool IsPropertySetComplete(List<PropertyCardGroup> tableHand, PropertyCardColoursEnum color);

        // ===== CALCULATIONS =====
        int CalculateRentAmount(string actionInitiatingPlayerId, TributeCard tributeCard, PropertyCardColoursEnum targetColor, List<Card> playerPropertyCards);
        int? GetPaymentAmount(ActionTypes actionType);

        // ===== UI LOGIC =====
        List<Player> IdentifyWhoSeesDialog(Player callerUser, Player? targetUser, List<Player> playerList, DialogTypeEnum dialogToOpen);
    }
}