
using property_dealer_API.Application.Services.CardManagement;
using property_dealer_API.Application.Services.GameManagement;

namespace property_dealer_API.Hubs.GamePlay.Service
{
    public class GameplayService : IGameplayService
    {
        private readonly IGameManagerService _gamePlayManagerService;
        private readonly ICardFactoryService _cardManagerService;
        public GameplayService(IGameManagerService gameManagerService, ICardFactoryService cardManagerService)
        {
            this._gamePlayManagerService = gameManagerService;
            this._cardManagerService = cardManagerService;
        }


    }
}
