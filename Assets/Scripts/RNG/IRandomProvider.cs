using System;

namespace SlotGame.RNG
{
    /// <summary>
    /// Contract for random number generation in the slot machine.
    /// Decouples game logic from specific RNG implementations to support deterministic testing.
    /// </summary>
    public interface IRandomProvider
    {
        /// <summary>
        /// Generates a random integer in the range [minInclusive, maxExclusive).
        /// </summary>
        int NextInt(int minInclusive, int maxExclusive);
    }
}
