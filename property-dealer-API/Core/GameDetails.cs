using property_dealer_API.Application.Consts;
using property_dealer_API.Application.DTOs.Responses;
using property_dealer_API.Application.Enums;
using property_dealer_API.Application.MethodReturns;
using property_dealer_API.Core.Entities;
using property_dealer_API.Core.Logic.DeckManager;
using property_dealer_API.Core.Logic.GameRuleManager;
using property_dealer_API.Core.Logic.GameStateMapper;
using property_dealer_API.Core.Logic.PendingActionManager;
using property_dealer_API.Core.Logic.PlayerHandsManager;
using property_dealer_API.Core.Logic.PlayerManager;
using property_dealer_API.Core.Logic.TurnManager;
using property_dealer_API.Models.Cards;
using property_dealer_API.Models.Enums;
using property_dealer_API.Models.Enums.Cards;
using System.Diagnostics.CodeAnalysis;

namespace property_dealer_API.Core
{
    public class GameDetails
    {
        // Deck manager and it's readonly counterpart (for public access to gets, etc)
        private readonly DeckManager _deckManager;
        public IReadOnlyDeckManager PublicDeckManager => _deckManager;

        // Player manager and it's readonly counterpart (for public access to gets, etc)
        private readonly PlayerManager _playerManager;
        public IReadOnlyPlayerManager PublicPlayerManager => _playerManager;

        // Player hand manager and it's readonly counterpart (for public access to gets, etc)
        private readonly PlayersHandManager _playerHandManager;
        public IReadOnlyPlayerHandManager PublicPlayerHandManager => _playerHandManager;

        private readonly GameStateMapper _mapper;
        private readonly GameRuleManager _rulesManager;
        private readonly TurnManager _turnManager;
        private readonly PendingActionManager _pendingActionManager;


        public required string RoomId { get; set; }
        public required string RoomName { get; set; }
        public required GameStateEnum GameState { get; set; }
        public required GameConfig Config { get; set; }

        [SetsRequiredMembers]
        public GameDetails(string roomId, string roomName, GameConfig config)
        {
            RoomId = roomId;
            RoomName = roomName;
            GameState = GameStateEnum.WaitingRoom;
            Config = config;
            this._deckManager = new DeckManager();
            this._playerManager = new PlayerManager();
            this._playerHandManager = new PlayersHandManager();
            this._mapper = new GameStateMapper(PublicPlayerHandManager, PublicPlayerManager);
            this._rulesManager = new GameRuleManager();
            this._turnManager = new TurnManager(roomId);
            this._pendingActionManager = new PendingActionManager();
        }

        // Adding players, validating game rules for player to join will be done here
        public JoinGameResponseEnum AddPlayer(Player player)
        {
            var hasIssue = this._rulesManager.ValidatePlayerJoining(this.GameState, this._playerManager.GetAllPlayers(), this.Config.MaxNumPlayers);

            if (hasIssue.HasValue)
            {
                return hasIssue.Value;
            }

            var result = this._playerManager.AddPlayerToDict(player);
            this._turnManager.AddPlayer(player.UserId);
            return result;
        }

        public RemovePlayerReturn RemovePlayerByUserId(string userId)
        {
            var playerName = _playerManager.RemovePlayerFromDictByUserId(userId);   // Removal from player list
            _playerHandManager.RemovePlayerByUserId(userId);                        // Removal from player hand lists

            // Successful removal with no players remaining
            if (this._playerManager.CountPlayers() < 1)
            {
                return new RemovePlayerReturn(playerName, RemovePlayerResponse.NoPlayersRemaining);
            }

            // Successful removal with players remaining
            return new RemovePlayerReturn(playerName, RemovePlayerResponse.SuccessfulPlayerRemovalWithPlayersRemaining);
        }

        public void StartGame(List<Card> initialDeck)
        {
            Console.WriteLine("CALLING START GAME");
            this._deckManager.PopulateInitialDeck(initialDeck);     // Populating initial decks
            this.InitializePlayerHands();                           // Instantiate blank hand and table hand
            this.AssignHands();                                     // Assigning hand to blank hands

            this.GameState = GameStateEnum.GameStarted;
        }

        public GameConfig? GetGameRoomConfig()
        {
            return this.Config;
        }

        // Return a tuple for flexibility of adjusting later.
        // Currently the only return type would be a dialog. If the rules manager gives a dialog we return that
        // Otherwise return null.
        public (List<Player> dialogTargetList, DialogTypeEnum dialogToOpen)? PlayTurn(string userId, string cardId, CardDestinationEnum cardDestination, PropertyCardColoursEnum? cardColoursDestinationEnum)
        {
            DialogTypeEnum? dialogToOpen = null;
            List<Player>? dialogTargetList = null;

            //Getting players and current user to be used in rules validation
            var players = this._playerManager.GetAllPlayers();
            var currentUser = this._playerManager.GetPlayerByUserId(userId);

            // Validating player turn and if they exceed their turn amount
            this._rulesManager.ValidateTurn(userId, this._turnManager.GetCurrentUserTurn());
            this._rulesManager.ValidateActionLimit(userId, this._turnManager.GetCurrentUserActionCount());

            var cardRemoved = this._playerHandManager.RemoveFromPlayerHand(userId, cardId);
            try
            {
                switch (cardDestination)
                {
                    case CardDestinationEnum.CommandPile:
                        // Get dialog to open
                        var newPendingAction = new PendingAction { InitiatorUserId = userId };
                        dialogToOpen = this._rulesManager.IdentifyDialogToOpen(cardRemoved, newPendingAction);

                        // If dialog is not null
                        if (dialogToOpen.HasValue)
                        {
                            dialogTargetList = this._rulesManager.IdentifyWhoSeesDialog(currentUser, null, players, dialogToOpen.Value);
                            newPendingAction.StoredData[StoredDataKeys.TargetPlayers] = dialogTargetList;
                        }

                        Console.WriteLine("Adding to discard pile");
                        this._deckManager.Discard(cardRemoved);
                        break;

                    case CardDestinationEnum.MoneyPile:
                        this._playerHandManager.AddCardToPlayerMoneyHand(userId, cardRemoved);
                        break;

                    case CardDestinationEnum.PropertyPile:
                        this._rulesManager.ValidatePropertyPileCardType(cardRemoved);

                        if (cardRemoved is SystemWildCard)
                        {
                            dialogToOpen = DialogTypeEnum.WildcardColor;
                            dialogTargetList = this._rulesManager.IdentifyWhoSeesDialog(currentUser, null, players, dialogToOpen.Value);
                        }

                        // If we're still here, it must be a standard property card.
                        var validatedColor = this._rulesManager.ValidateStandardPropertyCardDestination(cardColoursDestinationEnum);
                        this._playerHandManager.AddCardToPlayerTableHand(userId, cardRemoved, validatedColor);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(cardDestination), "An invalid card destination was specified.");
                }

                // If there is a dialog to open, dont change player turns yet.
                // They need to do the action before changing player turn.
                if (dialogToOpen == null)
                {
                    Console.WriteLine("INCREMENTING USER ACTION COUNT");
                    var nullOrNextUserTurn = this._turnManager.IncrementUserActionCount();
                    if (nullOrNextUserTurn != null)
                    {
                        this.NextPlayerTurn(nullOrNextUserTurn);
                    }
                    return null;
                }
                else
                {
                    if (dialogTargetList == null)
                    {
                        throw new InvalidOperationException("Target list in null but dialog to open has a value!");
                    }

                    return (dialogTargetList, dialogToOpen.Value);
                }
            }

            catch (Exception)
            {
                // If ANY part of the turn fails (e.g., invalid card, empty deck error),
                // we give the card back to the player. This prevents the card from
                // disappearing from the game and keeps the state consistent.
                this._playerHandManager.AddCardToPlayerHand(userId, cardRemoved);

                // Re-throw the original exception so the calling layer knows what went wrong
                // and can handle it (e.g., display an error message to the user).
                throw;
            }
        }

        public void NextPlayerTurn(string userId)
        {
            // Draw Cards for new user
            this.AssignCardToPlayer(userId, 2);
        }

        public List<TableHands> GetAllPlayerHands()
        {
            var allPlayerHands = this._mapper.GetAllTableHandsDto();
            return allPlayerHands;
        }

        public CardDto GetPlayerHandByCardId(string userId, string cardId)
        {
            return this._playerHandManager.GetCardFromPlayerHandById(userId, cardId).ToDto();
        }

        public CardDto GetMostRecentDiscardedCard()
        {
            return this._deckManager.GetMostRecentDiscardedCard().ToDto();
        }

        // This method gets the players list and initializes the hands from the draw cards function in deck manager.
        private void InitializePlayerHands()
        {
            Console.WriteLine("INITIALIZING ALL PLAYERS HANDS");

            var playerList = this._playerManager.GetAllPlayers();
            foreach (var player in playerList)
            {
                this._playerHandManager.AddPlayerHand(player.UserId);
            }
        }

        // Adding cards to all player hands.
        private void AssignHands()
        {
            Console.WriteLine("ASSIGNING CARDS TO ALL PLAYERS");

            var playerList = this._playerManager.GetAllPlayers();

            for (int i = 0; i < playerList.Count; i++)
            {
                if (i == 0)
                {
                    this.AssignCardToPlayer(playerList[i].UserId, 7);
                }
                else
                {
                    this.AssignCardToPlayer(playerList[i].UserId, 5);
                }
            }
        }

        // Method to assign cards to specific players (next turn or pass go, etc)
        private void AssignCardToPlayer(string userId, int numCardsToDraw)
        {
            var freshCards = this._deckManager.DrawCard(numCardsToDraw);
            this._playerHandManager.AssignPlayerHand(userId, freshCards);
        }
    }
}
