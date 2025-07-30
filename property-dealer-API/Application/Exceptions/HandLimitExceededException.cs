namespace property_dealer_API.Application.Exceptions
{
    public class HandLimitExceededException : Exception
    {
        public int CurrentCardCount { get; }
        public int MaxAllowed { get; }
        public int ExcessCards { get; }

        public HandLimitExceededException(int currentCardCount, int maxAllowed = 7)
            : base($"Hand limit exceeded. You have {currentCardCount} cards but the limit is {maxAllowed}. Please discard {currentCardCount - maxAllowed} card(s).")
        {
            CurrentCardCount = currentCardCount;
            MaxAllowed = maxAllowed;
            ExcessCards = currentCardCount - maxAllowed;
        }
    }
}