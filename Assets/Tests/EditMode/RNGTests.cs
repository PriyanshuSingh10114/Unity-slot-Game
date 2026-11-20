using System;
using NUnit.Framework;
using SlotGame.RNG;

namespace SlotGame.Tests.EditMode
{
    [TestFixture]
    public class RNGTests
    {
        [Test]
        public void CryptoRandomProvider_GeneratesValuesStrictlyWithinRange()
        {
            using (var rng = new CryptoRandomProvider())
            {
                int min = 0;
                int max = 4;
                for (int i = 0; i < 2000; i++)
                {
                    int val = rng.NextInt(min, max);
                    Assert.GreaterOrEqual(val, min);
                    Assert.Less(val, max);
                }
            }
        }

        [Test]
        public void DeterministicRandomProvider_WithSameSeed_ProducesIdenticalSequence()
        {
            var rng1 = new DeterministicRandomProvider(12345);
            var rng2 = new DeterministicRandomProvider(12345);

            for (int i = 0; i < 100; i++)
            {
                Assert.AreEqual(rng1.NextInt(0, 100), rng2.NextInt(0, 100));
            }
        }

        [Test]
        public void SlotOutcomeGenerator_GeneratesCorrectReelCountAndBoundedIndices()
        {
            var deterministicRng = new DeterministicRandomProvider(42);
            var generator = new SlotOutcomeGenerator(deterministicRng);

            int reelCount = 3;
            int symbolCount = 4;

            for (int trial = 0; trial < 100; trial++)
            {
                int[] outcome = generator.GenerateOutcome(reelCount, symbolCount);
                Assert.IsNotNull(outcome);
                Assert.AreEqual(reelCount, outcome.Length);

                for (int r = 0; r < reelCount; r++)
                {
                    Assert.GreaterOrEqual(outcome[r], 0);
                    Assert.Less(outcome[r], symbolCount);
                }
            }
        }

        [Test]
        public void SlotOutcomeGenerator_InvalidArguments_ThrowsException()
        {
            var deterministicRng = new DeterministicRandomProvider(42);
            var generator = new SlotOutcomeGenerator(deterministicRng);

            Assert.Throws<ArgumentOutOfRangeException>(() => generator.GenerateOutcome(0, 4));
            Assert.Throws<ArgumentOutOfRangeException>(() => generator.GenerateOutcome(3, 0));
        }
    }
}
