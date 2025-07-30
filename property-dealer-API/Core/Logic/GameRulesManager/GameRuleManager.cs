
using property_dealer_API.Application.Enums;
using property_dealer_API.Application.Exceptions;
using property_dealer_API.Core.Entities;
using property_dealer_API.Core.Entities.Cards.CardRelatedEntities;
using property_dealer_API.Models.Cards;
using property_dealer_API.Models.Enums;
using property_dealer_API.Models.Enums.Cards;

namespace property_dealer_API.Core.Logic.GameRulesManager
{
    // Stateless class which only has the job of validating rules.
    public class GameRuleManager : IGameRuleManager
    {
        public JoinGameResponseEnum? ValidatePlayerJoining(GameStateEnum gameState, List<Player> players, string? maxNumPlayers)
        {
            if (gameState != GameStateEnum.WaitingRoom)
            {
                return JoinGameResponseEnum.AlreadyInGame;
            }

            if (players.Count + 1 > Convert.ToInt32(maxNumPlayers))
            {
                return JoinGameResponseEnum.GameFull;
            }

            return null;

        }

        public void ValidateTurn(string userId, string currentUserIdTurn)
        {
            if (userId != currentUserIdTurn)
            {
                throw new NotPlayerTurnException(userId, currentUserIdTurn);
            }
        }

        public void ValidateActionLimit(string userId, int noOfActionsPlayed)
        {
            if (noOfActionsPlayed >= 3)
            {
                throw new PlayerExceedingActionLimitException(userId);
            }
        }

        public void ValidatePropertyPileCardType(Card cardRemoved)
        {
            if (cardRemoved is not (StandardSystemCard or SystemWildCard))
            {
                throw new InvalidOperationException($"Cannot play a non property card on the property section");
            }
        }

        public PropertyCardColoursEnum ValidateStandardPropertyCardDestination(PropertyCardColoursEnum? cardColoursDestinationEnum)
        {
            if (cardColoursDestinationEnum == null)
            {
                throw new InvalidOperationException($"Card destination color cannot be null");
            }

            return cardColoursDestinationEnum.Value;
        }

        // Method to identify who will receive the dialog on the ui to open.
        // Targeted can be == null/playerid.
        // If null, means everyone targeted, otherwise only one person
        public List<Player> IdentifyWhoSeesDialog(Player callerUser, Player? targetUser, List<Player> playerList, DialogTypeEnum dialogToOpen)
        {
            var playerListCopy = new List<Player>(playerList);
            switch (dialogToOpen)
            {
                // All Players will besides the caller will see this
                case DialogTypeEnum.PayValue:
                    if (targetUser == null)
                    {
                        var filteredPlayerList = playerListCopy.Remove(callerUser);
                        return playerListCopy;
                    }
                    else
                    {
                        return [targetUser];
                    }

                // Only the caller will be sent the dialog
                case DialogTypeEnum.PlayerSelection:
                case DialogTypeEnum.PropertySetSelection:
                case DialogTypeEnum.TableHandSelector:
                case DialogTypeEnum.WildcardColor:
                    return [callerUser];

                case DialogTypeEnum.ShieldsUp:
                    if (targetUser == null)
                    {
                        throw new InvalidOperationException("Cannot give ShieldsUp dialog if target user is null");
                    }

                    return [targetUser];
            }

            throw new InvalidOperationException("No dialog chosen!");
        }

        public Boolean DoesPlayerHaveShieldsUp(Player player, List<Card> playerHand)
        {
            return playerHand.Any(card => card is CommandCard commandCard && commandCard.Command == ActionTypes.ShieldsUp);
        }

        public bool IsPlayerHandEmpty(List<Card> cards)
        {
            if (cards.Count == 0)
            {
                return true;
            }

            return false;
        }

        public int CalculateRentAmount(string actionInitiatingPlayerId, TributeCard tributeCard, PropertyCardColoursEnum targetColor, List<Card> playerPropertyCards)
        {
            int cardCount = playerPropertyCards.Count(card =>
                card is StandardSystemCard systemCard && systemCard.CardColoursEnum == targetColor);

            // Find a system card of the target color to get rental values
            var systemCard = playerPropertyCards.FirstOrDefault(card =>
                card is StandardSystemCard sc && sc.CardColoursEnum == targetColor) as StandardSystemCard;

            if (systemCard == null || cardCount == 0)
            {
                return 0; // No cards of this color
            }

            // Get the rental value based on the number of cards owned
            // Array is 0-indexed, so subtract 1 from card count
            int rentalIndex = Math.Min(cardCount - 1, systemCard.RentalValues.Count - 1);

            return systemCard.RentalValues[rentalIndex];
        }

        public int? GetPaymentAmount(ActionTypes actionType)
        {
            switch (actionType)
            {
                case ActionTypes.BountyHunter:
                    return 5;
                case ActionTypes.TradeDividend:
                    return 2;
                default:
                    return null;
            }
        }

        public Boolean CheckIfPlayerWon(List<PropertyCardGroup> tableHand)
        {
            var countCompleteSets = 0;
            foreach (var propertyGroup in tableHand)
            {
                // The card dto given here contains the maxcards for each group.
                // Only need to take the first of them to know the max card for the group.
                var groupedPropertyCards = propertyGroup.groupedPropertyCards;
                var maxCardsForPropertyGroup = groupedPropertyCards.First().MaxCards;

                if (groupedPropertyCards.Count >= maxCardsForPropertyGroup)
                {
                    countCompleteSets += 1;
                }
            }

            if (countCompleteSets >= 3)
            {
                return true;
            }

            return false;
        }


        //public Boolean CanPlayCard(GameStateEnum currentGameState, string turnKeeperCurrUserTurn, string currentPlayer)
        //{

        //}

    }
}
