using Microsoft.AspNetCore.SignalR;
using property_dealer_API.Application.Exceptions;
using property_dealer_API.Core.Entities;
using property_dealer_API.Hubs.GameWaitingRoom.Service;

namespace property_dealer_API.Hubs.GameWaitingRoom
{
    public class WaitingRoomHub : Hub<IWaitingRoomHubClient>, IWaitingRoomHubServer
    {
        private readonly IWaitingRoomService _waitingRoomService;

        private const string GameRoomIdKey = "GameRoomId";
        private const string UserIdKey = "UserId";

        public WaitingRoomHub(IWaitingRoomService waitingRoomService)
        {
            this._waitingRoomService = waitingRoomService;
        }

        public override async Task OnConnectedAsync()
        {
            // 1. Get IDs from the query string
            var httpContext = this.Context.GetHttpContext();
            if (httpContext == null)
            {
                // Should not happen in normal circumstances
                this.Context.Abort();
                await base.OnConnectedAsync();
                return;
            }

            // 2. Validate the gameRoomId
            var gameRoomId = httpContext.Request.Query["gameRoomId"].ToString();
            var userId = httpContext.Request.Query["userId"].ToString();
            if (string.IsNullOrEmpty(gameRoomId) || string.IsNullOrEmpty(userId))
            {
                // If no ID is provided, we can't proceed.
                this.Context.Abort();
                return;
            }

            // Further validation to see if the room and player actually exists
            Console.WriteLine($"=== WAITING ROOM CONNECTION ===");
            Console.WriteLine($"Room ID from query: '{gameRoomId}'");
            Console.WriteLine($"User ID from query: '{userId}'");

            // Further validation to see if the room and player actually exists
            var roomExists = this._waitingRoomService.DoesRoomExist(gameRoomId);
            Console.WriteLine($"Room exists check: {roomExists}");

            if (!roomExists)
            {
                Console.WriteLine($"=== ROOM NOT FOUND ===");
                Console.WriteLine($"Looking for room ID: '{gameRoomId}'");

                // Let's also check what rooms DO exist
                var allRooms = this._waitingRoomService.GetAllExistingRoomIds(); // We'll need to add this method
                Console.WriteLine($"Existing rooms: {string.Join(", ", allRooms)}");

                await this.Clients.Caller.ErrorMsg("The game room you are trying to join does not exist.");
                this.Context.Abort();
                await base.OnConnectedAsync();
                return;
            }
            if (!this._waitingRoomService.DoesPlayerExist(userId, gameRoomId))
            {
                await this.Clients.Caller.ErrorMsg("Your player object does not exist, please retry.");
                this.Context.Abort();
                await base.OnConnectedAsync();
                return;
            }

            // 3. Store BOTH IDs in the connection's context for later use
            this.Context.Items[GameRoomIdKey] = gameRoomId;
            this.Context.Items[UserIdKey] = userId;

            // 4. Add connection to the group
            await this.Groups.AddToGroupAsync(this.Context.ConnectionId, gameRoomId);

            // 5. Notify the group that a player has joined
            try
            {
                var player = this._waitingRoomService.GetPlayerByUserId(gameRoomId, userId);
                await this.Clients.Group(gameRoomId).PlayerJoined(player.PlayerName);
            }
            catch (GameNotFoundException)
            {
                await this.Clients.Caller.ErrorMsg("Game room not found.");
                this.Context.Abort();
                return;
            }
            catch (PlayerNotFoundException)
            {
                await this.Clients.Caller.ErrorMsg("Player not found in game.");
                this.Context.Abort();
                return;
            }

            // Send the full player list to the newly connected client or the whole group
            await this.GetAllPlayerList(gameRoomId);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            this.Context.Items.TryGetValue(GameRoomIdKey, out var gameRoomIdObj);
            this.Context.Items.TryGetValue(UserIdKey, out var userIdObj);

            var gameRoomId = gameRoomIdObj as string;
            var userId = userIdObj as string;


            if (!String.IsNullOrEmpty(gameRoomId) && !String.IsNullOrEmpty(userId))
            {
                await this.Groups.RemoveFromGroupAsync(this.Context.ConnectionId, gameRoomId);

                // await this.LeaveWaitingRoom(gameRoomId, userId);
            }
        }

        public async Task GetAllPlayerList(string gameRoomId)
        {
            try
            {
                var allPlayers = this._waitingRoomService.GetAllPlayers(gameRoomId);

                if (allPlayers.Count == 0)
                {
                    await this.Clients.Caller.ErrorMsg("No players found in game room");
                    return;
                }

                Console.WriteLine(gameRoomId, allPlayers);
                await this.Clients.Group(gameRoomId).AllGameRoomPlayerList(allPlayers);
            }
            catch (GameNotFoundException)
            {
                await this.Clients.Caller.ErrorMsg("Game room not found");
            }
        }

        public async Task GetGameRoomCfg(string gameRoomId)
        {
            try
            {
                var gameRoomCfg = this._waitingRoomService.GetRoomConfig(gameRoomId);
                await this.Clients.Group(gameRoomId).GameRoomCfg(gameRoomCfg);
            }
            catch (GameNotFoundException)
            {
                await this.Clients.Caller.ErrorMsg("Game room not found");
            }
        }

        public Task UpdateCfg(string gameRoomId, GameConfig newConfig)
        {
            throw new NotImplementedException();
        }

        public async Task StartGame(string gameRoomId)
        {
            try
            {
                this._waitingRoomService.StartGame(gameRoomId);
                //Send a message to all those who are in the group 
                await this.Clients.Group(gameRoomId).GameStarted(gameRoomId);
            }
            catch (GameNotFoundException)
            {
                await this.Clients.Caller.ErrorMsg("Game room not found");
            }
        }

        public async Task LeaveWaitingRoom(string gameRoomId, string userId)
        {
            try
            {
                var playerName = this._waitingRoomService.RemovePlayerFromGame(gameRoomId, userId);

                await this.Clients.Group(gameRoomId).PlayerLeft(playerName);

                // Send the full player list to the whole group
                await this.GetAllPlayerList(gameRoomId);
            }
            catch (GameNotFoundException)
            {
                await this.Clients.Caller.ErrorMsg("Game room not found");
            }
            catch (PlayerNotFoundException)
            {
                await this.Clients.Caller.ErrorMsg("Player not found in game");
            }
            catch (PlayerRemovalFailedException e)
            {
                await this.Clients.Caller.ErrorMsg("Could not remove player table hand: " + $"{e.Message}");
            }
        }
    }
}