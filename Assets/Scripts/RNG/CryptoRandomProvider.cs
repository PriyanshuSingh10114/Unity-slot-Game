using System;
using System.Security.Cryptography;

namespace SlotGame.RNG
{
    /// <summary>
    /// Production RNG implementation using cryptographically secure random bytes.
    /// Employs rejection sampling to completely eliminate modulo bias for bounded random ranges.
    /// </summary>
    public sealed class CryptoRandomProvider : IRandomProvider, IDisposable
    {
        private readonly RandomNumberGenerator _rng;
        private readonly byte[] _uintBuffer = new byte[4];

        public CryptoRandomProvider()
        {
            _rng = RandomNumberGenerator.Create();
        }

        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (minInclusive >= maxExclusive)
            {
                throw new ArgumentException("minInclusive must be strictly less than maxExclusive");
            }

            long range = (long)maxExclusive - minInclusive;
            if (range == 1)
            {
                return minInclusive;
            }

            // Rejection sampling threshold: max multiple of range that fits in uint32
            long maxUnbiased = (1L << 32) - ((1L << 32) % range);

            uint sample;
            do
            {
                _rng.GetBytes(_uintBuffer);
                sample = BitConverter.ToUInt32(_uintBuffer, 0);
            } while (sample >= maxUnbiased);

            return (int)(minInclusive + (sample % range));
        }

        public void Dispose()
        {
            IDisposable disp = _rng as IDisposable;
            if (disp != null)
            {
                disp.Dispose();
            }
        }
    }
}
