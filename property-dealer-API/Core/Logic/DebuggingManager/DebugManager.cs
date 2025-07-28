
using property_dealer_API.Core.Logic.DecksManager;
using property_dealer_API.Core.Logic.GameRulesManager;
using property_dealer_API.Core.Logic.PendingActionsManager;
using property_dealer_API.Core.Logic.PlayerHandsManager;
using property_dealer_API.Core.Logic.PlayersManager;

namespace property_dealer_API.Core.Logic.DebuggingManager
{
    public class DebugManager
    {
        private readonly PlayersHandManager _playerHandManager;
        private readonly PlayerManager _playerManager;
        private readonly GameRuleManager _rulesManager;
        private readonly PendingActionManager _pendingActionManager;
        private readonly DeckManager _deckManager;

        public DebugManager(
            PlayersHandManager playerHandManager,
            PlayerManager playerManager,
            GameRuleManager rulesManager,
            PendingActionManager pendingActionManager,
            DeckManager deckManager)
        {
            this._playerHandManager = playerHandManager;
            this._playerManager = playerManager;
            this._rulesManager = rulesManager;
            this._pendingActionManager = pendingActionManager;
            this._deckManager = deckManager;
        }

        public void GiveAllCardsInDeck()
        {

        }
    }
}
