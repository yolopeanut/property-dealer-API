using property_dealer_API.Core.Entities;
using property_dealer_API.Core.Logic;
using property_dealer_API.Models.Cards;
using property_dealer_API.Models.Enums;
using System.Diagnostics.CodeAnalysis;

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

        // ============================================== END PLAYERS WRAPPERS ============================================== //


        // ================================================= CARD WRAPPERS ================================================== //

        // =============================================== END CARD WRAPPERS ================================================ //
    }
}
