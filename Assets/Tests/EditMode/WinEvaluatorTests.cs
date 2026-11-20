using NUnit.Framework;
using SlotGame.Payouts;

namespace SlotGame.Tests.EditMode
{
    [TestFixture]
    public class WinEvaluatorTests
    {
        private SlotWinEvaluator _evaluator;

        [SetUp]
        public void SetUp()
        {
            _evaluator = new SlotWinEvaluator();
        }

        [Test]
        public void ThreeIdenticalSymbols_ProducesWin()
        {
            // Test each symbol ID (0: Seven, 1: Cherry, 2: Bell, 3: BAR)
            for (int sym = 0; sym < 4; sym++)
            {
                int[] outcome = new int[] { sym, sym, sym };
                bool isWin = _evaluator.EvaluateWin(outcome, out int winningSym);

                Assert.IsTrue(isWin, $"Expected symbol {sym} to win.");
                Assert.AreEqual(sym, winningSym);
            }
        }

        [Test]
        public void TwoMatchingOneDifferent_ProducesLoss()
        {
            int[] outcome1 = new int[] { 0, 0, 1 };
            int[] outcome2 = new int[] { 1, 2, 1 };
            int[] outcome3 = new int[] { 3, 2, 2 };

            Assert.IsFalse(_evaluator.EvaluateWin(outcome1, out int win1));
            Assert.AreEqual(-1, win1);

            Assert.IsFalse(_evaluator.EvaluateWin(outcome2, out int win2));
            Assert.AreEqual(-1, win2);

            Assert.IsFalse(_evaluator.EvaluateWin(outcome3, out int win3));
            Assert.AreEqual(-1, win3);
        }

        [Test]
        public void AllDifferentSymbols_ProducesLoss()
        {
            int[] outcome = new int[] { 0, 1, 2 };
            bool isWin = _evaluator.EvaluateWin(outcome, out int winningSym);

            Assert.IsFalse(isWin);
            Assert.AreEqual(-1, winningSym);
        }

        [Test]
        public void NullOrEmptyOutcome_HandledSafely()
        {
            Assert.IsFalse(_evaluator.EvaluateWin(null, out int win1));
            Assert.AreEqual(-1, win1);

            Assert.IsFalse(_evaluator.EvaluateWin(new int[0], out int win2));
            Assert.AreEqual(-1, win2);
        }
    }
}
