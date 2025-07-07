using property_dealer_API.Application.MethodReturns;
using property_dealer_API.Core.Entities;
using property_dealer_API.Models.Cards;
using property_dealer_API.Models.Enums;
using System.Diagnostics.CodeAnalysis;
using property_dealer_API.Application.Enums;
using property_dealer_API.Core.Logic.DeckManager;
using property_dealer_API.Core.Logic.PlayerManager;
using property_dealer_API.Core.Logic.PlayerHandsManager;
using property_dealer_API.Application.Exceptions;
using property_dealer_API.Application.DTOs.Responses;
using property_dealer_API.Models.Enums.Cards;
using property_dealer_API.Core.Logic.GameStateMapper;

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


        public required string RoomId { get; set; }
        public required string RoomName { get; set; }
        public required GameStateEnum GameState { get; set; }
        public required GameConfig Config { get; set; }

        [SetsRequiredMembers]
        public GameDetails(string roomId, string roomName, GameConfig config, Player initialPlayer)
        {
            RoomId = roomId;
            RoomName = roomName;
            GameState = GameStateEnum.WaitingRoom;
            Config = config;
            this._deckManager = new DeckManager();
            this._playerManager = new PlayerManager(initialPlayer);
            this._playerHandManager = new PlayersHandManager();
            this._mapper = new GameStateMapper(PublicPlayerHandManager, PublicPlayerManager);
        }

        // Adding players, validating game rules for player to join will be done here
        public JoinGameResponseEnum AddPlayer(Player player)
        {
            // Game started
            if (GameState != GameStateEnum.WaitingRoom)
            {
                return JoinGameResponseEnum.AlreadyInGame;
            }

            // Game full
            if (this._playerManager.GetAllPlayers().Count + 1 > Convert.ToInt32(this.Config.MaxNumPlayers))
            {
                return JoinGameResponseEnum.GameFull;
            }

            var result = this._playerManager.AddPlayerToDict(player);

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

        public void PlayTurn(string userId, string cardId, CardDestinationEnum cardDestination, PropertyCardColoursEnum? cardColoursDestinationEnum)
        {
            //TODO validate rules here
            var cardRemoved = this._playerHandManager.RemoveFromPlayerHand(userId, cardId);
            try
            {
                switch (cardDestination)
                {
                    case CardDestinationEnum.ActionPile:
                        Console.WriteLine("Adding to discard pile");
                        this._deckManager.Discard(cardRemoved);
                        break;
                    case CardDestinationEnum.MoneyPile:
                        this._playerHandManager.AddCardToPlayerMoneyHand(userId, cardRemoved);
                        break;
                    case CardDestinationEnum.PropertyPile:
                        if (cardRemoved is not (StandardSystemCard or SystemWildCard))
                        {
                            throw new InvalidOperationException($"Cannot play a non property card on the property section");
                        }
                        if (cardColoursDestinationEnum == null)
                        {
                            throw new InvalidOperationException($"Card destination color cannot be null");
                        }

                        this._playerHandManager.AddCardToPlayerTableHand(userId, cardRemoved, cardColoursDestinationEnum.Value);

                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(cardDestination), "An invalid card destination was specified.");
                }

                // Drawing card and assigning
                var drawnCards = this._deckManager.DrawCard(1);

                if (drawnCards.Any())
                {
                    var cardDrawn = drawnCards.Single();
                    this._playerHandManager.AddCardToPlayerHand(userId, cardDrawn);
                }
                else
                {
                    Console.WriteLine($"Deck is empty. Player {userId} did not draw a card.");
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

        public List<TableHands> GetAllPlayerHands()
        {
            var allPlayerHands = this._mapper.GetAllTableHandsDto();
            return allPlayerHands;
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

        private void AssignHands()
        {
            Console.WriteLine("ASSIGNING CARDS TO ALL PLAYERS");

            var playerList = this._playerManager.GetAllPlayers();
            foreach (var player in playerList)
            {
                var freshCards = this._deckManager.DrawCard(5);
                this._playerHandManager.AssignPlayerHand(player.UserId, freshCards);
            }
        }
    }
}
