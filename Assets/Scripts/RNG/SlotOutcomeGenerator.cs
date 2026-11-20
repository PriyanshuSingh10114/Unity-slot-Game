using System;

namespace SlotGame.RNG
{
    /// <summary>
    /// Generates reel outcome arrays prior to any visual animation.
    /// Strictly guarantees outcome independence from visual timing and presentation.
    /// </summary>
    public sealed class SlotOutcomeGenerator
    {
        private readonly IRandomProvider _randomProvider;

        public SlotOutcomeGenerator(IRandomProvider randomProvider)
        {
            if (randomProvider == null)
            {
                throw new ArgumentNullException("randomProvider");
            }
            _randomProvider = randomProvider;
        }

        /// <summary>
        /// Generates an array of symbol indices, one per reel, using equal or weighted probabilities.
        /// </summary>
        /// <param name="reelCount">Number of reels (typically 3).</param>
        /// <param name="symbolCount">Total available symbols.</param>
        /// <returns>Integer array containing the target symbol index for each reel.</returns>
        public int[] GenerateOutcome(int reelCount, int symbolCount)
        {
            if (reelCount <= 0) throw new ArgumentOutOfRangeException("reelCount", "Reel count must be positive");
            if (symbolCount <= 0) throw new ArgumentOutOfRangeException("symbolCount", "Symbol count must be positive");

            int[] outcomes = new int[reelCount];
            for (int i = 0; i < reelCount; i++)
            {
                outcomes[i] = _randomProvider.NextInt(0, symbolCount);
            }
            return outcomes;
        }

        /// <summary>
        /// Generates outcome based on cumulative weights when non-uniform weighting is configured.
        /// </summary>
        public int[] GenerateWeightedOutcome(int reelCount, int[] weights)
        {
            if (weights == null || weights.Length == 0)
            {
                throw new ArgumentException("Weights array cannot be null or empty", "weights");
            }

            int totalWeight = 0;
            for (int i = 0; i < weights.Length; i++)
            {
                if (weights[i] < 0) throw new ArgumentException("Weights must be non-negative");
                totalWeight += weights[i];
            }

            if (totalWeight <= 0) throw new ArgumentException("Total weight must be positive");

            int[] outcomes = new int[reelCount];
            for (int r = 0; r < reelCount; r++)
            {
                int roll = _randomProvider.NextInt(0, totalWeight);
                int cumulative = 0;
                int selectedIndex = 0;
                for (int s = 0; s < weights.Length; s++)
                {
                    cumulative += weights[s];
                    if (roll < cumulative)
                    {
                        selectedIndex = s;
                        break;
                    }
                }
                outcomes[r] = selectedIndex;
            }
            return outcomes;
        }
    }
}
