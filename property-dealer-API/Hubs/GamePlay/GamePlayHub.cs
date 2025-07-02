using Microsoft.AspNetCore.SignalR;
using property_dealer_API.Application.Services.CardManagement;
using property_dealer_API.Hubs.GameLobby;

namespace property_dealer_API.Hubs.GamePlay
{

    public class GamePlayHub : Hub<IGamePlayHubClient>, IGamePlayHubServer
    {
        private readonly ICardFactoryService _cardFactoryService;

        public GamePlayHub(ICardFactoryService cardFactoryService)
        {
            this._cardFactoryService = cardFactoryService;
        }
        public async Task StartGame()
        {
            this._cardFactoryService.StartCardFactory();
        }

    }
}
