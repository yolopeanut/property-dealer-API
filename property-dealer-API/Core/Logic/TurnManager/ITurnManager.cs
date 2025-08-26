namespace property_dealer_API.Core.Logic.TurnManager
{
    public interface ITurnManager
    {
        string GetCurrentUserTurn();
        void SetNextUsersTurn();
        void AddPlayer(string userId);
        void RemovePlayerFromQueue(string userId);
        string? IncrementUserActionCount();
        string PrematurelyEndCurrentUserTurn();
        int GetCurrentUserActionCount();
        int GetRemainingActionCounts();
    }
}
