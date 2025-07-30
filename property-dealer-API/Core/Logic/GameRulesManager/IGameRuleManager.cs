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
        JoinGameResponseEnum? ValidatePlayerJoining(GameStateEnum gameState, List<Player> players, string? maxNumPlayers);
        void ValidateTurn(string userId, string currentUserIdTurn);
        void ValidateActionLimit(string userId, int noOfActionsPlayed);
        void ValidatePropertyPileCardType(Card cardRemoved);
        PropertyCardColoursEnum ValidateStandardPropertyCardDestination(PropertyCardColoursEnum? cardColoursDestinationEnum);
        List<Player> IdentifyWhoSeesDialog(Player callerUser, Player? targetUser, List<Player> playerList, DialogTypeEnum dialogToOpen);
        Boolean DoesPlayerHaveShieldsUp(Player player, List<Card> playerHand);
        bool IsPlayerHandEmpty(List<Card> cards);
        int CalculateRentAmount(string actionInitiatingPlayerId, TributeCard tributeCard, PropertyCardColoursEnum targetColor, List<Card> playerPropertyCards);
        int? GetPaymentAmount(ActionTypes actionType);
        Boolean CheckIfPlayerWon(List<PropertyCardGroup> tableHand);
    }
}