using property_dealer_API.Application.DTOs.Responses;
using property_dealer_API.Application.Enums;
using property_dealer_API.Application.MethodReturns;
using property_dealer_API.Core.Entities;
using property_dealer_API.Core.Logic.DebuggingManager;
using property_dealer_API.Core.Logic.DecksManager;
using property_dealer_API.Core.Logic.GameRulesManager;
using property_dealer_API.Core.Logic.GameStateMapper;
using property_dealer_API.Core.Logic.PlayerHandsManager;
using property_dealer_API.Core.Logic.PlayersManager;
using property_dealer_API.Core.Logic.TurnManager;
using property_dealer_API.Core.Logic.TurnExecutionsManager;
using property_dealer_API.Models.Cards;
using property_dealer_API.Models.Enums;
using property_dealer_API.Models.Enums.Cards;
using System.Diagnostics.CodeAnalysis;
using property_dealer_API.Core.Logic.DialogsManager;

namespace property_dealer_API.Core
{
    public class GameDetails
    {
        // Store full write interfaces internally
        private readonly IDeckManager _deckManager;
        private readonly IPlayerManager _playerManager;
        private readonly IPlayerHandManager _playerHandManager;

        // But expose readonly versions publicly
        public IReadOnlyDeckManager PublicDeckManager => _deckManager;
        public IReadOnlyPlayerManager PublicPlayerManager => _playerManager;
        public IReadOnlyPlayerHandManager PublicPlayerHandManager => _playerHandManager;

        private readonly IGameStateMapper _mapper;
        private readonly IGameRuleManager _rulesManager;
        private readonly ITurnManager _turnManager;
        private readonly IDebugManager _debugManager;
        private readonly ITurnExecutionManager _turnExecutionManager;
        private readonly IDialogManager _dialogManager;

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
            IDialogManager dialogManager)
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

        public ActionContext? PlayTurn(string userId, string cardId, CardDestinationEnum cardDestination, PropertyCardColoursEnum? cardColoursDestinationEnum)
        {
            // Validating player turn and if they exceed their turn amount
            this._rulesManager.ValidatePlayerCanPlayCard(this.GameState, userId, this._turnManager.GetCurrentUserTurn(), this._turnManager.GetCurrentUserActionCount());
            var cardInPlayerHand = _playerHandManager.GetCardFromPlayerHandById(userId, cardId);

            try
            {
                var actionContext = this._turnExecutionManager.ExecuteTurnAction(userId, cardInPlayerHand, cardDestination, cardColoursDestinationEnum);

                // Property pile or money pile
                if (actionContext == null)
                {
                    HandleRemoveFromHand(userId, cardId);
                    CompleteTurn();
                }
                return actionContext;
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

        public List<ActionContext>? RegisterActionResponse(string userId, ActionContext actionContext)
        {
            var player = this._playerManager.GetPlayerByUserId(userId);
            var dialogProcessingResult = this._dialogManager.RegisterActionResponse(player, actionContext);

            if (dialogProcessingResult.ShouldClearPendingAction)
            {
                this._playerHandManager.RemoveFromPlayerHand(actionContext.ActionInitiatingPlayerId, actionContext.CardId);
                this.CompleteTurn();
                return null;
            }

            return dialogProcessingResult.NewActionContexts;
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

        public void ExecuteDebugCommand(string userId, DebugOptionsEnum debugOption)
        {
            switch (debugOption)
            {
                case DebugOptionsEnum.SpawnCard:
                    this._debugManager.GiveAllCardsInDeck();
                    break;
            }
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
                        throw new InvalidOperationException("Player was not found when checking if player won!");
                    }

                    this.GameState = GameStateEnum.GameOver;
                    return player.Player;
                }
            }
            return null;
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
    }
}
