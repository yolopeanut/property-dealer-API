using property_dealer_API.Application.Enums;
using property_dealer_API.Core;
using property_dealer_API.Core.Entities;
using property_dealer_API.Core.Logic.PlayerHandsManager;
using property_dealer_API.Models.Cards;
using property_dealer_API.Models.Enums.Cards;

namespace PropertyDealer.API.Tests.TestHelpers
{
    public static class ResponseTestHelpers
    {
        public static ActionContext CreatePlayerSelectionResponse(
            ActionContext originalContext,
            string targetPlayerId)
        {
            var response = originalContext.Clone();
            response.TargetPlayerId = targetPlayerId;
            response.DialogResponse = CommandResponseEnum.SendIt;
            return response;
        }

        public static ActionContext CreatePropertySetResponse(
            ActionContext originalContext,
            PropertyCardColoursEnum color)
        {
            var response = originalContext.Clone();
            response.TargetSetColor = color;
            response.DialogResponse = CommandResponseEnum.Accept;
            return response;
        }

        public static ActionContext CreatePayValueResponse(
            ActionContext originalContext,
            IPlayerHandManager handManager,
            string payerId,
            int amountToPay)
        {
            Console.WriteLine($"[DEBUG] CreatePayValueResponse - Creating response for payerId: {payerId}, amountToPay: {amountToPay}");

            var response = originalContext.Clone();
            var selectedCards = SelectCardsForPayment(handManager, payerId, amountToPay);

            Console.WriteLine($"[DEBUG] CreatePayValueResponse - Selected {selectedCards.Count} cards for {payerId}:");
            foreach (var cardId in selectedCards)
            {
                Console.WriteLine($"[DEBUG]   Card ID: {cardId}");
            }

            response.OwnTargetCardId = selectedCards;
            response.DialogResponse = CommandResponseEnum.Accept;
            return response;
        }
        public static ActionContext CreateTableHandResponse(
            ActionContext originalContext,
            string cardId)
        {
            var response = originalContext.Clone();
            response.TargetCardId = cardId;
            response.DialogResponse = CommandResponseEnum.SendIt;
            return response;
        }

        public static ActionContext CreateOwnHandResponse(
            ActionContext originalContext,
            string cardId)
        {
            var response = originalContext.Clone();
            response.OwnTargetCardId = [cardId];
            response.DialogResponse = CommandResponseEnum.Accept;
            return response;
        }

        public static ActionContext CreateShieldsUpResponse(ActionContext originalContext)
        {
            var response = originalContext.Clone();
            response.DialogResponse = CommandResponseEnum.ShieldsUp;
            return response;
        }
        public static ActionContext CreateWildcardColorResponse(ActionContext originalContext, PropertyCardColoursEnum color)
        {
            var response = originalContext.Clone();
            response.TargetSetColor = color;
            response.DialogResponse = CommandResponseEnum.Accept;
            return response;

        }
        public static List<Card> GetPropertyGroupSafely(IPlayerHandManager handManager, string userId, PropertyCardColoursEnum color)
        {
            try
            {
                return handManager.GetPropertyGroupInPlayerTableHand(userId, color);
            }
            catch (InvalidOperationException)
            {
                return new List<Card>(); // Return empty list if property group doesn't exist
            }
        }

        private static List<string> SelectCardsForPayment(
            IPlayerHandManager handManager,
            string userId,
            int amountNeeded)
        {
            Console.WriteLine($"[DEBUG] SelectCardsForPayment - Starting for userId: {userId}, amountNeeded: {amountNeeded}");

            var selectedCards = new List<string>();
            var totalValue = 0;

            try
            {
                var moneyCards = new List<Card>();

                Console.WriteLine($"[DEBUG] SelectCardsForPayment - Processing all table hands to find user {userId}");

                handManager.ProcessAllTableHandsSafely((playerId, tableHand, moneyHand) =>
                {
                    Console.WriteLine($"[DEBUG] SelectCardsForPayment - Processing player: {playerId}, money hand count: {moneyHand.Count}");

                    foreach (var card in moneyHand)
                    {
                        Console.WriteLine($"[DEBUG]   Player {playerId} has card: {card.CardGuid} (Type: {card.GetType().Name})");
                    }

                    if (playerId == userId)
                    {
                        Console.WriteLine($"[DEBUG] SelectCardsForPayment - Found target user {userId}, adding {moneyHand.Count} money cards");
                        moneyCards.AddRange(moneyHand);
                    }
                });

                Console.WriteLine($"[DEBUG] SelectCardsForPayment - Found {moneyCards.Count} money cards for user {userId}");

                foreach (var card in moneyCards.OfType<MoneyCard>().OrderBy(c => c.BankValue))
                {
                    Console.WriteLine($"[DEBUG] SelectCardsForPayment - Considering card: {card.CardGuid}, value: {card.BankValue}, totalValue: {totalValue}");

                    if (totalValue >= amountNeeded)
                    {
                        Console.WriteLine($"[DEBUG] SelectCardsForPayment - Reached required amount, stopping");
                        break;
                    }

                    selectedCards.Add(card.CardGuid.ToString());
                    totalValue += card.BankValue ?? 0;

                    Console.WriteLine($"[DEBUG] SelectCardsForPayment - Selected card: {card.CardGuid}, new totalValue: {totalValue}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG] SelectCardsForPayment - Exception: {ex.Message}");
                throw new InvalidOperationException($"Failed to select cards for payment for user {userId}: {ex.Message}", ex);
            }

            Console.WriteLine($"[DEBUG] SelectCardsForPayment - Final result for {userId}: {selectedCards.Count} cards, totalValue: {totalValue}");
            return selectedCards;
        }

    }
}