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

        public void ValidatePlayerCanPlayCard(GameStateEnum gameState, string playerId, string currentTurnPlayerId, int noOfActionPlayed)
        {
            if (gameState != GameStateEnum.GameStarted)
            {
                throw new InvalidGameStateException(gameState, GameStateEnum.GameStarted, "play cards");
            }

            this.ValidateTurn(playerId, currentTurnPlayerId);
            this.ValidateActionLimit(playerId, noOfActionPlayed);
        }

        public void ValidateTurn(string userId, string currentUserIdTurn)
        {
            if (userId != currentUserIdTurn)
            {
                throw new NotPlayerTurnException(userId, currentUserIdTurn);
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

        public void ValidateCommandPileCardType(Card cardRemoved)
        {
            if (cardRemoved is not CommandCard && cardRemoved is not SystemWildCard && cardRemoved is not TributeCard)
            {
                throw new InvalidOperationException($"Cannot play a {cardRemoved.CardType} card on the command section. Only comamnd cards are allowed.");
            }

        }

        public void ValidateMoneyPileCardType(Card cardRemoved)
        {
            if (cardRemoved is not CommandCard && cardRemoved is not TributeCard && cardRemoved is not MoneyCard)
            {
                throw new InvalidOperationException($"Cannot play a {cardRemoved.CardType} card on the money section. Only money cards are allowed.");
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

        #region Card Specific Rule Validation

        public void ValidateHostileTakeoverTarget(List<Card> targetPlayerTableHand, PropertyCardColoursEnum targetColor)
        {
            this.ValidatePropertySetCompletion(targetPlayerTableHand, targetColor, shouldBeComplete: true, ActionTypes.HostileTakeover);
        }

        public void ValidatePirateRaidTarget(List<Card> targetPlayerTableHand, PropertyCardColoursEnum targetColor)
        {
            this.ValidatePropertySetCompletion(targetPlayerTableHand, targetColor, shouldBeComplete: false, ActionTypes.PirateRaid);
        }

        public void ValidateForcedTradeTarget(List<Card> targetPlayerTableHand, PropertyCardColoursEnum targetColor)
        {
            this.ValidatePropertySetCompletion(targetPlayerTableHand, targetColor, shouldBeComplete: false, ActionTypes.ForcedTrade);
        }

        public void ValidateSpaceStationPlacement(List<Card> playerTableHand, PropertyCardColoursEnum targetColor)
        {
            this.ValidatePropertySetCompletion(playerTableHand, targetColor, shouldBeComplete: true, ActionTypes.SpaceStation);
            this.ValidateNoDuplicateCard(playerTableHand, targetColor, ActionTypes.SpaceStation);
        }

        public void ValidateStarbasePlacement(List<Card> playerTableHand, PropertyCardColoursEnum targetColor)
        {
            this.ValidatePropertySetCompletion(playerTableHand, targetColor, shouldBeComplete: true, ActionTypes.Starbase);
            this.ValidateHasCardPresent(playerTableHand, targetColor, ActionTypes.Starbase);
            this.ValidateNoDuplicateCard(playerTableHand, targetColor, ActionTypes.Starbase);
        }

        public void ValidateTradeEmbargoTarget(List<Card> targetPlayerTableHand, PropertyCardColoursEnum targetColor)
        {
            this.ValidatePropertySetExists(targetPlayerTableHand, targetColor, ActionTypes.TradeEmbargo);
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
        public void ValidateTributeCardTarget(PropertyCardColoursEnum targetColor, Card cardToValidate)
        {
            if (cardToValidate is not TributeCard)
            {
                throw new InvalidOperationException($"{cardToValidate} is not a tribute card!");
            }

            if (cardToValidate is TributeCard tributeCard)
            {
                var result = tributeCard.TargetColorsToApplyRent.Any(color => color == targetColor);
                if (!result)
                {
                    throw new InvalidOperationException($"Target color selected it not a available color on the tribute card!");
                }
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

            if (tableHand.Count >= 11)
            {
                return true;
            }

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
            var propertyGroup = this.GetPropertyGroup(tableHand, color);
            if (propertyGroup == null) return false;

            var maxCards = propertyGroup.groupedPropertyCards.First().MaxCards;
            return propertyGroup.groupedPropertyCards.Count >= maxCards;
        }

        public bool IsCardSystemWildCard(Card targetCard)
        {
            if (targetCard is SystemWildCard)
            {
                return true;
            }
            return false;
        }

        #endregion

        #region Calculations

        public int CalculateRentAmount(PropertyCardColoursEnum targetColor, List<Card> playerPropertyCards)
        {
            var representativeCard = playerPropertyCards
                .OfType<StandardSystemCard>()
                .FirstOrDefault(sc => sc.CardColoursEnum == targetColor);

            if (representativeCard == null)
            {
                return 0;
            }

            int propertyCount = playerPropertyCards.Count(card =>
                (card is StandardSystemCard sc && sc.CardColoursEnum == targetColor) ||
                (card is SystemWildCard)
            );

            if (propertyCount == 0)
            {
                return 0;
            }

            // Use Math.Min to prevent an index out-of-bounds error if they have more properties than rent tiers.
            int rentalIndex = Math.Min(propertyCount - 1, representativeCard.RentalValues.Count - 1);
            int baseRent = representativeCard.RentalValues[rentalIndex];
            int additionalRent = 0;

            const int starbaseRentValue = 3;
            const int spaceStationRentValue = 4;

            int starbaseCount = playerPropertyCards
                .OfType<CommandCard>()
                .Count(cc => cc.Command == ActionTypes.Starbase);

            int spaceStationCount = playerPropertyCards
                .OfType<CommandCard>()
                .Count(cc => cc.Command == ActionTypes.SpaceStation);

            additionalRent = (starbaseCount >= 1 ? starbaseRentValue : 0) + (spaceStationCount >= 1 ? spaceStationRentValue : 0);

            // Return the total rent.
            return baseRent + additionalRent;
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
                DialogTypeEnum.OwnHandSelection or
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



        private void ValidateActionLimit(string userId, int noOfActionsPlayed)
        {
            if (noOfActionsPlayed >= 3)
            {
                throw new PlayerExceedingActionLimitException(userId);
            }
        }

        #endregion

        #region Property Set Validation Helpers

        private void ValidatePropertySetExists(List<Card> propertySet, PropertyCardColoursEnum targetColor, ActionTypes actionType)
        {
            if (propertySet == null || propertySet.Count == 0)
            {
                throw new InvalidTargetException(actionType.ToString(), targetColor, "target", "player doesn't own any properties of this color");
            }
        }

        private StandardSystemCard GetPropertySetSystemCard(List<Card> propertySet, PropertyCardColoursEnum targetColor)
        {
            var systemCard = propertySet.FirstOrDefault(card => card is StandardSystemCard) as StandardSystemCard;

            if (systemCard == null)
            {
                throw new InvalidOperationException($"No valid property cards found in the {targetColor} property set.");
            }

            return systemCard;
        }

        private void ValidatePropertySetCompletion(List<Card> propertySet, PropertyCardColoursEnum targetColor, bool shouldBeComplete, ActionTypes actionType)
        {
            this.ValidatePropertySetExists(propertySet, targetColor, actionType);

            var systemCard = this.GetPropertySetSystemCard(propertySet, targetColor);
            var maxCards = systemCard.MaxCards;
            var currentCount = propertySet.Count;
            var isComplete = currentCount >= maxCards;

            if (shouldBeComplete && !isComplete)
            {
                throw new InvalidOperationException($"{actionType} can only be used on completed property sets. The {targetColor} set has {currentCount}/{maxCards} properties.");
            }
            else if (!shouldBeComplete && isComplete)
            {
                switch (actionType)
                {
                    case ActionTypes.PirateRaid:
                        throw new CompletePropertySetException(targetColor);
                    case ActionTypes.ForcedTrade:
                        throw new InvalidOperationException($"Forced Trade cannot target completed property sets. The {targetColor} set is already complete and protected.");
                    default:
                        throw new InvalidOperationException($"{actionType} cannot target completed property sets. The {targetColor} set is already complete.");
                }
            }
        }

        private void ValidateHasCardPresent(List<Card> propertySet, PropertyCardColoursEnum targetColor, ActionTypes actionType)
        {
            if (!propertySet.Exists(card => card is CommandCard command && command.Command == actionType))
            {
                // A duplicate command card was found. Handle the error.
                throw new InvalidOperationException($"Cannot place card without {actionType} present first!");
            }
        }

        private void ValidateNoDuplicateCard(List<Card> propertySet, PropertyCardColoursEnum targetColor, ActionTypes actionType)
        {
            if (propertySet.Exists(card => card is CommandCard command && command.Command == actionType))
            {
                // A duplicate command card was found. Handle the error.
                throw new InvalidOperationException($"A duplicate card with action type '{actionType}' already exists in the set.");
            }
        }

        #endregion
    }
}