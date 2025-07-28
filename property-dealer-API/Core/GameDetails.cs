using property_dealer_API.Application.DTOs.Responses;
using property_dealer_API.Application.Enums;
using property_dealer_API.Application.Exceptions;
using property_dealer_API.Application.MethodReturns;
using property_dealer_API.Core.Entities;
using property_dealer_API.Core.Logic.ActionExecution;
using property_dealer_API.Core.Logic.DebuggingManager;
using property_dealer_API.Core.Logic.DecksManager;
using property_dealer_API.Core.Logic.GameRulesManager;
using property_dealer_API.Core.Logic.GameStateMapper;
using property_dealer_API.Core.Logic.PendingActionsManager;
using property_dealer_API.Core.Logic.PlayerHandsManager;
using property_dealer_API.Core.Logic.PlayersManager;
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
        private readonly ActionExecutionManager _actionExecutionManager;
        private readonly DebugManager _debugManager;


        public required string RoomId { get; set; }
        public required string RoomName { get; set; }
        public required GameStateEnum GameState { get; set; }
        public required GameConfig Config { get; set; }

        [SetsRequiredMembers]
        public GameDetails(string roomId, string roomName, GameConfig config)
        {
            this.RoomId = roomId;
            this.RoomName = roomName;
            this.GameState = GameStateEnum.WaitingRoom;
            this.Config = config;
            this._deckManager = new DeckManager();
            this._playerManager = new PlayerManager();
            this._playerHandManager = new PlayersHandManager();
            this._mapper = new GameStateMapper(PublicPlayerHandManager, PublicPlayerManager);
            this._rulesManager = new GameRuleManager();
            this._turnManager = new TurnManager(roomId);
            this._pendingActionManager = new PendingActionManager();
            this._debugManager = new DebugManager(
                           _playerHandManager,
                           _playerManager,
                           _rulesManager,
                           _pendingActionManager,
                           _deckManager);
            this._actionExecutionManager = new ActionExecutionManager(
                           _playerHandManager,
                           _playerManager,
                           _rulesManager,
                           _pendingActionManager,
                           _deckManager);
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

            // Only can remove player hand if game is started, else will throw error
            if (GameState == GameStateEnum.GameStarted)
            {
                _playerHandManager.RemovePlayerByUserId(userId);                        // Removal from player hand lists
            }

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
        public ActionContext? PlayTurn(string userId, string cardId, CardDestinationEnum cardDestination, PropertyCardColoursEnum? cardColoursDestinationEnum)
        {
            //Getting players and current user to be used in rules validation
            var players = this._playerManager.GetAllPlayers();
            var currentUser = this._playerManager.GetPlayerByUserId(userId);

            // Validating player turn and if they exceed their turn amount
            this._rulesManager.ValidateTurn(userId, this._turnManager.GetCurrentUserTurn());
            this._rulesManager.ValidateActionLimit(userId, this._turnManager.GetCurrentUserActionCount());

            var foundCardId = this._playerHandManager.GetCardFromPlayerHandById(userId, cardId);

            try
            {
                switch (cardDestination)
                {
                    case CardDestinationEnum.CommandPile:
                        // Get dialog to open
                        var actionContext = this._actionExecutionManager.ExecuteAction(userId, cardId, foundCardId, currentUser, players);

                        // If dialog is not null
                        if (actionContext != null)
                        {
                            // Action needs dialog - return context
                            return actionContext;
                        }
                        else
                        {
                            // Immediate action completed - continue with turn flow
                            this.HandleRemoveFromHand(userId, cardId);
                            var nullOrNextUserTurn = this._turnManager.IncrementUserActionCount();
                            if (nullOrNextUserTurn != null)
                            {
                                this.NextPlayerTurn(nullOrNextUserTurn);
                            }
                            return null;
                        }

                    case CardDestinationEnum.MoneyPile:
                        this._playerHandManager.AddCardToPlayerMoneyHand(userId, foundCardId);
                        break;

                    case CardDestinationEnum.PropertyPile:
                        this._rulesManager.ValidatePropertyPileCardType(foundCardId);

                        if (foundCardId is SystemWildCard)
                        {
                            return this._actionExecutionManager.ExecuteAction(userId, cardId, foundCardId, currentUser, players);
                        }
                        else
                        {
                            // If we're still here, it must be a standard property card.
                            var validatedColor = this._rulesManager.ValidateStandardPropertyCardDestination(cardColoursDestinationEnum);
                            this._playerHandManager.AddCardToPlayerTableHand(userId, foundCardId, validatedColor);
                        }
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(cardDestination), "An invalid card destination was specified.");
                }

                // Complete the turn for non-dialog actions (MoneyPile and standard PropertyPile)
                this.HandleRemoveFromHand(userId, cardId);
                this.CompleteTurn();
                return null;
            }

            catch (Exception)
            {
                // If ANY part of the turn fails (e.g., invalid card, empty deck error),
                // we give the card back to the player. This prevents the card from
                // disappearing from the game and keeps the state consistent.
                this._playerHandManager.AddCardToPlayerHand(userId, foundCardId);

                // Re-throw the original exception so the calling layer knows what went wrong
                // and can handle it (e.g., display an error message to the user).
                throw;
            }
        }

        public List<ActionContext>? RegisterActionResponse(string userId, ActionContext actionContext)
        {
            var player = this._playerManager.GetPlayerByUserId(userId);
            var shouldProcess = this._pendingActionManager.AddResponseToQueue(player, actionContext);

            if (shouldProcess)
            {
                return this.ProcessPendingAction(actionContext);
            }
            return null;
        }

        public void NextPlayerTurn(string userId)
        {
            // Draw Cards for new user
            this.AssignCardToPlayer(userId, 2);
        }

        private void CompleteTurn()
        {
            var nextUserTurn = this._turnManager.IncrementUserActionCount();
            if (nextUserTurn != null)
            {
                this.NextPlayerTurn(nextUserTurn);
            }
        }

        public Player GetCurrentPlayerTurn()
        {
            string currUserTurn = this._turnManager.GetCurrentUserTurn();
            return this._playerManager.GetPlayerByUserId(currUserTurn);
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

        private Card HandleRemoveFromHand(string userId, string cardId)
        {
            // Remove card and check if hand is empty, if it is, regerate all 5 cards for them.
            var cardRemoved = this._playerHandManager.RemoveFromPlayerHand(userId, cardId);
            this._deckManager.Discard(cardRemoved);
            var handIsEmpty = this._rulesManager.IsPlayerHandEmpty(this._playerHandManager.GetPlayerHand(userId));
            if (handIsEmpty)
            {
                this.AssignCardToPlayer(userId, 5);
            }

            return cardRemoved;
        }

        // Only responses from dialogs to navigate to next dialog or handle logic
        private List<ActionContext>? ProcessPendingAction(ActionContext actionContext)
        {
            var pendingAction = this._pendingActionManager.CurrPendingAction;
            var allResponses = new List<(Player Player, ActionContext Context)>();
            var newActionContexts = new List<ActionContext>();
            var cardWasRemoved = false;

            try
            {
                // Drain the entire queue first
                while (pendingAction.ResponseQueue.TryDequeue(out var response))
                {
                    allResponses.Add(response);
                }

                foreach (var (player, context) in allResponses)
                {
                    var newActionContext = actionContext.Clone();
                    // For the scenario that the user does shields up (can come from pays value-button or shields up dialog)
                    if (context.DialogResponse == CommandResponseEnum.ShieldsUp)
                    {
                        this._actionExecutionManager.HandleShieldsUpResponse(player, context);
                        continue;
                    }

                    // Handle all the responses
                    switch (context.DialogToOpen)
                    {
                        #region No new dialogs
                        case DialogTypeEnum.PayValue:
                            //This can only have ok or shields up, since shields up handled, no further dialog.
                            this._actionExecutionManager.HandlePayValueResponse(player, newActionContext);
                            break;
                        case DialogTypeEnum.WildcardColor:
                            // just do processing on wildcard color
                            this._actionExecutionManager.HandleWildCardResponse(player, newActionContext);
                            break;
                        #endregion

                        #region Might have new dialog
                        case DialogTypeEnum.PropertySetSelection:
                            this._actionExecutionManager.HandlePropertySetSelectionResponse(player, newActionContext);
                            newActionContexts.Add(newActionContext);
                            break;
                        case DialogTypeEnum.TableHandSelector:
                            this._actionExecutionManager.HandleTableHandSelectorResponse(player, newActionContext);
                            newActionContexts.Add(newActionContext);
                            // prompt shields up next dialog
                            break;
                        #endregion

                        #region Only new target dialogs
                        case DialogTypeEnum.PlayerSelection:
                            this._actionExecutionManager.HandlePlayerSelectionResponse(player, newActionContext);
                            newActionContexts.Add(newActionContext);
                            break;
                            #endregion
                    }
                }

                // Clear pending action after finishing processing all responses
                if (this._pendingActionManager.CanClearPendingAction)
                {
                    this._pendingActionManager.ClearPendingAction();
                    this._playerHandManager.RemoveFromPlayerHand(actionContext.ActionInitiatingPlayerId, actionContext.CardId);
                    this.CompleteTurn();
                    cardWasRemoved = true;
                }
                return newActionContexts;
            }
            catch (Exception)
            {
                // Only try to recover if we actually removed the card
                if (cardWasRemoved)
                {
                    try
                    {
                        var cardFound = this._deckManager.GetDiscardedCardById(actionContext.CardId);
                        this._playerHandManager.AddCardToPlayerHand(actionContext.ActionInitiatingPlayerId, cardFound);
                    }
                    catch (CardNotFoundException)
                    {
                        // Card wasn't discarded yet, that's fine - it's still in player's hand
                    }
                }
                throw;
            }
        }

        public void ExecuteDebugCommand(string userId, DebugOptionsEnum debugOption)
        {
            switch (debugOption)
            {
                case DebugOptionsEnum.SpawnCard:
                    this._debugManager.GiveAllCardsInDeck();
                    break;
            }
        }
    }
}
