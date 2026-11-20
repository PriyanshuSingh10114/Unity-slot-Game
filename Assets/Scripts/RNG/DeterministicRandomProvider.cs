using System;

namespace SlotGame.RNG
{
    /// <summary>
    /// Seeded, deterministic pseudo-random provider designed for testing and repeatable verification.
    /// </summary>
    public sealed class DeterministicRandomProvider : IRandomProvider
    {
        private readonly System.Random _random;

        public DeterministicRandomProvider(int seed)
        {
            _random = new System.Random(seed);
        }

        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (minInclusive >= maxExclusive)
            {
                throw new ArgumentException("minInclusive must be strictly less than maxExclusive");
            }
            return _random.Next(minInclusive, maxExclusive);
        }
    }
}
