
using property_dealer_API.Application.Enums;
using property_dealer_API.Application.Exceptions;
using property_dealer_API.Core.Entities;
using property_dealer_API.Models.Cards;
using property_dealer_API.Models.Enums;
using property_dealer_API.Models.Enums.Cards;

namespace property_dealer_API.Core.Logic.GameRuleManager
{
    // Stateless class which only has the job of validating rules.
    public class GameRuleManager
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

        public DialogTypeEnum? IdentifyDialogToOpen(Card cardPlayed, PendingAction pendingAction)
        {
            if (cardPlayed is CommandCard commandCard)
            {
                pendingAction.ActionType = commandCard.Command;
                switch (commandCard.Command)
                {
                    case ActionTypes.HostileTakeover:
                    case ActionTypes.PirateRaid:
                    case ActionTypes.ForcedTrade:
                    case ActionTypes.BountyHunter:
                        return DialogTypeEnum.PlayerSelection;

                    // Cannot use shields up without any attack
                    case ActionTypes.ExploreNewSector:
                    case ActionTypes.ShieldsUp:
                        return null;

                    case ActionTypes.TradeDividend:
                        return DialogTypeEnum.PayValue;

                    case ActionTypes.TradeEmbargo:
                    case ActionTypes.SpaceStation:
                    case ActionTypes.Starbase:
                        return DialogTypeEnum.PropertySetSelection;
                }
            }
            else if (cardPlayed is TributeCard)
            {
                return DialogTypeEnum.PropertySetSelection;
            }
            return null;
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


    }
}
