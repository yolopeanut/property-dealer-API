using System.Collections.Concurrent;
using property_dealer_API.Application.DTOs.Responses;
using property_dealer_API.Application.Enums;
using property_dealer_API.Application.Exceptions;
using property_dealer_API.Application.MethodReturns;
using property_dealer_API.Core.Entities;
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
using property_dealer_API.Core.Logic.ActionExecution;

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
            DialogTypeEnum? dialogToOpen = null;
            List<Player>? dialogTargetList = null;
            PendingAction? newPendingAction = null;

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
                        newPendingAction = new PendingAction { InitiatorUserId = userId };
                        dialogToOpen = this._rulesManager.IdentifyDialogToOpen(foundCardId, newPendingAction); // Sets pending action type

                        // If dialog is not null
                        if (dialogToOpen.HasValue)
                        {
                            dialogTargetList = this._rulesManager.IdentifyWhoSeesDialog(currentUser, null, players, dialogToOpen.Value);

                            var playerHand = this._playerHandManager.GetPlayerHand(userId);
                            newPendingAction.RequiredResponders = [.. dialogTargetList]; // Setting the target list as a concurrent bag
                        }
                        else
                        {
                            if(foundCardId is CommandCard commandCard)
                            {
                                switch (commandCard.Command)
                                {
                                    case ActionTypes.ExploreNewSector:
                                        this.AssignCardToPlayer(userId, 2);
                                        break;
                                }
                            }
                        }

                            this._deckManager.Discard(foundCardId);
                        break;

                    case CardDestinationEnum.MoneyPile:
                        this._playerHandManager.AddCardToPlayerMoneyHand(userId, foundCardId);
                        break;

                    case CardDestinationEnum.PropertyPile:
                        this._rulesManager.ValidatePropertyPileCardType(foundCardId);

                        if (foundCardId is SystemWildCard)
                        {
                            newPendingAction = new PendingAction { InitiatorUserId = userId };
                            dialogToOpen = DialogTypeEnum.WildcardColor;
                            dialogTargetList = this._rulesManager.IdentifyWhoSeesDialog(currentUser, null, players, dialogToOpen.Value);
                            newPendingAction.RequiredResponders = [.. dialogTargetList]; // Setting the target list as a concurrent bag
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

                // If there is a dialog to open, dont change player turns yet.
                // They need to do the action before changing player turn.
                if (dialogToOpen == null)
                {
                    this.HandleRemoveFromHand(userId, cardId);

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

                    var actionContext = new ActionContext
                    {
                        CardId = cardId,
                        ActionInitiatingPlayerId = userId,
                        DialogTargetList = dialogTargetList,
                        DialogToOpen = dialogToOpen.Value,
                    };

                    if (newPendingAction == null)
                    {
                        throw new PendingActionNotFoundException(actionContext);
                    }
                    this._pendingActionManager.CurrPendingAction = newPendingAction;

                    return actionContext;
                }
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

        public ActionContext? RegisterActionResponse(string userId, ActionContext actionContext)
        {
            var player = this._playerManager.GetPlayerByUserId(userId);
            var shouldProcess = this._pendingActionManager.AddResponseToQueue(player, actionContext);

            if (shouldProcess)
            {
                this.ProcessPendingAction(actionContext);
            }
            return null;
        }

        //public ActionContext? RegisterActionResponse(string userId, ActionContext actionContext)
        //{
        //    // 1. ammend new dialog for them to see if certain condition
        //    // Select Player -> Table hand selector/pay value/property set selector -> shields up


        //    // 2. if no new dialog, process and reflect. 
        //    // Wildcard
        //    if (actionContext.DialogToOpen == DialogTypeEnum.WildcardColor)
        //    {
        //        if (actionContext.TargetSetColor == null)
        //        {
        //            throw new InvalidOperationException("Wildcard target color is not found in response!");
        //        }

        //        var cardRemoved = this.HandleRemoveFromHand(actionContext.ActionInitiatingPlayerId, actionContext.CardId);
        //        this._playerHandManager.AddCardToPlayerTableHand(userId, cardRemoved, actionContext.TargetSetColor.Value);
        //    }

        //    return null;
        //}

        public void NextPlayerTurn(string userId)
        {
            // Draw Cards for new user
            this.AssignCardToPlayer(userId, 2);
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
        private void ProcessPendingAction(ActionContext actionContext)
        {
            var pendingAction = this._pendingActionManager.CurrPendingAction;
            var allResponses = new List<(Player Player, ActionContext Context)>();

            // Drain the entire queue first
            while (pendingAction.ResponseQueue.TryDequeue(out var response))
            {
                allResponses.Add(response);
            }

            foreach (var (player, context) in allResponses)
            {
                // For the scenario that the user does shields up (can come from pays value-button or shields up dialog)
                if (context.DialogResponse == CommandResponseEnum.ShieldsUp)
                {
                    this._actionExecutionManager.HandleShieldsUpResponse(player, context);
                    continue;
                }

                // Handle all the responses
                switch (context.DialogToOpen)
                {
                    case DialogTypeEnum.PayValue:
                        //This can only have ok or shields up, no further dialog
                        this._actionExecutionManager.HandlePayValueResponse(player, context);
                        break;
                    case DialogTypeEnum.PlayerSelection:
                        this._actionExecutionManager.HandlePlayerSelectionResponse(player, context);
                        break;
                    case DialogTypeEnum.PropertySetSelection:
                        this._actionExecutionManager.HandlePropertySetSelectionResponse(player, context);
                        break;
                    case DialogTypeEnum.TableHandSelector:
                        this._actionExecutionManager.HandleTableHandSelectorResponse(player, context);
                        // prompt shields up next dialog
                        break;
                    case DialogTypeEnum.WildcardColor:
                        // just do processing on wildcard color
                        this._actionExecutionManager.HandleWildCardResponse(player, context);
                        break;
                }
            }

            // Clear pending action after finishing processing all responses
            if (this._pendingActionManager.CanClearPendingAction)
            {
                this._pendingActionManager.ClearPendingAction();
                this._turnManager.IncrementUserActionCount();
                this._playerHandManager.RemoveFromPlayerHand(actionContext.ActionInitiatingPlayerId, actionContext.CardId);
            }
        }


    }
}
