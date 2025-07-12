using property_dealer_API.Models.Cards;
using property_dealer_API.Models.Cards.BaseJsonDeckDefinitions;
using property_dealer_API.Models.Enums.Cards;
using System.Text.Json;

namespace property_dealer_API.Application.Services.CardManagement
{
    public class CardFactoryService : ICardFactoryService
    {
        public CardFactoryService()
        {

        }

        public List<Card> StartCardFactory()
        {
            var deck = new List<Card>();

            // Read json
            var baseDeckJsonString = File.ReadAllText("Data/BaseCards.json");

            // Deserialize json
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var cardDefinitions = JsonSerializer.Deserialize<List<DeckConfiguration>>(baseDeckJsonString, options);

            // Loop through and build deck
            if (cardDefinitions != null)
            {
                var cardConfig = cardDefinitions[0];
                Console.WriteLine($" - Found deck with {cardConfig.TotalCards} total cards.");

                GenerateMoneyCards(deck, cardConfig.Money);
                GenerateSystemCards(deck, cardConfig.Systems);
                GenerateSystemWildCards(deck, cardConfig.SystemWilds);
                GenerateCommandCards(deck, cardConfig.Commands);
                GenerateTributeCards(deck, cardConfig.Tributes);
            }
            else
            {
                Console.WriteLine("Deserialization failed, the result is null.");
                return [];

            }
            deck.Shuffle();

            return deck;
        }

        private static void GenerateMoneyCards(List<Card> deck, List<MoneyCardDefinition> moneyCardsToGen)
        {
            foreach (var cardsDef in moneyCardsToGen)
            {
                for (int i = 0; i < cardsDef.Count; i++)
                {
                    var card = new MoneyCard(CardTypesEnum.MoneyCard, cardsDef.Value);
                    deck.Add(card);
                }
            }
        }

        private static void GenerateSystemCards(List<Card> deck, List<SystemCardDefinition> systemCardsToGen)
        {
            foreach (var cardsDef in systemCardsToGen)
            {
                if (Enum.TryParse<PropertyCardColoursEnum>(cardsDef.Color, true, out PropertyCardColoursEnum colorEnum))
                {
                    for (int i = 0; i < cardsDef.Count; i++)
                    {
                        var card = new StandardSystemCard(CardTypesEnum.SystemCard, cardsDef.Name, cardsDef.Value, colorEnum, cardsDef.Description);
                        deck.Add(card);
                    }
                }
                else
                {
                    Console.WriteLine($"Warning: Invalid color '{cardsDef.Color}' found for card '{cardsDef.Name}'. This card will not be generated.");
                }
            }
        }

        private static void GenerateSystemWildCards(List<Card> deck, List<SystemWildCardDefinition> systemWildCardsToGen)
        {
            foreach (var cardsDef in systemWildCardsToGen)
            {
                for (int i = 0; i < cardsDef.Count; i++)
                {
                    var card = new SystemWildCard(CardTypesEnum.SystemWildCard, cardsDef.Name, cardsDef.Value, cardsDef.Description);
                    deck.Add(card);
                }
            }
        }

        private static void GenerateCommandCards(List<Card> deck, List<CommandCardDefinition> commandCardsToGen)
        {
            foreach (var cardsDef in commandCardsToGen)
            {
                if (Enum.TryParse<ActionTypes>(cardsDef.Type, true, out ActionTypes command))
                {
                    for (int i = 0; i < cardsDef.Count; i++)
                    {
                        var card = new CommandCard(CardTypesEnum.CommandCard, command, cardsDef.Name, cardsDef.Value, cardsDef.Description);
                        deck.Add(card);
                    }
                }
                else
                {
                    Console.WriteLine($"Warning: Invalid ActionTypes '{cardsDef.Type}' found for card '{cardsDef.Name}'. This card will not be generated.");
                }
            }
        }

        private static void GenerateTributeCards(List<Card> deck, List<TributeCardDefinition> tributeCardsToGen)
        {
            foreach (var cardsDef in tributeCardsToGen)
            {
                var colorsList = cardsDef.Colors;
                var colorsEnumAsList = new List<PropertyCardColoursEnum>();

                // Validating each color
                foreach (var colors in colorsList)
                {
                    if (Enum.TryParse<PropertyCardColoursEnum>(colors, true, out PropertyCardColoursEnum colorsEnum))
                    {
                        colorsEnumAsList.Add(colorsEnum);
                    }
                    else
                    {
                        Console.WriteLine($"Warning: Invalid color '{colors}' found for rental card. This card will not be generated.");
                        continue;
                    }
                }

                for (int i = 0; i < cardsDef.Count; i++)
                {
                    var card = new TributeCard(CardTypesEnum.TributeCard, cardsDef.Value, colorsEnumAsList, cardsDef.Description);
                    deck.Add(card);
                }
            }
        }
    }
}
