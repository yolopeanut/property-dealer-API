

using property_dealer_API.Hubs.GameWaitingRoom.Service;

namespace property_dealer_API.Hubs.GameWaitingRoom
{
    public class WaitingRoomHub : Microsoft.AspNetCore.SignalR.Hub<IWaitingRoomHub>
    {
        IWaitingRoomService _waitingRoomService;
        public WaitingRoomHub(IWaitingRoomService waitingRoomService)
        {
            this._waitingRoomService = waitingRoomService;
        }

        public override Task OnConnectedAsync()
        {
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            //TODO implement changing ids for reconnection
            return base.OnDisconnectedAsync(exception);
        }

        public async Task GetAllPlayerList(string gameRoomLobbyId)
        {
            var allPlayers = this._waitingRoomService.GetAllPlayers(gameRoomLobbyId);
            if (allPlayers.Count == 0)
            {
                await Clients.Caller.ErrorMsg("Error no game found");
                return;
            }

            await Clients.All.AllGameRoomPlayerList(allPlayers);
        }

        public async Task GetGameRoomCfg(string gameRoomId)
        {
            var gameRoomCfg = this._waitingRoomService.GetRoomConfig(gameRoomId);

            if (gameRoomCfg == null)
            {
                await Clients.Caller.ErrorMsg("Error no game found");
                return;
            }

            await Clients.All.GameRoomCfg(gameRoomCfg);
        }

        //public async Task StartGame() { }

    }
}
