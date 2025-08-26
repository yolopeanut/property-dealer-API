using System.Diagnostics.CodeAnalysis;
using property_dealer_API.Application.DTOs.Responses;
using property_dealer_API.Application.Enums;
using property_dealer_API.Application.MethodReturns;
using property_dealer_API.Core.Entities;
using property_dealer_API.Core.Logic.DebuggingManager;
using property_dealer_API.Core.Logic.DecksManager;
using property_dealer_API.Core.Logic.DialogsManager;
using property_dealer_API.Core.Logic.GameRulesManager;
using property_dealer_API.Core.Logic.GameStateMapper;
using property_dealer_API.Core.Logic.PlayerHandsManager;
using property_dealer_API.Core.Logic.PlayersManager;
using property_dealer_API.Core.Logic.TurnExecutionsManager;
using property_dealer_API.Core.Logic.TurnManager;
using property_dealer_API.Models.Cards;
using property_dealer_API.Models.Enums;
using property_dealer_API.Models.Enums.Cards;

namespace property_dealer_API.Core
{
    public class GameDetails
    {
        // Store full write interfaces internally
        private readonly IDeckManager _deckManager;
        private readonly IPlayerManager _playerManager;
        private readonly IPlayerHandManager _playerHandManager;

        // But expose readonly versions publicly
        public IReadOnlyDeckManager PublicDeckManager => this._deckManager;
        public IReadOnlyPlayerManager PublicPlayerManager => this._playerManager;
        public IReadOnlyPlayerHandManager PublicPlayerHandManager => this._playerHandManager;

        private readonly IGameStateMapper _mapper;
        private readonly IGameRuleManager _rulesManager;
        private readonly ITurnManager _turnManager;
        private readonly IDebugManager _debugManager;
        private readonly ITurnExecutionManager _turnExecutionManager;
        private readonly IDialogManager _dialogManager;
        private readonly IServiceScope _scope;

        public required string RoomId { get; set; }
        public required string RoomName { get; set; }
        public required GameStateEnum GameState { get; set; }
        public required GameConfig Config { get; set; }

        [SetsRequiredMembers]
        public GameDetails(
            string roomId,
            string roomName,
            GameConfig config,
            IDeckManager deckManager,
            IPlayerManager playerManager,
            IPlayerHandManager playerHandManager,
            IGameStateMapper mapper,
            IGameRuleManager rulesManager,
            ITurnManager turnManager,
            IDebugManager debugManager,
            ITurnExecutionManager turnExecutionManager,
            IDialogManager dialogManager,
            IServiceScope scope
        )
        {
            this.RoomId = roomId;
            this.RoomName = roomName;
            this.GameState = GameStateEnum.WaitingRoom;
            this.Config = config;
            this._deckManager = deckManager;
            this._playerManager = playerManager;
            this._playerHandManager = playerHandManager;
            this._mapper = mapper;
            this._rulesManager = rulesManager;
            this._turnManager = turnManager;
            this._debugManager = debugManager;
            this._turnExecutionManager = turnExecutionManager;
            this._dialogManager = dialogManager;
            this._scope = scope;
        }

        // Adding players, validating game rules for player to join will be done here
        public JoinGameResponseEnum AddPlayer(Player player)
        {
            var hasIssue = this._rulesManager.ValidatePlayerJoining(
                this.GameState,
                this._playerManager.GetAllPlayers(),
                this.Config.MaxNumPlayers
            );

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
            var playerName = this._playerManager.RemovePlayerFromDictByUserId(userId); // Removal from player list

            // Only can remove player hand if game is started, else will throw error
            if (this.GameState == GameStateEnum.GameStarted)
            {
                this._playerHandManager.RemovePlayerByUserId(userId); // Removal from player hand lists
            }

            // Successful removal with no players remaining
            if (this._playerManager.CountPlayers() < 1)
            {
                return new RemovePlayerReturn(playerName, RemovePlayerResponse.NoPlayersRemaining);
            }

            // Successful removal with players remaining
            return new RemovePlayerReturn(
                playerName,
                RemovePlayerResponse.SuccessfulPlayerRemovalWithPlayersRemaining
            );
        }

        public List<Player> GetPlayers()
        {
            return this._playerManager.GetAllPlayers();
        }

        public void SetNewGamePlayers(List<Player> players)
        {
            foreach (var player in players)
            {
                this._playerManager.AddPlayerToDict(player);
            }
        }

        public void StartGame(List<Card> initialDeck)
        {
            this._deckManager.PopulateInitialDeck(initialDeck); // Populating initial decks
            this.InitializePlayerHands(); // Instantiate blank hand and table hand
            this.AssignHands(); // Assigning hand to blank hands

            var firstPlayer = this._turnManager.GetCurrentUserTurn();
            this.AssignCardToPlayer(firstPlayer, 2);
            this.GameState = GameStateEnum.GameStarted;
        }

        public GameConfig? GetGameRoomConfig()
        {
            return this.Config;
        }

        public TurnResult PlayTurn(
            string userId,
            string cardId,
            CardDestinationEnum cardDestination,
            PropertyCardColoursEnum? cardColoursDestinationEnum
        )
        {
            // Validating player turn and if they exceed their turn amount
            this._rulesManager.ValidatePlayerCanPlayCard(
                this.GameState,
                userId,
                this._turnManager.GetCurrentUserTurn(),
                this._turnManager.GetCurrentUserActionCount()
            );
            var cardInPlayerHand = this._playerHandManager.GetCardFromPlayerHandById(
                userId,
                cardId
            );
            var allPlayers = this._playerManager.GetAllPlayers();

            try
            {
                var actionContext = this._turnExecutionManager.ExecuteTurnAction(
                    userId,
                    cardInPlayerHand,
                    cardDestination,
                    cardColoursDestinationEnum
                );

                // Property pile or money pile
                if (actionContext == null)
                {
                    this.HandleRemoveFromHand(userId, cardId);
                    var turnResult = new TurnResult(null, null, null, null);
                    turnResult = this.CompleteTurn(userId, turnResult); // Null if no winning players found
                    return turnResult;
                }
                return new TurnResult(actionContext, null);
            }
            catch (Exception)
            {
                // If ANY part of the turn fails (e.g., invalid card, empty deck error),
                // we give the card back to the player. This prevents the card from
                // disappearing from the game and keeps the state consistent.
                this._turnExecutionManager.RecoverFromFailedTurn(userId, cardInPlayerHand);
                throw;
            }
        }

        public TurnResult RegisterActionResponse(string userId, ActionContext actionContext)
        {
            TurnResult turnResult;
            if (actionContext.DialogResponse == CommandResponseEnum.Cancel)
            {
                this._dialogManager.ClearPendingAction();
                return new TurnResult(); // return empty turn result because no turn was made
            }

            var player = this._playerManager.GetPlayerByUserId(userId);
            var allPlayers = this._playerManager.GetAllPlayers();

            var dialogProcessingResult = this._dialogManager.RegisterActionResponse(
                player,
                actionContext
            );

            turnResult = new TurnResult(null, null, null, dialogProcessingResult.ActionResult);

            if (dialogProcessingResult.ShouldClearPendingAction)
            {
                this._playerHandManager.RemoveFromPlayerHand(
                    actionContext.ActionInitiatingPlayerId,
                    actionContext.CardId
                );
                turnResult = this.CompleteTurn(actionContext.ActionInitiatingPlayerId, turnResult);

                return turnResult;
            }

            turnResult.ActionContext = dialogProcessingResult.NewActionContexts?.FirstOrDefault();
            return turnResult;
        }

        private void NextPlayerTurn(string userId)
        {
            // Draw Cards for new user
            this.AssignCardToPlayer(userId, 2);
        }

        private TurnResult CompleteTurn(string currPlayerId, TurnResult turnResult)
        {
            var newTurnResult = new TurnResult
            {
                ActionContext = turnResult.ActionContext,
                ActionResults = turnResult.ActionResults,
                NeedToRemoveCardPlayer = turnResult.NeedToRemoveCardPlayer,
                WinningPlayer = turnResult.WinningPlayer,
            };

            var currPlayerHand = this._playerHandManager.GetPlayerHand(currPlayerId);

            // Check for win condition before moving to next turn
            var winningPlayer = this.CheckIfAnyPlayersWon();
            if (winningPlayer != null)
            {
                // Game is over, don't proceed with next turn
                newTurnResult.WinningPlayer = winningPlayer;
                return newTurnResult;
            }

            var nextUserTurn = this._turnManager.IncrementUserActionCount();
            if (nextUserTurn != null)
            {
                try
                {
                    this._rulesManager.ValidateEndOfTurnCardLimit(currPlayerHand);
                }
                catch (Exception)
                {
                    var currPlayer = this._playerManager.GetPlayerByUserId(currPlayerId);
                    newTurnResult.NeedToRemoveCardPlayer = currPlayer;
                    return newTurnResult;
                }
                this.NextPlayerTurn(nextUserTurn);
            }

            return newTurnResult;
        }

        public (Player player, int numTurnsLeft) GetCurrentPlayerTurn()
        {
            string currUserTurn = this._turnManager.GetCurrentUserTurn();
            Player player = this._playerManager.GetPlayerByUserId(currUserTurn);
            int numTurnsLeft = this._turnManager.GetRemainingActionCounts();
            return (player, numTurnsLeft);
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

        public CardDto? GetMostRecentDiscardedCard()
        {
            return this._deckManager.GetMostRecentDiscardedCard()?.ToDto();
        }

        public void ExecuteDebugCommand(DebugOptionsEnum debugCommand, DebugContext debugContext)
        {
            this._debugManager.ProcessCommand(debugCommand, debugContext);
        }

        public Player? CheckIfAnyPlayersWon()
        {
            var playerTableHands = this.GetAllPlayerHands();

            foreach (var player in playerTableHands)
            {
                if (this._rulesManager.CheckIfPlayerWon(player.TableHand))
                {
                    if (player.Player == null)
                    {
                        throw new InvalidOperationException(
                            "Player was not found when checking if player won!"
                        );
                    }

                    this.GameState = GameStateEnum.GameOver;
                    this._scope.Dispose();
                    return player.Player;
                }
            }
            return null;
        }

        public TurnResult EndPlayerTurnEarlier(string userId)
        {
            if (this.GetCurrentPlayerTurn().player.UserId == userId)
            {
                try
                {
                    var currPlayerHand = this._playerHandManager.GetPlayerHand(userId);
                    this._rulesManager.ValidateEndOfTurnCardLimit(currPlayerHand);
                }
                catch (Exception)
                {
                    var currPlayer = this._playerManager.GetPlayerByUserId(userId);
                    return new TurnResult(null, null, currPlayer); // current player needs to dispose
                }
                var nextUserTurn = this._turnManager.PrematurelyEndCurrentUserTurn();
                this.NextPlayerTurn(nextUserTurn);
            }
            return new TurnResult(null, null, null);
        }

        public void DisposeExtraCards(string userId, List<string> cardIdsToDispose)
        {
            cardIdsToDispose.ForEach(cardId => this.HandleRemoveFromHand(userId, cardId));
            var nextUserTurn = this._turnManager.PrematurelyEndCurrentUserTurn();
            this.NextPlayerTurn(nextUserTurn);
        }

        public void MovePropertySetModifierBetweenSets(
            string userId,
            string selectedCardId,
            PropertyCardColoursEnum destinationColor
        )
        {
            var currentPlayerTurn = this._turnManager.GetCurrentUserTurn();
            this._rulesManager.ValidateTurn(userId, currentPlayerTurn);
            this._playerHandManager.MoveCardsBetweenTableHands(
                userId,
                selectedCardId,
                destinationColor
            );
        }

        public List<Player> GetPendingActionPlayers()
        {
            return this._dialogManager.GetPendingPlayers();
        }

        public TurnResult GetCurrentPendingAction()
        {
            var currActionContext = this._dialogManager.GetCurrentActionContext();

            return new TurnResult(currActionContext);
        }

        // This method gets the players list and initializes the hands from the draw cards function in deck manager.
        private void InitializePlayerHands()
        {
            var playerList = this._playerManager.GetAllPlayers();
            foreach (var player in playerList)
            {
                this._playerHandManager.AddPlayerHand(player.UserId);
                this.SetupStartDebugCommands(player);
            }
        }

        // Adding cards to all player hands.
        private void AssignHands()
        {
            var playerList = this._playerManager.GetAllPlayers();

            for (int i = 0; i < playerList.Count; i++)
            {
                this.AssignCardToPlayer(playerList[i].UserId, 5);
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
            var handIsEmpty = this._rulesManager.IsPlayerHandEmpty(
                this._playerHandManager.GetPlayerHand(userId)
            );
            if (handIsEmpty)
            {
                this.AssignCardToPlayer(userId, 5);
            }

            return cardRemoved;
        }

        private void SetupStartDebugCommands(Player player)
        {
            this._debugManager.ProcessCommand(
                DebugOptionsEnum.SpawnFullSet,
                new DebugContext { UserId = player.UserId }
            );
            this._debugManager.ProcessCommand(
                DebugOptionsEnum.ChangeHandLimit,
                new DebugContext { UserId = player.UserId, NewHandLimit = 999 }
            );
            this._debugManager.ProcessCommand(
                DebugOptionsEnum.SpawnAllCommandCard,
                new DebugContext { UserId = player.UserId }
            );
        }
    }
}
