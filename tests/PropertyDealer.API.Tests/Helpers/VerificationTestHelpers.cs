using property_dealer_API.Core.Logic.PlayerHandsManager;
using property_dealer_API.Models.Cards;
using property_dealer_API.Models.Enums.Cards;

namespace PropertyDealer.API.Tests.TestHelpers
{
    public static class VerificationTestHelpers
    {
        public static int GetPlayerMoneyTotal(IPlayerHandManager handManager, string userId)
        {
            var totalMoney = 0;

            handManager.ProcessAllTableHandsSafely((playerId, tableHand, moneyHand) =>
            {
                if (playerId == userId)
                {
                    totalMoney = moneyHand.OfType<MoneyCard>().Sum(c => c.BankValue ?? 0);
                }
            });

            return totalMoney;
        }

        public static int GetPropertySetSize(IPlayerHandManager handManager, string userId, PropertyCardColoursEnum color)
        {
            var properties = handManager.GetPropertyGroupInPlayerTableHand(userId, color);
            return properties.Count;
        }

        public static bool PlayerHasCard(IPlayerHandManager handManager, string userId, Card card)
        {
            var hand = handManager.GetPlayerHand(userId);
            return hand.Contains(card);
        }
    }
}