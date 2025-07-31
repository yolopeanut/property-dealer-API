using property_dealer_API.Application.Enums;
using property_dealer_API.Application.Exceptions;
using property_dealer_API.Core.Entities;
using property_dealer_API.Core.Entities.Cards.CardRelatedEntities;
using property_dealer_API.Models.Cards;
using property_dealer_API.Models.Enums;
using property_dealer_API.Models.Enums.Cards;

namespace property_dealer_API.Core.Logic.GameRulesManager
{
    /// <summary>
    /// Stateless class responsible for validating game rules and providing game-related queries.
    /// Validation methods throw exceptions with specific error messages for user feedback.
    /// Query methods return boolean or calculated values.
    /// </summary>
    public class GameRuleManager : IGameRuleManager
    {
        #region Game State & Player Validation

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

        public void ValidatePlayerCanPlayCard(GameStateEnum gameState, string playerId, string currentTurnPlayerId)
        {
            if (gameState != GameStateEnum.GameStarted)
            {
                throw new InvalidGameStateException(gameState, GameStateEnum.GameStarted, "play cards");
            }

            ValidateTurn(playerId, currentTurnPlayerId);
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

        #endregion

        #region Card Placement Validation

        public void ValidatePropertyPileCardType(Card cardRemoved)
        {
            if (cardRemoved is not StandardSystemCard)
            {
                throw new InvalidOperationException($"Cannot play a {cardRemoved.CardType} card on the property section. Only property cards are allowed.");
            }
        }

        public PropertyCardColoursEnum ValidateStandardPropertyCardDestination(PropertyCardColoursEnum? cardColoursDestinationEnum)
        {
            if (cardColoursDestinationEnum == null)
            {
                throw new InvalidOperationException("Property card destination color cannot be null. Please select a valid color.");
            }

            return cardColoursDestinationEnum.Value;
        }

        #endregion

        #region Card-Specific Rule Validation

        public void ValidateHostileTakeoverTarget(List<PropertyCardGroup> targetPlayerTableHand, PropertyCardColoursEnum targetColor)
        {
            var propertyGroup = targetPlayerTableHand.FirstOrDefault(group => group.cardColorEnum == targetColor);

            if (propertyGroup == null)
            {
                throw new InvalidTargetException("Hostile Takeover", targetColor, "target", "player doesn't own any properties of this color");
            }

            var maxCards = propertyGroup.groupedPropertyCards.First().MaxCards ?? 0;
            var currentCount = propertyGroup.groupedPropertyCards.Count;

            if (currentCount < maxCards)
            {
                throw new IncompletePropertySetException(targetColor, currentCount, maxCards);
            }
        }

        public void ValidatePirateRaidTarget(List<PropertyCardGroup> targetPlayerTableHand, PropertyCardColoursEnum targetColor)
        {
            var propertyGroup = targetPlayerTableHand.FirstOrDefault(group => group.cardColorEnum == targetColor);

            if (propertyGroup == null)
            {
                throw new InvalidTargetException("Pirate Raid", targetColor, "target", "player doesn't own any properties of this color");
            }

            var maxCards = propertyGroup.groupedPropertyCards.First().MaxCards ?? 0;
            var currentCount = propertyGroup.groupedPropertyCards.Count;

            if (currentCount >= maxCards)
            {
                throw new CompletePropertySetException(targetColor);
            }
        }

        public void ValidateForcedTradeTarget(List<PropertyCardGroup> targetPlayerTableHand, PropertyCardColoursEnum targetColor)
        {
            if (IsPropertySetComplete(targetPlayerTableHand, targetColor))
            {
                throw new InvalidOperationException($"Forced Trade cannot target completed property sets. The {targetColor} set is already complete and protected.");
            }

            // Also validate that the target actually has properties of this color
            var propertyGroup = GetPropertyGroup(targetPlayerTableHand, targetColor);
            if (propertyGroup == null || propertyGroup.groupedPropertyCards.Count == 0)
            {
                throw new InvalidOperationException($"Forced Trade cannot target the {targetColor} property set because the target player doesn't own any {targetColor} properties.");
            }
        }

        public void ValidateSpaceStationPlacement(List<PropertyCardGroup> playerTableHand, PropertyCardColoursEnum targetColor)
        {
            if (!IsPropertySetComplete(playerTableHand, targetColor))
            {
                var propertyGroup = GetPropertyGroup(playerTableHand, targetColor);
                if (propertyGroup != null)
                {
                    var currentCount = propertyGroup.groupedPropertyCards.Count;
                    var requiredCount = propertyGroup.groupedPropertyCards.First().MaxCards;
                    throw new InvalidOperationException($"Space Station can only be played on completed property sets. Your {targetColor} set has {currentCount}/{requiredCount} properties.");
                }
                else
                {
                    throw new InvalidOperationException($"Space Station cannot be played on the {targetColor} property set because you don't own any {targetColor} properties.");
                }
            }
        }

        public void ValidateStarbasePlacement(List<PropertyCardGroup> playerTableHand, PropertyCardColoursEnum targetColor)
        {
            if (!IsPropertySetComplete(playerTableHand, targetColor))
            {
                var propertyGroup = GetPropertyGroup(playerTableHand, targetColor);
                if (propertyGroup != null)
                {
                    var currentCount = propertyGroup.groupedPropertyCards.Count;
                    var requiredCount = propertyGroup.groupedPropertyCards.First().MaxCards;
                    throw new InvalidOperationException($"Starbase can only be played on completed property sets. Your {targetColor} set has {currentCount}/{requiredCount} properties.");
                }
                else
                {
                    throw new InvalidOperationException($"Starbase cannot be played on the {targetColor} property set because you don't own any {targetColor} properties.");
                }
            }

            // Additional rule: Starbase requires Space Station first (if you want this rule)
            // TODO: Add logic to check if Space Station exists on the property set
        }

        public void ValidateRentTarget(PropertyCardColoursEnum targetColor, List<Card> targetPlayerProperties)
        {
            var hasTargetColorProperties = targetPlayerProperties.Any(card =>
                card is StandardSystemCard systemCard && systemCard.CardColoursEnum == targetColor);

            if (!hasTargetColorProperties)
            {
                throw new InvalidOperationException($"Cannot charge rent for {targetColor} properties because the target player doesn't own any {targetColor} properties.");
            }
        }

        public void ValidateTradeEmbargoTarget(List<PropertyCardGroup> targetPlayerTableHand, PropertyCardColoursEnum targetColor)
        {
            var propertyGroup = GetPropertyGroup(targetPlayerTableHand, targetColor);

            if (propertyGroup == null || propertyGroup.groupedPropertyCards.Count == 0)
            {
                throw new InvalidTargetException("Trade Embargo", targetColor, "target", "player doesn't own any properties of this color");
            }
        }

        public void ValidateRentCardColors(PropertyCardColoursEnum rentCardColor, PropertyCardColoursEnum targetColor)
        {
            // For specific color rent cards, target must match
            if (rentCardColor != PropertyCardColoursEnum.OmniSector && rentCardColor != targetColor)
            {
                throw new InvalidOperationException($"Cannot use {rentCardColor} rent card to charge rent on {targetColor} properties. Colors must match.");
            }
        }

        public void ValidateWildcardRentTarget(List<PropertyCardColoursEnum> availableColors, PropertyCardColoursEnum selectedColor)
        {
            if (!availableColors.Contains(selectedColor))
            {
                throw new InvalidOperationException($"Cannot charge rent for {selectedColor} properties. Available colors are: {string.Join(", ", availableColors)}.");
            }
        }

        public void ValidateEndOfTurnCardLimit(List<Card> playerHand)
        {
            const int MAX_CARDS_IN_HAND = 7;

            if (playerHand.Count > MAX_CARDS_IN_HAND)
            {
                var excessCards = playerHand.Count - MAX_CARDS_IN_HAND;
                throw new InvalidOperationException($"You must discard {excessCards} card(s) to end your turn. Hand limit is {MAX_CARDS_IN_HAND} cards.");
            }
        }

        #endregion

        #region Queries (Boolean Returns)

        public bool DoesPlayerHaveShieldsUp(Player player, List<Card> playerHand)
        {
            return playerHand.Any(card => card is CommandCard commandCard && commandCard.Command == ActionTypes.ShieldsUp);
        }

        public bool IsPlayerHandEmpty(List<Card> cards)
        {
            return cards.Count == 0;
        }

        public bool CheckIfPlayerWon(List<PropertyCardGroup> tableHand)
        {
            var completeSets = 0;
            foreach (var propertyGroup in tableHand)
            {
                var groupedPropertyCards = propertyGroup.groupedPropertyCards;
                var maxCardsForPropertyGroup = groupedPropertyCards.First().MaxCards;

                if (groupedPropertyCards.Count >= maxCardsForPropertyGroup)
                {
                    completeSets++;
                }
            }

            return completeSets >= 3;
        }

        public bool IsPropertySetComplete(List<PropertyCardGroup> tableHand, PropertyCardColoursEnum color)
        {
            var propertyGroup = GetPropertyGroup(tableHand, color);
            if (propertyGroup == null) return false;

            var maxCards = propertyGroup.groupedPropertyCards.First().MaxCards;
            return propertyGroup.groupedPropertyCards.Count >= maxCards;
        }

        #endregion

        #region Calculations

        public int CalculateRentAmount(string actionInitiatingPlayerId, TributeCard tributeCard, PropertyCardColoursEnum targetColor, List<Card> playerPropertyCards)
        {
            int cardCount = playerPropertyCards.Count(card =>
                card is StandardSystemCard systemCard && systemCard.CardColoursEnum == targetColor);

            var systemCard = playerPropertyCards.FirstOrDefault(card =>
                card is StandardSystemCard sc && sc.CardColoursEnum == targetColor) as StandardSystemCard;

            if (systemCard == null || cardCount == 0)
            {
                return 0;
            }

            int rentalIndex = Math.Min(cardCount - 1, systemCard.RentalValues.Count - 1);
            return systemCard.RentalValues[rentalIndex];
        }

        public int? GetPaymentAmount(ActionTypes actionType)
        {
            return actionType switch
            {
                ActionTypes.BountyHunter => 5,
                ActionTypes.TradeDividend => 2,
                _ => null
            };
        }

        #endregion

        #region UI Logic

        public List<Player> IdentifyWhoSeesDialog(Player callerUser, Player? targetUser, List<Player> playerList, DialogTypeEnum dialogToOpen)
        {
            var playerListCopy = new List<Player>(playerList);

            return dialogToOpen switch
            {
                DialogTypeEnum.PayValue => targetUser == null
                    ? playerListCopy.Where(p => p.UserId != callerUser.UserId).ToList()
                    : [targetUser],

                DialogTypeEnum.PlayerSelection or
                DialogTypeEnum.PropertySetSelection or
                DialogTypeEnum.TableHandSelector or
                DialogTypeEnum.WildcardColor => [callerUser],

                DialogTypeEnum.ShieldsUp => targetUser != null
                    ? [targetUser]
                    : throw new InvalidOperationException("Cannot give ShieldsUp dialog if target user is null"),

                _ => throw new InvalidOperationException($"No dialog handling defined for {dialogToOpen}")
            };
        }

        #endregion

        #region Private Helper Methods
        private PropertyCardGroup? GetPropertyGroup(List<PropertyCardGroup> tableHand, PropertyCardColoursEnum color)
        {
            return tableHand.FirstOrDefault(group => group.cardColorEnum == color);
        }
        #endregion
    }
}