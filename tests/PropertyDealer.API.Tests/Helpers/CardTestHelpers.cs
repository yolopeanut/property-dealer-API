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
    }
}