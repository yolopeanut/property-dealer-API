using property_dealer_API.Core.Logic.DecksManager;
using property_dealer_API.Core.Logic.GameRulesManager;
using property_dealer_API.Core.Logic.PendingActionsManager;
using property_dealer_API.Core.Logic.PlayerHandsManager;
using property_dealer_API.Core.Logic.PlayersManager;
using property_dealer_API.Models.Enums.Cards;

namespace property_dealer_API.Core.Logic.DebuggingManager
{
    public class DebugManager : IDebugManager
    {
        // Use FULL write interfaces for admin powers
        private readonly IPlayerHandManager _playerHandManager;
        private readonly IPlayerManager _playerManager;
        private readonly IGameRuleManager _rulesManager;
        private readonly IPendingActionManager _pendingActionManager;
        private readonly IDeckManager _deckManager;

        public DebugManager(
            IPlayerHandManager playerHandManager,
            IPlayerManager playerManager,
            IGameRuleManager rulesManager,
            IPendingActionManager pendingActionManager,
            IDeckManager deckManager)
        {
            this._playerHandManager = playerHandManager;
            this._playerManager = playerManager;
            this._rulesManager = rulesManager;
            this._pendingActionManager = pendingActionManager;
            this._deckManager = deckManager;
        }

        public void CompletePlayerPropertySet(string userId, PropertyCardColoursEnum color)
        {
            throw new NotImplementedException();
        }

        public void ForcePlayerWin(string userId)
        {
            throw new NotImplementedException();
        }

        public void GiveAllCardsInDeck()
        {
            // Now you can actually implement this!
            var allCards = this._deckManager.ViewAllCardsInDeck();
            var players = this._playerManager.GetAllPlayers();

            foreach (var player in players)
            {
                foreach (var card in allCards)
                {
                    this._playerHandManager.AddCardToPlayerHand(player.UserId, card);
                }
            }
        }

        public void GivePlayerCard(string userId, string cardType)
        {
            throw new NotImplementedException();
        }

        public void ResetGame()
        {
            throw new NotImplementedException();
        }

        public void SetPlayerHandSize(string userId, int size)
        {
            throw new NotImplementedException();
        }

        public void SetPlayerMoney(string userId, int amount)
        {
            throw new NotImplementedException();
        }

        public void SkipToNextPlayer()
        {
            throw new NotImplementedException();
        }
    }
}