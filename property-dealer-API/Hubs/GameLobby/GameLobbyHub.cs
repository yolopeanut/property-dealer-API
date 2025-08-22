using Microsoft.AspNetCore.SignalR;
using property_dealer_API.Application.DTOs.Responses;
using property_dealer_API.Application.Exceptions;
using property_dealer_API.Core.Entities;
using property_dealer_API.Hubs.GameLobby.Service;
using property_dealer_API.Models.Enums;

namespace property_dealer_API.Hubs.GameLobby
{
    public class GameLobbyHub : Hub<IGameLobbyHubClient>, IGameLobbyHubServer
    {
        private readonly IGameLobbyHubService _gameLobbyHubService;

        public GameLobbyHub(IGameLobbyHubService gameLobbyHubService)
        {
            this._gameLobbyHubService = gameLobbyHubService;
        }

        public override async Task OnConnectedAsync()
        {
            await this.GetAllGameLobbySummaries();
        }

        public async Task GetAllGameLobbySummaries()
        {
            var summaries = this._gameLobbyHubService.GetGameListSummary();

            await this.Clients.All.GetAllLobbySummary(summaries);
        }

        public async Task CreateGameRoom(
            string userId,
            string playerName,
            string roomName,
            GameConfig createGameConfig
        )
        {
            try
            {
                // Create Room
                var roomIdCreated = this._gameLobbyHubService.CreateRoom(
                    userId,
                    playerName,
                    roomName,
                    createGameConfig
                );
                await this.Clients.Caller.CreateGameRoomId(roomIdCreated);

                // Broadcast summaries
                var summaries = this._gameLobbyHubService.GetGameListSummary();
                await this.Clients.All.GetAllLobbySummary(summaries);
            }
            catch (Exception ex)
            {
                throw; // Important: re-throw so client gets the error
            }
        }

        public async Task JoinGameRoom(string userId, string playerName, string gameRoomId)
        {
            try
            {
                // Joining room
                var response = this._gameLobbyHubService.JoinRoom(gameRoomId, userId, playerName);
                await this.Clients.Caller.JoinGameRoomStatus(
                    new JoinGameResponse(response, gameRoomId)
                );

                // Broadcasting to all lobby status
                var summaries = this._gameLobbyHubService.GetGameListSummary();
                await this.Clients.All.GetAllLobbySummary(summaries);
            }
            catch (GameNotFoundException)
            {
                await this.Clients.Caller.JoinGameRoomStatus(
                    new JoinGameResponse(JoinGameResponseEnum.FailedToJoin, gameRoomId)
                );
            }
        }
    }
}
