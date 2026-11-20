using NUnit.Framework;
using SlotGame.Payouts;

namespace SlotGame.Tests.EditMode
{
    [TestFixture]
    public class PayoutCalculatorTests
    {
        private SlotPayoutCalculator _calculator;

        [SetUp]
        public void SetUp()
        {
            _calculator = new SlotPayoutCalculator();
            _calculator.SetMultiplier(0, 25); // Seven: 25x
            _calculator.SetMultiplier(1, 3);  // Cherry: 3x
            _calculator.SetMultiplier(2, 5);  // Bell: 5x
            _calculator.SetMultiplier(3, 10); // BAR: 10x
        }

        [Test]
        [TestCase(10, 0, 250)]  // Bet 10, Seven (25x) = 250
        [TestCase(25, 0, 625)]  // Bet 25, Seven (25x) = 625
        [TestCase(50, 1, 150)]  // Bet 50, Cherry (3x) = 150
        [TestCase(100, 2, 500)] // Bet 100, Bell (5x) = 500
        [TestCase(10, 3, 100)]  // Bet 10, BAR (10x) = 100
        public void CalculatePayout_MatchesExpectedFormula(int bet, int symbolId, int expectedPayout)
        {
            int payout = _calculator.CalculatePayout(bet, symbolId);
            Assert.AreEqual(expectedPayout, payout);
        }

        [Test]
        public void CalculatePayout_ZeroOrNegativeBet_ReturnsZero()
        {
            Assert.AreEqual(0, _calculator.CalculatePayout(0, 1));
            Assert.AreEqual(0, _calculator.CalculatePayout(-10, 1));
        }

        [Test]
        public void CalculatePayout_UnregisteredSymbol_ReturnsZero()
        {
            int payout = _calculator.CalculatePayout(50, 999);
            Assert.AreEqual(0, payout);
        }

        [Test]
        public void CalculatePayout_NegativeSymbolId_ReturnsZero()
        {
            int payout = _calculator.CalculatePayout(50, -1);
            Assert.AreEqual(0, payout);
        }
    }
}
