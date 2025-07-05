using property_dealer_API.Application.MethodReturns;
using property_dealer_API.Core.Entities;
using property_dealer_API.Core.Logic;
using property_dealer_API.Models.Cards;
using property_dealer_API.Models.Enums;
using System.Diagnostics.CodeAnalysis;
using property_dealer_API.Application.Enums;

namespace property_dealer_API.Core
{
    public class GameDetails
    {
        private readonly DeckManager _deckManager;
        private readonly PlayerManager _playerManager;

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
        }

        public void StartGame(List<Card> initialDeck)
        {
            this._deckManager.PopulateInitialDeck(initialDeck);
            this.InitializeHands();
            this.GameState = GameStateEnum.GameStarted;
        }

        public GameConfig? GetGameRoomConfig()
        {
            return this.Config;
        }


        // ================================================ PLAYERS WRAPPERS ================================================ //

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

        // Getting all players
        public List<Player> GetPlayers()
        {
            return this._playerManager.GetAllPlayers();
        }

        public Player GetPlayerByUserId(string userId)
        {
            return this._playerManager.GetPlayerByUserId(userId);
        }

        public RemovePlayerReturn RemovePlayerByUserId(string userId)
        {
            var playerName = _playerManager.RemovePlayerFromDictByUserId(userId);

            // Successful removal with no players remaining
            if (this._playerManager.CountPlayers() < 1)
            {
                return new RemovePlayerReturn(playerName, RemovePlayerResponse.NoPlayersRemaining);
            }

            // Successful removal with players remaining
            return new RemovePlayerReturn(playerName, RemovePlayerResponse.SuccessfulPlayerRemovalWithPlayersRemaining);


            ;
        }

        // ============================================== END PLAYERS WRAPPERS ============================================== //


        // ================================================= CARD WRAPPERS & LOGICS ================================================== //

        // =============================================== END CARD WRAPPERS & LOGICS ================================================ //

        // This method gets the players list and initializes the hands from the draw cards function in deck manager.
        private void InitializeHands()
        {
            var playerList = this._playerManager.GetAllPlayers();

            foreach (var player in playerList)
            {
                player.Hand = this._deckManager.DrawCard(7);
            }
        }
    }
}
