using Microsoft.AspNetCore.SignalR;
using property_dealer_API.Application.Services.CardManagement;
using property_dealer_API.Hubs.GameLobby;

namespace property_dealer_API.Hubs.GamePlay
{

    public class GamePlayHub : Hub<IGamePlayHubClient>, IGamePlayHubServer
    {

        public GamePlayHub()
        {

        }
        public async Task StartGame()
        {

        }

    }
}
