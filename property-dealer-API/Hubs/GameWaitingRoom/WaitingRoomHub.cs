

namespace property_dealer_API.Hubs.GameWaitingRoom
{
    public class WaitingRoomHub : Microsoft.AspNetCore.SignalR.Hub<IWaitingRoomHub>
    {
        public override Task OnConnectedAsync()
        {
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            return base.OnDisconnectedAsync(exception);
        }

        public async Task GetAllPlayerList() { }

        public async Task GetGameRoomCfg() { }

        public async Task StartGame() { }

    }
}
