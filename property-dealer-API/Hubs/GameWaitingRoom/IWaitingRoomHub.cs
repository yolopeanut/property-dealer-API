namespace property_dealer_API.Hubs.GameWaitingRoom
{
    public interface IWaitingRoomHub
    {
        Task AllGameRoomPlayerList();
        Task GameRoomCfg();
    }
}
