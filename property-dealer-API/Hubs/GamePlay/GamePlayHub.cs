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
            Console.WriteLine($"=== GAMEPLAY ROOM CONNECTION ===");
            Console.WriteLine($"Room ID from query: '{gameRoomId}'");
            Console.WriteLine($"User ID from query: '{userId}'");

            // Further validation to see if the room and player actually exists
            var roomExists = this._gamePlayService.DoesRoomExist(gameRoomId);

            if (!roomExists)
            {
                await this.Clients.Caller.ErrorMsg("The game room you are trying to join does not exist.");
                this.Context.Abort();
                await base.OnConnectedAsync();
                return;
            }
            if (!this._gamePlayService.DoesPlayerExist(userId, gameRoomId))
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
            var player = this._gamePlayService.GetPlayerByUserId(gameRoomId, userId);
            await this.GetAllTableCard(gameRoomId);
            await this.GetLatestDiscardPileCard(gameRoomId);

        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            this.Context.Items.TryGetValue(GameRoomIdKey, out var gameRoomIdObj);
            this.Context.Items.TryGetValue(UserIdKey, out var userIdObj);

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
                var playerName = this._gamePlayService.RemovePlayerFromGame(gameRoomId, userId);

                if (!string.IsNullOrEmpty(playerName))
                {
                    await this.Clients.Group(gameRoomId).PlayerLeft(playerName);

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
                await this.Clients.Group(gameRoomId).AllGameRoomPlayerList(allPlayers);
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
                await this.Clients.Caller.PlayerHand(playerHand);
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
                await this.Clients.Group(gameRoomId).AllTableHands(allTableHands);
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
                if (discardedCard!= null)
                {
                    await this.Clients.Group(gameRoomId).LatestDiscardPileCard(discardedCard);
                }
            }
            catch (Exception e)
            {
                await this.ExceptionHandler(e);
            }
        }

        public async Task PlayCard(string gameRoomId, string userId, string cardId, CardDestinationEnum cardDestination, PropertyCardColoursEnum? cardColorDestinationEnum)
        {
            this._logger.LogInformation(
                   "--> PlayCard called with params: GameRoomId={GameRoomId}, UserId={UserId}, CardId={CardId}, Destination={Destination}, ColorDestination={ColorDestination}",
                   gameRoomId, userId, cardId, cardDestination, cardColorDestinationEnum);
            //Play user card (send to discard pile, show up on all players screen, remove from user hand, draw card)
            try
            {
                var result = this._gamePlayService.PlayCard(gameRoomId, userId, cardId, cardDestination, cardColorDestinationEnum);

                // Check if someone won
                if (result.GameEnded)
                {
                    await this.Clients.Group(gameRoomId).PlayerWon(result.WinningPlayer);
                    return; // Don't continue with normal flow if game ended
                }

                if (result.ActionContext != null)
                {
                    await this.Clients.Group(gameRoomId).OpenCommandDialog(result.ActionContext);
                }

                bool includeDiscardPile = cardDestination == CardDestinationEnum.CommandPile;
                await this.RefreshFullGameState(gameRoomId, includeDiscardPile, userId);
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
                await this.Clients.Group(gameRoomId).CurrentPlayerTurn(this._gamePlayService.GetCurrentPlayerTurn(gameRoomId));
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
                var result = this._gamePlayService.SendActionResponse(gameRoomId, userId, actionContext);

                // Check if someone won
                if (result.GameEnded)
                {
                    await this.Clients.Group(gameRoomId).PlayerWon(result.WinningPlayer);
                    return; // Don't continue with normal flow if game ended
                }

                await this.RefreshGameState(gameRoomId, actionContext.ActionInitiatingPlayerId, userId);

                // If there's a new dialog to open, send it to clients
                if (result.ActionContext != null)
                {
                    await this.Clients.Group(gameRoomId).OpenCommandDialog(result.ActionContext);
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
            await this.Clients.Group(gameRoomId).PlayerWon(player);
        }

        #endregion

        #region Helper methods

        private async Task ExceptionHandler(Exception e)
        {
            await this.Clients.Caller.ErrorMsg("SERVER ERROR: " + e.Message + e.StackTrace);
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
            await this.RefreshGameState(gameRoomId, userIds);

            if (includeDiscardPile)
            {
                await this.GetLatestDiscardPileCard(gameRoomId);
            }
        }
        #endregion
    }
}
