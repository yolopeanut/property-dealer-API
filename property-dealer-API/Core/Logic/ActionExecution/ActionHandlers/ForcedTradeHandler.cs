using property_dealer_API.Application.Enums;
using property_dealer_API.Application.Exceptions;
using property_dealer_API.Application.MethodReturns;
using property_dealer_API.Core.Entities;
using property_dealer_API.Core.Logic.ActionExecution.ActionHandlers.ActionSteps;
using property_dealer_API.Core.Logic.ActionExecution.ActionHandlers.ActionSteps.PlayerSelectStep;
using property_dealer_API.Core.Logic.ActionExecution.ActionHandlers.ActionSteps.TableHandSelectStep;
using property_dealer_API.Core.Logic.GameRulesManager;
using property_dealer_API.Core.Logic.PendingActionsManager;
using property_dealer_API.Core.Logic.PlayerHandsManager;
using property_dealer_API.Core.Logic.PlayersManager;
using property_dealer_API.Models.Cards;
using property_dealer_API.Models.Enums.Cards;

namespace property_dealer_API.Core.Logic.ActionExecution.ActionHandlers
{
    public class ForcedTradeHandler : ActionHandlerBase, IActionHandler
    {
        private readonly PlayerSelectionStep _playerSelectionStep;
        private readonly ForcedTradeTableHandSelectionStep _tableHandSelectionStep;
        private readonly ForcedTradeWildcardColorSelectionStep _forcedTradeWildcardColorSelectionStep;
        private readonly IActionStepService _stepService;

        public ActionTypes ActionType => ActionTypes.ForcedTrade;

        public ForcedTradeHandler(
            IPlayerManager playerManager,
            IPlayerHandManager playerHandManager,
            IGameRuleManager rulesManager,
            IPendingActionManager pendingActionManager,
            IActionExecutor actionExecutor,
            PlayerSelectionStep playerSelectionStep,
            ForcedTradeTableHandSelectionStep tableHandSelectionStep,
            ForcedTradeWildcardColorSelectionStep forcedTradeWildcardColorSelectionStep,
            IActionStepService stepService
        )
            : base(
                playerManager,
                playerHandManager,
                rulesManager,
                pendingActionManager,
                actionExecutor
            )
        {
            this._playerSelectionStep = playerSelectionStep;
            this._tableHandSelectionStep = tableHandSelectionStep;
            this._forcedTradeWildcardColorSelectionStep = forcedTradeWildcardColorSelectionStep;
            this._stepService = stepService;
        }

        public ActionContext? Initialize(Player initiator, Card card, List<Player> allPlayers)
        {
            if (
                card is not CommandCard commandCard
                || commandCard.Command != ActionTypes.ForcedTrade
            )
            {
                throw new CardMismatchException(initiator.UserId, card.CardGuid.ToString());
            }

            var pendingAction = new PendingAction
            {
                InitiatorUserId = initiator.UserId,
                ActionType = commandCard.Command,
            };
            var newActionContext = base.CreateActionContext(
                card.CardGuid.ToString(),
                DialogTypeEnum.PlayerSelection,
                initiator,
                null,
                allPlayers,
                pendingAction
            );

            base.SetNextDialog(newActionContext, DialogTypeEnum.PlayerSelection, initiator, null);
            return newActionContext;
        }

        public ActionResult? ProcessResponse(Player responder, ActionContext currentContext)
        {
            var isNotActionInitiatingPlayer =
                responder.UserId != currentContext.ActionInitiatingPlayerId;
            var isNotTargetPlayer = responder.UserId != currentContext.TargetPlayerId;

            // For this action, only the initiator should be responding after the first step unless for shields up.
            if (isNotActionInitiatingPlayer && isNotTargetPlayer)
            {
                throw new InvalidOperationException(
                    "Only the action initiator and target player can respond during a Forced Trade."
                );
            }

            switch (currentContext.DialogToOpen)
            {
                case DialogTypeEnum.PlayerSelection:
                    return this._playerSelectionStep.ProcessStep(
                        responder,
                        currentContext,
                        this._stepService,
                        DialogTypeEnum.TableHandSelector
                    );

                case DialogTypeEnum.TableHandSelector:
                    return this._tableHandSelectionStep.ProcessStep(
                        responder,
                        currentContext,
                        this._stepService
                    );

                case DialogTypeEnum.ShieldsUp:
                    return this._stepService.HandleShieldsUp(
                        responder,
                        currentContext,
                        (context, player, _) =>
                            this._tableHandSelectionStep.ProcessStep(
                                player,
                                context,
                                this._stepService
                            )
                    );

                case DialogTypeEnum.WildcardColor:
                    return this._forcedTradeWildcardColorSelectionStep.ProcessStep(
                        responder,
                        currentContext,
                        this._stepService
                    );

                default:
                    throw new InvalidOperationException(
                        $"Invalid state for ForcedTrade action: {currentContext.DialogToOpen}"
                    );
            }
        }
    }
}
