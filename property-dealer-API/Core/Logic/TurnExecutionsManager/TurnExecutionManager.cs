using property_dealer_API.Application.Enums;
using property_dealer_API.Core.Logic.ActionExecution;
using property_dealer_API.Core.Logic.GameRulesManager;
using property_dealer_API.Core.Logic.PlayerHandsManager;
using property_dealer_API.Core.Logic.PlayersManager;
using property_dealer_API.Models.Cards;
using property_dealer_API.Models.Enums.Cards;

namespace property_dealer_API.Core.Logic.TurnExecutionsManager
{
    public class TurnExecutionManager : ITurnExecutionManager
    {
        private IPlayerHandManager _playerHandManager;
        private IPlayerManager _playerManager;
        private IGameRuleManager _rulesManager;
        private IActionExecutionManager _actionExecutionManager;

        public TurnExecutionManager(
            IPlayerHandManager playerHandManager,
            IPlayerManager playerManager,
            IGameRuleManager rulesManager,
            IActionExecutionManager actionExecutionManager
        )
        {
            this._playerHandManager = playerHandManager;
            this._playerManager = playerManager;
            this._rulesManager = rulesManager;
            this._actionExecutionManager = actionExecutionManager;
        }

        public ActionContext? ExecuteTurnAction(
            string userId,
            Card playerHandCard,
            CardDestinationEnum cardDestination,
            PropertyCardColoursEnum? colorDestination
        )
        {
            //Getting players and current user to be used in rules validation
            var players = this._playerManager.GetAllPlayers();
            var currentUser = this._playerManager.GetPlayerByUserId(userId);

            switch (cardDestination)
            {
                case CardDestinationEnum.CommandPile:
                    // Get dialog to open
                    this._rulesManager.ValidateCommandPileCardType(playerHandCard);
                    var actionContext = this._actionExecutionManager.ExecuteAction(
                        userId,
                        playerHandCard,
                        currentUser,
                        players
                    );
                    return actionContext;

                case CardDestinationEnum.MoneyPile:
                    this._rulesManager.ValidateMoneyPileCardType(playerHandCard);
                    this._playerHandManager.AddCardToPlayerMoneyHand(userId, playerHandCard);
                    break;

                case CardDestinationEnum.PropertyPile:
                    // Validate rules for property pile
                    this._rulesManager.ValidatePropertyPileCardType(playerHandCard);
                    var validatedColor = this._rulesManager.ValidateStandardPropertyCardDestination(
                        colorDestination
                    );
                    this._playerHandManager.AddCardToPlayerTableHand(
                        userId,
                        playerHandCard,
                        validatedColor
                    );
                    break;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(cardDestination),
                        "An invalid card destination was specified."
                    );
            }

            return null;
        }

        public void RecoverFromFailedTurn(string userId, Card card)
        {
            this._playerHandManager.AddCardToPlayerHand(userId, card);
        }
    }
}
