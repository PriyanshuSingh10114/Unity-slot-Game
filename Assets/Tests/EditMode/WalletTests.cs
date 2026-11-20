using System;
using NUnit.Framework;
using SlotGame.Core;

namespace SlotGame.Tests.EditMode
{
    [TestFixture]
    public class WalletTests
    {
        [Test]
        public void InitialBalance_MatchesConstructorArgument()
        {
            var wallet = new SlotWallet(1000);
            Assert.AreEqual(1000, wallet.CurrentBalance);
        }

        [Test]
        public void Constructor_NegativeBalance_ThrowsException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new SlotWallet(-100));
        }

        [Test]
        public void TryDeductBet_ValidBet_DeductsExactlyOnceAndReturnsTrue()
        {
            var wallet = new SlotWallet(500);
            bool success = wallet.TryDeductBet(50);

            Assert.IsTrue(success);
            Assert.AreEqual(450, wallet.CurrentBalance);
        }

        [Test]
        public void TryDeductBet_InsufficientBalance_ReturnsFalseAndPreservesBalance()
        {
            var wallet = new SlotWallet(25);
            bool success = wallet.TryDeductBet(50);

            Assert.IsFalse(success);
            Assert.AreEqual(25, wallet.CurrentBalance);
        }

        [Test]
        public void TryDeductBet_ZeroOrNegativeBet_ReturnsFalseAndPreservesBalance()
        {
            var wallet = new SlotWallet(100);

            Assert.IsFalse(wallet.TryDeductBet(0));
            Assert.IsFalse(wallet.TryDeductBet(-25));
            Assert.AreEqual(100, wallet.CurrentBalance);
        }

        [Test]
        public void CreditWin_PositiveAmount_IncreasesBalanceExactlyOnce()
        {
            var wallet = new SlotWallet(200);
            wallet.CreditWin(150);

            Assert.AreEqual(350, wallet.CurrentBalance);
        }

        [Test]
        public void CreditWin_ZeroAmount_PreservesBalance()
        {
            var wallet = new SlotWallet(200);
            wallet.CreditWin(0);

            Assert.AreEqual(200, wallet.CurrentBalance);
        }

        [Test]
        public void CreditWin_NegativeAmount_ThrowsException()
        {
            var wallet = new SlotWallet(200);
            Assert.Throws<ArgumentOutOfRangeException>(() => wallet.CreditWin(-50));
        }

        [Test]
        public void BalanceNeverBecomesNegativeThroughNormalGameplay()
        {
            var wallet = new SlotWallet(100);

            // Attempt series of bets
            wallet.TryDeductBet(60); // 40 remaining
            wallet.TryDeductBet(60); // rejected, 40 remaining
            wallet.TryDeductBet(40); // 0 remaining
            wallet.TryDeductBet(1);  // rejected, 0 remaining

            Assert.AreEqual(0, wallet.CurrentBalance);
            Assert.IsFalse(wallet.CanAfford(1));
        }

        [Test]
        public void OnBalanceChangedEvent_FiresOnDeductAndCredit()
        {
            var wallet = new SlotWallet(100);
            int eventCallCount = 0;
            int lastReportedBalance = 0;

            wallet.OnBalanceChanged += (bal) =>
            {
                eventCallCount++;
                lastReportedBalance = bal;
            };

            wallet.TryDeductBet(30);
            Assert.AreEqual(1, eventCallCount);
            Assert.AreEqual(70, lastReportedBalance);

            wallet.CreditWin(50);
            Assert.AreEqual(2, eventCallCount);
            Assert.AreEqual(120, lastReportedBalance);
        }
    }
}
