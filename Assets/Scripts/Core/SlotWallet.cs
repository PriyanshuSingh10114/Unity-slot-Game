using System;

namespace SlotGame.Core
{
    /// <summary>
    /// Thread-safe and atomic simulated wallet managing integer player credits.
    /// Strictly prevents floating-point inaccuracy, negative balances, and unauthorized adjustments.
    /// </summary>
    public sealed class SlotWallet
    {
        private int _balance;
        private readonly object _lock = new object();

        public event Action<int> OnBalanceChanged;

        public int CurrentBalance
        {
            get
            {
                lock (_lock)
                {
                    return _balance;
                }
            }
        }

        public SlotWallet(int initialBalance)
        {
            if (initialBalance < 0)
            {
                throw new ArgumentOutOfRangeException("initialBalance", "Initial balance cannot be negative.");
            }
            _balance = initialBalance;
        }

        /// <summary>
        /// Checks whether the player has enough balance to cover the specified bet.
        /// </summary>
        public bool CanAfford(int betAmount)
        {
            if (betAmount <= 0) return false;
            lock (_lock)
            {
                return _balance >= betAmount;
            }
        }

        /// <summary>
        /// Atomically deducts the bet amount if sufficient funds exist.
        /// Rejects zero or negative bets. Returns true only if deduction succeeds.
        /// </summary>
        public bool TryDeductBet(int betAmount)
        {
            if (betAmount <= 0)
            {
                return false;
            }

            int newBalance;
            lock (_lock)
            {
                if (_balance < betAmount)
                {
                    return false;
                }

                _balance -= betAmount;
                newBalance = _balance;
            }

            if (OnBalanceChanged != null)
            {
                OnBalanceChanged(newBalance);
            }
            return true;
        }

        /// <summary>
        /// Atomically credits winnings to the player's balance.
        /// </summary>
        public void CreditWin(int winAmount)
        {
            if (winAmount < 0)
            {
                throw new ArgumentOutOfRangeException("winAmount", "Winnings credit amount cannot be negative.");
            }

            if (winAmount == 0)
            {
                return;
            }

            int newBalance;
            lock (_lock)
            {
                checked
                {
                    _balance += winAmount;
                }
                newBalance = _balance;
            }

            if (OnBalanceChanged != null)
            {
                OnBalanceChanged(newBalance);
            }
        }

        /// <summary>
        /// Resets the wallet balance to a specified non-negative amount.
        /// </summary>
        public void Reset(int balance)
        {
            if (balance < 0) throw new ArgumentOutOfRangeException("balance", "Balance cannot be negative.");
            lock (_lock)
            {
                _balance = balance;
            }
            if (OnBalanceChanged != null)
            {
                OnBalanceChanged(_balance);
            }
        }
    }
}
