using property_dealer_API.Models.Cards;
using property_dealer_API.Models.Enums.Cards;

namespace PropertyDealer.API.Tests.TestHelpers
{
    public static class CardTestHelpers
    {
        public static CommandCard CreateCommandCard(
            ActionTypes command = ActionTypes.HostileTakeover,
            string name = "Test Command Card",
            int value = 1,
            string description = "Test command card description")
        {
            return new CommandCard(CardTypesEnum.CommandCard, command, name, value, description);
        }

        public static MoneyCard CreateMoneyCard(int value = 1)
        {
            return new MoneyCard(CardTypesEnum.MoneyCard, value);
        }

        public static StandardSystemCard CreateStandardSystemCard(
            PropertyCardColoursEnum color = PropertyCardColoursEnum.Red,
            string name = "Test Property",
            int value = 2,
            string description = "Test property description",
            int maxCards = 3,
            List<int>? rentalValues = null)
        {
            rentalValues ??= new List<int> { 1, 2, 4 };
            return new StandardSystemCard(CardTypesEnum.SystemCard, name, value, color, description, maxCards, rentalValues);
        }

        public static TributeCard CreateTributeCard(
            int value = 2,
            List<PropertyCardColoursEnum>? targetColors = null,
            string description = "Test tribute card")
        {
            targetColors ??= new List<PropertyCardColoursEnum> { PropertyCardColoursEnum.Red, PropertyCardColoursEnum.Green };
            return new TributeCard(CardTypesEnum.TributeCard, value, targetColors, description);
        }

        public static SystemWildCard CreateSystemWildCard(
            string name = "Test Wild Card",
            int value = 0,
            string description = "Test wild card description")
        {
            return new SystemWildCard(CardTypesEnum.SystemWildCard, name, value, description);
        }

        public static Card CreateActionTypeCard(int actionTypeValue)
        {
            if (!Enum.IsDefined(typeof(ActionTypes), actionTypeValue))
            {
                throw new ArgumentException($"Invalid ActionType value: {actionTypeValue}");
            }

            var actionType = (ActionTypes)actionTypeValue;

            return actionType switch
            {
                ActionTypes.Tribute => CreateTributeCard(),
                ActionTypes.SystemWildCard => CreateSystemWildCard(),
                _ => CreateCommandCard(actionType)
            };
        }

        public static List<Card> CreateAllActionTypeCards()
        {
            var cards = new List<Card>();
            var actionTypeValues = Enum.GetValues<ActionTypes>();

            foreach (var actionType in actionTypeValues)
            {
                cards.Add(CreateActionTypeCard((int)actionType));
            }

            return cards;
        }

        public static List<Card> CreatePropertyCardSet(
            PropertyCardColoursEnum color,
            int cardCount,
            int maxCards = 3,
            List<int>? rentalValues = null)
        {
            var cards = new List<Card>();
            rentalValues ??= new List<int> { 1, 2, 4 };

            for (int i = 0; i < cardCount; i++)
            {
                var card = CreateStandardSystemCard(
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