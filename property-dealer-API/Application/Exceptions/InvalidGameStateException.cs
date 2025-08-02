using property_dealer_API.Models.Enums;

namespace property_dealer_API.Application.Exceptions
{
    public class InvalidGameStateException : Exception
    {
        public GameStateEnum CurrentState { get; }
        public GameStateEnum RequiredState { get; }

        public InvalidGameStateException(GameStateEnum currentState, GameStateEnum requiredState, string action)
            : base($"Cannot {action} when game is in {currentState} state. Game must be in {requiredState} state.")
        {
            this.CurrentState = currentState;
            this.RequiredState = requiredState;
        }
    }
}