using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SlotGame.Audio;
using SlotGame.Payouts;
using SlotGame.Reels;
using SlotGame.RNG;
using SlotGame.UI;

namespace SlotGame.Core
{
    /// <summary>
    /// Master coordinator for slot machine gameplay, state machine, wallet, and visual orchestration.
    /// Follows the Single Responsibility Principle and guarantees atomic balance and payout operations.
    /// </summary>
    public class SlotGameManager : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private SlotGameConfig _config;

        [Header("Subsystem References")]
        [SerializeField] private SlotReelController[] _reels;
        [SerializeField] private SlotUIController _ui;
        [SerializeField] private SlotAudioManager _audio;

        private SlotWallet _wallet;
        private IRandomProvider _randomProvider;
        private SlotOutcomeGenerator _outcomeGenerator;
        private SlotWinEvaluator _winEvaluator;
        private SlotPayoutCalculator _payoutCalculator;

        private SlotGameState _currentState = SlotGameState.Ready;
        private int _currentBetIndex = 0;
        private bool _isAutoSpinning = false;
        private Coroutine _activeSpinLifecycleCoroutine;
        private Coroutine _autoSpinCoroutine;

        public SlotGameState CurrentState
        {
            get { return _currentState; }
        }

        public SlotWallet Wallet
        {
            get { return _wallet; }
        }

        public int CurrentBet
        {
            get
            {
                return (_config != null && _config.BetOptions != null && _config.BetOptions.Length > 0)
                    ? _config.BetOptions[_currentBetIndex]
                    : 10;
            }
        }

        public event Action<SlotGameState> OnStateChanged;
        public event Action<int[]> OnSpinCompleted;

        private void Awake()
        {
            InitializeSubsystems();
        }

        private void Start()
        {
            SetupUIEventListeners();
            UpdateInitialUI();
            SetState(SlotGameState.Ready);
        }

        private void InitializeSubsystems()
        {
            if (_config == null)
            {
                _config = ScriptableObject.CreateInstance<SlotGameConfig>();
            }

            _wallet = new SlotWallet(_config.StartingBalance);
            _randomProvider = new CryptoRandomProvider();
            _outcomeGenerator = new SlotOutcomeGenerator(_randomProvider);
            _winEvaluator = new SlotWinEvaluator();
            _payoutCalculator = new SlotPayoutCalculator();

            if (_config.Symbols != null)
            {
                for (int i = 0; i < _config.Symbols.Length; i++)
                {
                    var s = _config.Symbols[i];
                    if (s != null)
                    {
                        _payoutCalculator.SetMultiplier(s.SymbolId, s.PayoutMultiplier);
                    }
                }
            }

            if (_reels != null)
            {
                for (int i = 0; i < _reels.Length; i++)
                {
                    if (_reels[i] != null)
                    {
                        _reels[i].Initialize(i, _config.Symbols, _config.SymbolHeight, _config.SpinSpeed);
                    }
                }
            }

            _currentBetIndex = _config.DefaultBetIndex;
        }

        private void SetupUIEventListeners()
        {
            if (_ui == null) return;

            _ui.OnSpinClicked += HandleSpinRequest;
            _ui.OnBetMinusClicked += HandleBetMinus;
            _ui.OnBetPlusClicked += HandleBetPlus;
            _ui.OnAutoSpinToggled += HandleAutoSpinToggle;
            _ui.OnPaytableClicked += HandlePaytableToggle;
            _ui.OnMuteToggled += HandleMuteToggle;
        }

        private void UpdateInitialUI()
        {
            if (_ui == null) return;
            _ui.SetBalanceInstant(_wallet.CurrentBalance);
            _ui.SetBetDisplay(CurrentBet);
            _ui.SetWinDisplay(0);
            _ui.SetStatusMessage("PRESS SPIN TO PLAY!");
            _ui.SetAutoSpinState(false);
            if (_audio != null)
            {
                _ui.SetMuteState(_audio.IsMuted);
            }
        }

        public void HandleSpinRequest()
        {
            if (_currentState != SlotGameState.Ready && _currentState != SlotGameState.ShowingResult)
            {
                return;
            }

            int bet = CurrentBet;
            if (!_wallet.CanAfford(bet))
            {
                SetState(SlotGameState.InsufficientBalance);
                _ui.SetStatusMessage("INSUFFICIENT CREDITS! LOWER YOUR BET.");
                if (_audio != null) _audio.PlayLossSound();
                StopAutoSpin();
                return;
            }

            if (_activeSpinLifecycleCoroutine != null)
            {
                StopCoroutine(_activeSpinLifecycleCoroutine);
            }

            _activeSpinLifecycleCoroutine = StartCoroutine(ExecuteSpinLifecycle(bet));
        }

        private IEnumerator ExecuteSpinLifecycle(int bet)
        {
            SetState(SlotGameState.Spinning);

            bool deducted = _wallet.TryDeductBet(bet);
            if (!deducted)
            {
                SetState(SlotGameState.InsufficientBalance);
                _ui.SetStatusMessage("INSUFFICIENT CREDITS!");
                yield break;
            }

            _ui.SetBalanceInstant(_wallet.CurrentBalance);
            _ui.SetWinDisplay(0);
            _ui.SetStatusMessage("GOOD LUCK! REELS SPINNING...");
            _ui.SetSpinButtonInteractable(false);
            _ui.SetBetControlsInteractable(false);

            if (_audio != null)
            {
                _audio.PlayButtonClick();
                _audio.PlayReelSpinLoop();
            }

            int reelCount = _reels != null ? _reels.Length : 3;
            int symbolCount = _config.Symbols != null && _config.Symbols.Length > 0 ? _config.Symbols.Length : 4;
            int[] outcomes = _outcomeGenerator.GenerateOutcome(reelCount, symbolCount);

            for (int i = 0; i < reelCount; i++)
            {
                if (_reels[i] != null)
                {
                    _reels[i].StartSpin();
                }
            }

            yield return new WaitForSeconds(_config.MinSpinDuration);

            for (int i = 0; i < reelCount; i++)
            {
                if (_reels[i] != null)
                {
                    _reels[i].StopAtSymbol(outcomes[i]);
                }

                yield return new WaitForSeconds(_config.ReelStopStaggerDelay);
            }

            bool anySpinning = true;
            while (anySpinning)
            {
                anySpinning = false;
                for (int i = 0; i < reelCount; i++)
                {
                    if (_reels[i] != null && _reels[i].IsSpinning)
                    {
                        anySpinning = true;
                        break;
                    }
                }
                yield return null;
            }

            if (_audio != null)
            {
                _audio.StopReelSpinLoop();
                _audio.PlayReelStop();
            }

            SetState(SlotGameState.Evaluating);
            yield return new WaitForSeconds(0.2f);

            int winningSymbolId;
            bool isWin = _winEvaluator.EvaluateWin(outcomes, out winningSymbolId);
            int winAmount = 0;

            if (isWin)
            {
                winAmount = _payoutCalculator.CalculatePayout(bet, winningSymbolId);
                _wallet.CreditWin(winAmount);

                string symbolName = GetSymbolName(winningSymbolId);
                int multiplier = _payoutCalculator.GetMultiplier(winningSymbolId);

                _ui.SetWinDisplay(winAmount);
                _ui.AnimateBalanceTo(_wallet.CurrentBalance);

                if (multiplier >= 20)
                {
                    _ui.SetStatusMessage(string.Format("*** MEGA JACKPOT! 3x {0}! +{1:N0} CREDITS! ***", symbolName, winAmount));
                    _ui.ShowWinBanner(string.Format("MEGA JACKPOT!\n+{0:N0} CREDITS!", winAmount));
                    _ui.ShowWinPopup("MEGA JACKPOT WINNER!", winAmount);
                    if (_audio != null) _audio.PlayJackpotFanfare();
                }
                else
                {
                    _ui.SetStatusMessage(string.Format("WINNER! 3x {0}! +{1:N0} CREDITS ({2}x)!", symbolName, winAmount, multiplier));
                    _ui.ShowWinBanner(string.Format("WINNER!\n+{0:N0} CREDITS!", winAmount));
                    if (_audio != null) _audio.PlayWinChime();
                }
            }
            else
            {
                _ui.SetStatusMessage("TRY AGAIN! SPIN FOR 3 MATCHES.");
                if (_audio != null) _audio.PlayLossSound();
            }

            SetState(SlotGameState.ShowingResult);
            if (OnSpinCompleted != null)
            {
                OnSpinCompleted(outcomes);
            }

            _ui.SetBetControlsInteractable(true);
            _ui.SetSpinButtonInteractable(true);

            _activeSpinLifecycleCoroutine = null;

            if (!_isAutoSpinning)
            {
                SetState(SlotGameState.Ready);
            }
        }

        private string GetSymbolName(int symbolId)
        {
            if (_config.Symbols != null)
            {
                for (int i = 0; i < _config.Symbols.Length; i++)
                {
                    var s = _config.Symbols[i];
                    if (s != null && s.SymbolId == symbolId)
                    {
                        return s.SymbolName.ToUpperInvariant();
                    }
                }
            }
            return string.Format("SYMBOL {0}", symbolId);
        }

        private void HandleBetMinus()
        {
            if (_currentState == SlotGameState.Spinning) return;
            if (_currentBetIndex > 0)
            {
                _currentBetIndex--;
                if (_ui != null) _ui.SetBetDisplay(CurrentBet);
                if (_audio != null) _audio.PlayButtonClick();
            }
        }

        private void HandleBetPlus()
        {
            if (_currentState == SlotGameState.Spinning) return;
            if (_config.BetOptions != null && _currentBetIndex < _config.BetOptions.Length - 1)
            {
                _currentBetIndex++;
                if (_ui != null) _ui.SetBetDisplay(CurrentBet);
                if (_audio != null) _audio.PlayButtonClick();
            }
        }

        private void HandleAutoSpinToggle()
        {
            if (_audio != null) _audio.PlayButtonClick();
            if (_isAutoSpinning)
            {
                StopAutoSpin();
            }
            else
            {
                StartAutoSpin();
            }
        }

        private void StartAutoSpin()
        {
            _isAutoSpinning = true;
            if (_ui != null) _ui.SetAutoSpinState(true);
            if (_autoSpinCoroutine != null) StopCoroutine(_autoSpinCoroutine);
            _autoSpinCoroutine = StartCoroutine(AutoSpinRoutine());
        }

        private void StopAutoSpin()
        {
            _isAutoSpinning = false;
            if (_ui != null) _ui.SetAutoSpinState(false);
            if (_autoSpinCoroutine != null)
            {
                StopCoroutine(_autoSpinCoroutine);
                _autoSpinCoroutine = null;
            }
        }

        private IEnumerator AutoSpinRoutine()
        {
            while (_isAutoSpinning)
            {
                if (_currentState == SlotGameState.Ready || _currentState == SlotGameState.ShowingResult)
                {
                    if (!_wallet.CanAfford(CurrentBet))
                    {
                        StopAutoSpin();
                        if (_ui != null) _ui.SetStatusMessage("AUTO-SPIN STOPPED: INSUFFICIENT CREDITS.");
                        break;
                    }
                    HandleSpinRequest();
                }
                yield return new WaitForSeconds(0.8f);
            }
        }

        private void HandlePaytableToggle()
        {
            if (_audio != null) _audio.PlayButtonClick();
            if (_ui != null) _ui.TogglePaytableModal();
        }

        private void HandleMuteToggle()
        {
            if (_audio != null)
            {
                _audio.ToggleMute();
                if (_ui != null) _ui.SetMuteState(_audio.IsMuted);
            }
        }

        private void SetState(SlotGameState newState)
        {
            _currentState = newState;
            if (OnStateChanged != null)
            {
                OnStateChanged(_currentState);
            }
        }

        private void OnDestroy()
        {
            IDisposable disp = _randomProvider as IDisposable;
            if (disp != null)
            {
                disp.Dispose();
            }
        }
    }
}
