namespace SlotGame.Core
{
    /// <summary>
    /// Explicit state machine states for the slot machine lifecycle.
    /// </summary>
    public enum SlotGameState
    {
        Ready,
        Spinning,
        Evaluating,
        ShowingResult,
        InsufficientBalance
    }
}
