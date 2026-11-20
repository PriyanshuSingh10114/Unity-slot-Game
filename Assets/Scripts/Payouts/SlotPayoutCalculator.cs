using System;
using System.Collections.Generic;

namespace SlotGame.Payouts
{
    /// <summary>
    /// Configurable payout table calculator.
    /// Calculates reward credits based on bet amount and symbol multiplier.
    /// </summary>
    public sealed class SlotPayoutCalculator
    {
        private readonly Dictionary<int, int> _multipliers = new Dictionary<int, int>();

        public Dictionary<int, int> Multipliers
        {
            get { return _multipliers; }
        }

        public SlotPayoutCalculator()
        {
        }

        public SlotPayoutCalculator(IDictionary<int, int> payoutTable)
        {
            if (payoutTable != null)
            {
                foreach (var kvp in payoutTable)
                {
                    SetMultiplier(kvp.Key, kvp.Value);
                }
            }
        }

        /// <summary>
        /// Registers or updates the winning multiplier for a specific symbol.
        /// </summary>
        public void SetMultiplier(int symbolId, int multiplier)
        {
            if (multiplier < 0)
            {
                throw new ArgumentOutOfRangeException("multiplier", "Multiplier cannot be negative.");
            }
            _multipliers[symbolId] = multiplier;
        }

        /// <summary>
        /// Retrieves the multiplier for a symbol ID. Defaults to 0 if not registered.
        /// </summary>
        public int GetMultiplier(int symbolId)
        {
            int mult;
            return _multipliers.TryGetValue(symbolId, out mult) ? mult : 0;
        }

        /// <summary>
        /// Computes final payout: Payout = Current Bet * Winning Multiplier.
        /// </summary>
        public int CalculatePayout(int betAmount, int winningSymbolId)
        {
            if (betAmount <= 0 || winningSymbolId < 0)
            {
                return 0;
            }

            int multiplier = GetMultiplier(winningSymbolId);
            return checked(betAmount * multiplier);
        }
    }
}
