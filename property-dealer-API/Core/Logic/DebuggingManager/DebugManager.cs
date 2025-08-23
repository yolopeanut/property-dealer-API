using property_dealer_API.Application.DTOs.Responses;
using property_dealer_API.Application.Enums;
using property_dealer_API.Core.Logic.DecksManager;
using property_dealer_API.Core.Logic.GameRulesManager;
using property_dealer_API.Core.Logic.PendingActionsManager;
using property_dealer_API.Core.Logic.PlayerHandsManager;
using property_dealer_API.Core.Logic.PlayersManager;
using property_dealer_API.Models.Cards;
using property_dealer_API.Models.Enums.Cards;

namespace property_dealer_API.Core.Logic.DebuggingManager
{
    public class DebugManager : IDebugManager
    {
        // Use FULL write interfaces for admin powers
        private readonly IPlayerHandManager _playerHandManager;
        private readonly IPlayerManager _playerManager;
        private readonly IPendingActionManager _pendingActionManager;
        private readonly IGameRuleManager _rulesManager;
        private readonly IDeckManager _deckManager;

        public DebugManager(
            IPlayerHandManager playerHandManager,
            IPlayerManager playerManager,
            IPendingActionManager pendingActionManager,
            IGameRuleManager gameRuleManager,
            IDeckManager deckManager
        )
        {
            this._playerHandManager = playerHandManager;
            this._playerManager = playerManager;
            this._rulesManager = gameRuleManager;
            this._pendingActionManager = pendingActionManager;
            this._deckManager = deckManager;
        }

        public void ProcessCommand(DebugOptionsEnum debugCommand, DebugContext debugContext)
        {
            switch (debugCommand)
            {
                case (DebugOptionsEnum.SpawnFullSet):
                    this.SpawnFullSet(debugContext);
                    break;

                case DebugOptionsEnum.SpawnCard:
                    this.SpawnCard(debugContext);
                    break;

                case DebugOptionsEnum.SpawnDummyPlayer:
                    this.SpawnDummyPlayer(debugContext);
                    break;

                case DebugOptionsEnum.SpawnAllCommandCard:
                    this.SpawnAllCommandCards(debugContext);
                    break;

                case DebugOptionsEnum.ChangeHandLimit:
                    this.ChangeHandLimits(debugContext);
                    break;
            }
        }

        private void ChangeHandLimits(DebugContext debugContext)
        {
            var newHandLimit = debugContext.NewHandLimit;
            if (newHandLimit == null)
            {
                throw new InvalidOperationException(
                    "Cannot set new max card in hand limit when value is null!"
                );
            }

            this._rulesManager.MAX_CARDS_IN_HAND = (int)newHandLimit;
        }

        private void SpawnAllCommandCards(DebugContext debugContext)
        {
            var cardsSpawned = this.CreateAllActionTypeCards();

            cardsSpawned.ForEach(card =>
                this._playerHandManager.AddCardToPlayerHand(debugContext.UserId, card)
            );
        }

        private void SpawnDummyPlayer(DebugContext debugContext)
        {
            for (int i = 0; i < debugContext.NumberOfDummyPlayersToSpawn; i++)
            {
                this._playerHandManager.AddPlayerHand($"dummyPlayer{i}");
                var freshCards = this._deckManager.DrawCard(5);
                this._playerHandManager.AssignPlayerHand($"dummyPlayer{i}", freshCards);
            }
        }

        private void SpawnFullSet(DebugContext debugContext)
        {
            List<StandardSystemCard> cardsToSpawn;
            if (debugContext.SetColorToSpawn.HasValue)
            {
                cardsToSpawn = this.CreatePropertyCardSet(debugContext.SetColorToSpawn.Value, 3);
            }
            else
            {
                cardsToSpawn = this.CreatePropertyCardSet(PropertyCardColoursEnum.Red, 3);
            }

            cardsToSpawn.ForEach(card =>
                this._playerHandManager.AddCardToPlayerTableHand(
                    debugContext.UserId,
                    card,
                    card.CardColoursEnum
                )
            );
        }

        private void SpawnCard(DebugContext debugContext)
        {
            var cardTypeToSpawn = debugContext.CardTypeToSpawn;
            if (cardTypeToSpawn == null)
            {
                throw new InvalidOperationException("Cannot spawn card when card type is null!");
            }

            Card spawnedCard;
            switch (cardTypeToSpawn)
            {
                case CardTypesEnum.CommandCard:
                    if (!debugContext.ActionCardToSpawnType.HasValue)
                    {
                        throw new InvalidOperationException(
                            "Cannot spawn command card when ActionCardToSpawnType is null!"
                        );
                    }

                    spawnedCard = this.CreateCommandCard(debugContext.ActionCardToSpawnType.Value);
                    break;

                case CardTypesEnum.SystemCard:
                    if (!debugContext.SetColorToSpawn.HasValue)
                    {
                        throw new InvalidOperationException(
                            "Cannot spawn system card when SetColorToSpawn is null!"
                        );
                    }

                    spawnedCard = this.CreateStandardSystemCard(debugContext.SetColorToSpawn.Value);
                    break;

                case CardTypesEnum.MoneyCard:
                    spawnedCard = this.CreateMoneyCard();
                    break;

                case CardTypesEnum.SystemWildCard:
                    spawnedCard = this.CreateSystemWildCard();
                    break;

                case CardTypesEnum.TributeCard:
                    if (
                        debugContext.TributeTargetColors == null
                        || debugContext.TributeTargetColors.Count < 0
                    )
                    {
                        throw new InvalidOperationException(
                            "Cannot spawn tribute card when TributeTargetColors is null!"
                        );
                    }

                    spawnedCard = this.CreateTributeCard(
                        targetColors: debugContext.TributeTargetColors
                    );
                    break;
            }
        }

        private CommandCard CreateCommandCard(ActionTypes command)
        {
            var (name, value, description) = command switch
            {
                ActionTypes.HostileTakeover => (
                    "Hostile Takeover",
                    5,
                    "Steal a full set of systems from any player."
                ),
                ActionTypes.ShieldsUp => (
                    "Shields Up",
                    4,
                    "Cancel any command card played against you."
                ),
                ActionTypes.PirateRaid => (
                    "Pirate Raid",
                    3,
                    "Steal any single system from another player (cannot be from a completed set)."
                ),
                ActionTypes.ForcedTrade => (
                    "Forced Trade",
                    3,
                    "Force another player to trade one of their systems for one of yours."
                ),
                ActionTypes.BountyHunter => (
                    "Bounty Hunter",
                    3,
                    "Force any one player to pay you 5M Credits."
                ),
                ActionTypes.TradeDividend => (
                    "Trade Dividend",
                    2,
                    "All players must pay you 2M Credits."
                ),
                ActionTypes.ExploreNewSector => (
                    "Explore a New Sector",
                    1,
                    "Draw 2 extra cards from the deck."
                ),
                ActionTypes.SpaceStation => (
                    "Space Station",
                    3,
                    "Add 3M Credits to the tribute value of a completed system set. Can only be played on a full set."
                ),
                ActionTypes.Starbase => (
                    "Starbase",
                    4,
                    "Add 4M Credits to the tribute value of a completed system set. Requires a Space Station to be on the set first."
                ),
                ActionTypes.TradeEmbargo => (
                    "Trade Embargo",
                    1,
                    "Play with a Tribute card to double the amount owed."
                ),
                ActionTypes.TributeWildCard => (
                    "TributeWildCard",
                    5,
                    "Use this tribute card on any property set"
                ),

                // Add a default case to handle any undefined ActionTypes
                _ => throw new ArgumentOutOfRangeException(
                    nameof(command),
                    $"No definition found for command card type: {command}"
                ),
            };

            return new CommandCard(CardTypesEnum.CommandCard, command, name, value, description);
        }

        private MoneyCard CreateMoneyCard(int value = 1)
        {
            return new MoneyCard(CardTypesEnum.MoneyCard, value);
        }

        private StandardSystemCard CreateStandardSystemCard(
            PropertyCardColoursEnum color = PropertyCardColoursEnum.Red,
            string name = "Test Property",
            int value = 2,
            string description = "Test property description",
            int maxCards = 3,
            List<int>? rentalValues = null
        )
        {
            rentalValues ??= new List<int> { 1, 2, 4 };
            return new StandardSystemCard(
                CardTypesEnum.SystemCard,
                name,
                value,
                color,
                description,
                maxCards,
                rentalValues
            );
        }

        private TributeCard CreateTributeCard(
            int value = 2,
            List<PropertyCardColoursEnum>? targetColors = null,
            string description = "Test tribute card"
        )
        {
            targetColors ??= new List<PropertyCardColoursEnum>
            {
                PropertyCardColoursEnum.Red,
                PropertyCardColoursEnum.Cyan,
            };
            return new TributeCard(CardTypesEnum.TributeCard, value, targetColors, description);
        }

        private SystemWildCard CreateSystemWildCard(
            string name = "Test Wild Card",
            int value = 0,
            string description = "Test wild card description"
        )
        {
            return new SystemWildCard(CardTypesEnum.SystemWildCard, name, value, description);
        }

        private Card CreateActionTypeCard(int actionTypeValue)
        {
            if (!Enum.IsDefined(typeof(ActionTypes), actionTypeValue))
            {
                throw new ArgumentException($"Invalid ActionType value: {actionTypeValue}");
            }

            var actionType = (ActionTypes)actionTypeValue;

            return actionType switch
            {
                ActionTypes.Tribute => this.CreateTributeCard(),
                ActionTypes.SystemWildCard => this.CreateSystemWildCard(),
                _ => this.CreateCommandCard(actionType),
            };
        }

        private List<Card> CreateAllActionTypeCards()
        {
            var cards = new List<Card>();
            var actionTypeValues = Enum.GetValues<ActionTypes>();

            foreach (var actionType in actionTypeValues)
            {
                cards.Add(this.CreateActionTypeCard((int)actionType));
            }

            return cards;
        }

        private List<StandardSystemCard> CreatePropertyCardSet(
            PropertyCardColoursEnum color,
            int cardCount,
            int maxCards = 3,
            List<int>? rentalValues = null
        )
        {
            var cards = new List<StandardSystemCard>();
            rentalValues ??= new List<int> { 1, 2, 4 };

            for (int i = 0; i < cardCount; i++)
            {
                var card = this.CreateStandardSystemCard(
                    color: color,
                    name: $"{color} Property {i + 1}",
                    value: 2,
                    description: $"Test {color} property card",
                    maxCards: maxCards,
                    rentalValues: rentalValues
                );
                cards.Add(card);
            }

            return cards;
        }
    }
}
