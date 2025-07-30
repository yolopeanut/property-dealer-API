using Microsoft.AspNetCore.SignalR;
using property_dealer_API.Application.Enums;
using property_dealer_API.Core;
using property_dealer_API.Hubs.GameLobby;
using property_dealer_API.Hubs.GamePlay.Service;
using property_dealer_API.Models.Enums.Cards;

namespace property_dealer_API.Hubs.GamePlay
{

    public class GamePlayHub : Hub<IGamePlayHubClient>, IGamePlayHubServer
    {

        #region States
        private readonly IGameplayService _gamePlayService;
        private readonly ILogger<GamePlayHub> _logger;

        private const string GameRoomIdKey = "GameRoomId";
        private const string UserIdKey = "UserId";
        #endregion

        #region Constructor
        public GamePlayHub(IGameplayService gameplayService, ILogger<GamePlayHub> logger)
        {
            this._gamePlayService = gameplayService;
            this._logger = logger;
        }
        #endregion

        #region Hub Connection Management
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
            var roomExists = this._gamePlayService.DoesRoomExist(gameRoomId);

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
            await this.GetAllTableCard(gameRoomId);
            await this.GetLatestDiscardPileCard(gameRoomId);

        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            Context.Items.TryGetValue(GameRoomIdKey, out var gameRoomIdObj);
            Context.Items.TryGetValue(UserIdKey, out var userIdObj);

            var gameRoomId = gameRoomIdObj as string;
            var userId = userIdObj as string;


            //if (!String.IsNullOrEmpty(gameRoomId) && !String.IsNullOrEmpty(userId))
            //{
            //    await Groups.RemoveFromGroupAsync(Context.ConnectionId, gameRoomId);

            //    await this.LeaveGameRoom(gameRoomId, userId);
            //}
        }
        #endregion

        #region Public Server hub functions
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
            catch (Exception e)
            {
                await this.ExceptionHandler(e);
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
            catch (Exception e)
            {
                await this.ExceptionHandler(e);
            }


        }

        public async Task GetPlayerHand(string gameRoomId, string userId)
        {
            try
            {
                var playerHand = this._gamePlayService.GetPlayerHand(gameRoomId, userId);
                await Clients.Caller.PlayerHand(playerHand);
            }
            catch (Exception e)
            {
                await this.ExceptionHandler(e);
            }
        }

        public async Task GetAllTableCard(string gameRoomId)
        {
            try
            {
                var allTableHands = this._gamePlayService.GetAllPlayerTableHands(gameRoomId);
                await Clients.Group(gameRoomId).AllTableHands(allTableHands);
            }
            catch (Exception e)
            {
                await this.ExceptionHandler(e);
            }
        }

        public async Task GetLatestDiscardPileCard(string gameRoomId)
        {
            try
            {
                var discardedCard = this._gamePlayService.GetMostRecentDiscardedCard(gameRoomId);
                await Clients.Group(gameRoomId).LatestDiscardPileCard(discardedCard);
            }
            catch (Exception e)
            {
                await this.ExceptionHandler(e);
            }
        }

        public async Task PlayCard(string gameRoomId, string userId, string cardId, CardDestinationEnum cardDestination, PropertyCardColoursEnum? cardColorDestinationEnum)
        {
            _logger.LogInformation(
                   "--> PlayCard called with params: GameRoomId={GameRoomId}, UserId={UserId}, CardId={CardId}, Destination={Destination}, ColorDestination={ColorDestination}",
                   gameRoomId, userId, cardId, cardDestination, cardColorDestinationEnum);
            //Play user card (send to discard pile, show up on all players screen, remove from user hand, draw card)
            try
            {
                var actionContext = this._gamePlayService.PlayCard(gameRoomId, userId, cardId, cardDestination, cardColorDestinationEnum);

                if (actionContext != null)
                {
                    await Clients.Group(gameRoomId).OpenCommandDialog(actionContext);
                }

                bool includeDiscardPile = cardDestination == CardDestinationEnum.CommandPile;
                await RefreshFullGameState(gameRoomId, includeDiscardPile, userId);
            }
            catch (Exception e)
            {
                await this.ExceptionHandler(e);
            }
        }

        public async Task GetCurrentPlayerTurn(string gameRoomId)
        {
            try
            {
                await Clients.Group(gameRoomId).CurrentPlayerTurn(this._gamePlayService.GetCurrentPlayerTurn(gameRoomId));
            }
            catch (Exception e)
            {
                await this.ExceptionHandler(e);
            }
        }

        public async Task SendActionResponse(string gameRoomId, string userId, ActionContext actionContext)
        {
            try
            {
                var newActionContexts = this._gamePlayService.SendActionResponse(gameRoomId, userId, actionContext);

                await RefreshGameState(gameRoomId, actionContext.ActionInitiatingPlayerId, userId);

                // If there's a new dialog to open, send it to clients
                if (newActionContexts != null)
                {
                    foreach (var newActionContext in newActionContexts)
                    {
                        await Clients.Group(gameRoomId).OpenCommandDialog(newActionContext);
                    }
                }
            }
            catch (Exception e)
            {
                await this.ExceptionHandler(e);
            }
        }

        public async Task DebugManager(string gameRoomId, string userId, DebugOptionsEnum debugOption)
        {
            try
            {
                this._gamePlayService.SendDebugCommand(gameRoomId, userId, debugOption);
            }
            catch (Exception e)
            {
                await this.ExceptionHandler(e);
            }
        }

        public async Task CheckIfAnyPlayersWon(string gameRoomId)
        {
            var player = this._gamePlayService.CheckIfAnyPlayersWon(gameRoomId);
            await Clients.Group(gameRoomId).PlayerWon(player);
        }

        #endregion

        #region Helper methods

        private async Task ExceptionHandler(Exception e)
        {
            await Clients.Caller.ErrorMsg("SERVER ERROR: " + e.Message + e.StackTrace);
        }

        private async Task RefreshGameState(string gameRoomId, params string[] userIds)
        {
            // Always refresh shared state
            await this.GetAllTableCard(gameRoomId);
            await this.GetCurrentPlayerTurn(gameRoomId);

            // Refresh specific player hands if provided
            foreach (var userId in userIds)
            {
                if (!string.IsNullOrEmpty(userId))
                {
                    await this.GetPlayerHand(gameRoomId, userId);
                }
            }
        }

        private async Task RefreshFullGameState(string gameRoomId, bool includeDiscardPile = false, params string[] userIds)
        {
            await RefreshGameState(gameRoomId, userIds);

            if (includeDiscardPile)
            {
                await this.GetLatestDiscardPileCard(gameRoomId);
            }
        }
        #endregion
    }
}
