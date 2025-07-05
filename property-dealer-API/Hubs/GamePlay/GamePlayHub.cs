using Microsoft.AspNetCore.SignalR;
using property_dealer_API.Application.Exceptions;
using property_dealer_API.Hubs.GameLobby;
using property_dealer_API.Hubs.GamePlay.Service;

namespace property_dealer_API.Hubs.GamePlay
{

    public class GamePlayHub : Hub<IGamePlayHubClient>, IGamePlayHubServer
    {
        private readonly IGameplayService _gamePlayService;

        private const string GameRoomIdKey = "GameRoomId";
        private const string UserIdKey = "UserId";

        public GamePlayHub(IGameplayService gameplayService)
        {
            this._gamePlayService = gameplayService;
        }

        public override async Task OnConnectedAsync()
        {
            // 1. Get IDs from the query string
            var httpContext = Context.GetHttpContext();
            if (httpContext == null)
            {
                // Should not happen in normal circumstances
                Context.Abort();
                await base.OnConnectedAsync();
                return;
            }

            // 2. Validate the gameRoomId
            var gameRoomId = httpContext.Request.Query["gameRoomId"].ToString();
            var userId = httpContext.Request.Query["userId"].ToString();
            if (string.IsNullOrEmpty(gameRoomId) || string.IsNullOrEmpty(userId))
            {
                // If no ID is provided, we can't proceed.
                Context.Abort();
                return;
            }

            // Further validation to see if the room and player actually exists
            Console.WriteLine($"=== GAMEPLAY ROOM CONNECTION ===");
            Console.WriteLine($"Room ID from query: '{gameRoomId}'");
            Console.WriteLine($"User ID from query: '{userId}'");

            // Further validation to see if the room and player actually exists
            var roomExists = _gamePlayService.DoesRoomExist(gameRoomId);

            if (!roomExists)
            {
                await Clients.Caller.ErrorMsg("The game room you are trying to join does not exist.");
                Context.Abort();
                await base.OnConnectedAsync();
                return;
            }
            if (!_gamePlayService.DoesPlayerExist(userId, gameRoomId))
            {
                await Clients.Caller.ErrorMsg("Your player object does not exist, please retry.");
                Context.Abort();
                await base.OnConnectedAsync();
                return;
            }

            // 3. Store BOTH IDs in the connection's context for later use
            Context.Items[GameRoomIdKey] = gameRoomId;
            Context.Items[UserIdKey] = userId;

            // 4. Add connection to the group
            await Groups.AddToGroupAsync(Context.ConnectionId, gameRoomId);

            // 5. Notify the group that a player has joined
            var player = this._gamePlayService.GetPlayerByUserId(gameRoomId, userId);

            // Send the full player list to the newly connected client or the whole group
            await this.GetAllPlayerList(gameRoomId);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            Context.Items.TryGetValue(GameRoomIdKey, out var gameRoomIdObj);
            Context.Items.TryGetValue(UserIdKey, out var userIdObj);

            var gameRoomId = gameRoomIdObj as string;
            var userId = userIdObj as string;


            if (!String.IsNullOrEmpty(gameRoomId) && !String.IsNullOrEmpty(userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, gameRoomId);

                await this.LeaveGameRoom(gameRoomId, userId);
            }
        }

        public async Task LeaveGameRoom(string gameRoomId, string userId)
        {
            try
            {
                var playerName = _gamePlayService.RemovePlayerFromGame(gameRoomId, userId);

                if (!string.IsNullOrEmpty(playerName))
                {
                    await Clients.Group(gameRoomId).PlayerLeft(playerName);

                    // Send the full player list to the whole group
                    await this.GetAllPlayerList(gameRoomId);
                }
            }
            catch (PlayerNotFoundException e)
            {
                await Clients.Group(gameRoomId).ErrorMsg(string.Concat("Player trying to leave was not found, report this bug if found.", e));
            }
        }

        public async Task GetAllPlayerList(string gameRoomId)
        {
            try
            {
                var allPlayers = this._gamePlayService.GetAllPlayers(gameRoomId);

                Console.WriteLine(gameRoomId, allPlayers);
                await Clients.Group(gameRoomId).AllGameRoomPlayerList(allPlayers);
            }
            catch (GameNotFoundException e)
            {
                await Clients.Group(gameRoomId).ErrorMsg(string.Concat("Getting all player list was not found, report this bug if found.", e));
            }


        }

        public async Task GetPlayerHand(string gameRoomId, string userId)
        {
            try
            {
                var playerHand = this._gamePlayService.GetPlayerHand(gameRoomId, userId);
                await Clients.Caller.PlayerHand(playerHand);
            }
            catch (PlayerNotFoundException e)
            {
                await Clients.Caller.ErrorMsg(e.Message);
            }
            catch (GameNotFoundException e)
            {
                await Clients.Caller.ErrorMsg(e.Message);
            }
        }

        public async Task GetAllTableCard(string gameRoomId, string userId)
        {
            try
            {
                var allTableHands = this._gamePlayService.GetAllOtherPlayerTableHands(gameRoomId);
                await Clients.Group(gameRoomId).AllTableHands(allTableHands);
            }
            catch (GameNotFoundException e)
            {
                await Clients.Caller.ErrorMsg(e.Message);
            }
        }

        public async Task PlayCard(string gameRoomId, string userId)
        {
            await GetAllTableCard(gameRoomId, userId);
        }

        public Task DrawCard(string gameRoomId, string userId)
        {
            throw new NotImplementedException();
        }
    }
}
