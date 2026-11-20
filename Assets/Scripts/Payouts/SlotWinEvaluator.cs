using System;

namespace SlotGame.Payouts
{
    /// <summary>
    /// Evaluates reel outcomes to determine whether a winning combination was landed.
    /// In this classic 3-reel slot game, a win is awarded strictly when all reels match the exact same symbol.
    /// </summary>
    public sealed class SlotWinEvaluator
    {
        /// <summary>
        /// Evaluates whether the given reel outcome forms a winning match.
        /// </summary>
        /// <param name="outcomeSymbolIds">Array of symbol IDs for each reel.</param>
        /// <param name="winningSymbolId">The symbol ID that matched across all reels, or -1 if loss.</param>
        /// <returns>True if all symbols match; false otherwise.</returns>
        public bool EvaluateWin(int[] outcomeSymbolIds, out int winningSymbolId)
        {
            winningSymbolId = -1;

            if (outcomeSymbolIds == null || outcomeSymbolIds.Length == 0)
            {
                return false;
            }

            int first = outcomeSymbolIds[0];
            for (int i = 1; i < outcomeSymbolIds.Length; i++)
            {
                if (outcomeSymbolIds[i] != first)
                {
                    return false;
                }
            }

            winningSymbolId = first;
            return true;
        }
    }
}
