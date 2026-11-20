using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SlotGame.UI
{
    /// <summary>
    /// Master UI controller managing displays, controls, animations, and modal dialogs.
    /// Supports smooth balance rolling animations, win banners, bet stepping, and popups.
    /// </summary>
    public class SlotUIController : MonoBehaviour
    {
        [Header("Balance & Bet Displays")]
        [SerializeField] private Text _balanceText;
        [SerializeField] private Text _betText;
        [SerializeField] private Text _winAmountText;
        [SerializeField] private Text _messageText;

        [Header("Control Buttons")]
        [SerializeField] private Button _spinButton;
        [SerializeField] private Button _betMinusButton;
        [SerializeField] private Button _betPlusButton;
        [SerializeField] private Button _autoSpinButton;
        [SerializeField] private Text _autoSpinButtonText;
        [SerializeField] private Button _paytableButton;
        [SerializeField] private Button _muteButton;
        [SerializeField] private Text _muteButtonText;

        [Header("Win Popup Modal")]
        [SerializeField] private GameObject _winPopupRoot;
        [SerializeField] private Text _winPopupTitleText;
        [SerializeField] private Text _winPopupAmountText;
        [SerializeField] private Button _winPopupCloseButton;
        [SerializeField] private Button _winPopupOkButton;

        [Header("Paytable Modal")]
        [SerializeField] private GameObject _paytableModalRoot;
        [SerializeField] private Button _paytableCloseButton;

        [Header("Win Celebratory Banner")]
        [SerializeField] private GameObject _winBannerRoot;
        [SerializeField] private Text _winBannerText;

        private Coroutine _balanceRollCoroutine;
        private int _displayedBalance;

        public event Action OnSpinClicked;
        public event Action OnBetMinusClicked;
        public event Action OnBetPlusClicked;
        public event Action OnAutoSpinToggled;
        public event Action OnPaytableClicked;
        public event Action OnMuteToggled;

        private void Awake()
        {
            if (_spinButton != null) _spinButton.onClick.AddListener(() => OnSpinClicked?.Invoke());
            if (_betMinusButton != null) _betMinusButton.onClick.AddListener(() => OnBetMinusClicked?.Invoke());
            if (_betPlusButton != null) _betPlusButton.onClick.AddListener(() => OnBetPlusClicked?.Invoke());
            if (_autoSpinButton != null) _autoSpinButton.onClick.AddListener(() => OnAutoSpinToggled?.Invoke());
            if (_paytableButton != null) _paytableButton.onClick.AddListener(() => OnPaytableClicked?.Invoke());
            if (_muteButton != null) _muteButton.onClick.AddListener(() => OnMuteToggled?.Invoke());

            if (_winPopupCloseButton != null) _winPopupCloseButton.onClick.AddListener(HideWinPopup);
            if (_winPopupOkButton != null) _winPopupOkButton.onClick.AddListener(HideWinPopup);
            if (_paytableCloseButton != null) _paytableCloseButton.onClick.AddListener(HidePaytableModal);

            HideWinPopup();
            HidePaytableModal();
            HideWinBanner();
        }

        public void SetBalanceInstant(int balance)
        {
            if (_balanceRollCoroutine != null)
            {
                StopCoroutine(_balanceRollCoroutine);
                _balanceRollCoroutine = null;
            }
            _displayedBalance = balance;
            if (_balanceText != null)
            {
                _balanceText.text = $"CREDITS: {balance:N0}";
            }
        }

        public void AnimateBalanceTo(int targetBalance, float duration = 0.8f)
        {
            if (_balanceRollCoroutine != null)
            {
                StopCoroutine(_balanceRollCoroutine);
            }
            _balanceRollCoroutine = StartCoroutine(RollBalanceRoutine(_displayedBalance, targetBalance, duration));
        }

        private IEnumerator RollBalanceRoutine(int from, int to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                _displayedBalance = (int)Mathf.Lerp(from, to, t);
                if (_balanceText != null)
                {
                    _balanceText.text = $"CREDITS: {_displayedBalance:N0}";
                }
                yield return null;
            }
            _displayedBalance = to;
            if (_balanceText != null)
            {
                _balanceText.text = $"CREDITS: {to:N0}";
            }
            _balanceRollCoroutine = null;
        }

        public void SetBetDisplay(int betAmount)
        {
            if (_betText != null)
            {
                _betText.text = $"BET: {betAmount}";
            }
        }

        public void SetWinDisplay(int winAmount)
        {
            if (_winAmountText != null)
            {
                _winAmountText.text = winAmount > 0 ? $"WIN: {winAmount:N0}" : "WIN: 0";
            }
        }

        public void SetStatusMessage(string message)
        {
            if (_messageText != null)
            {
                _messageText.text = message;
            }
        }

        public void SetSpinButtonInteractable(bool interactable)
        {
            if (_spinButton != null)
            {
                _spinButton.interactable = interactable;
            }
        }

        public void SetBetControlsInteractable(bool interactable)
        {
            if (_betMinusButton != null) _betMinusButton.interactable = interactable;
            if (_betPlusButton != null) _betPlusButton.interactable = interactable;
        }

        public void SetAutoSpinState(bool isAutoSpinning)
        {
            if (_autoSpinButtonText != null)
            {
                _autoSpinButtonText.text = isAutoSpinning ? "STOP AUTO" : "AUTO SPIN";
            }
        }

        public void SetMuteState(bool isMuted)
        {
            if (_muteButtonText != null)
            {
                _muteButtonText.text = isMuted ? "SOUND: OFF" : "SOUND: ON";
            }
        }

        public void ShowWinBanner(string text, float duration = 2.5f)
        {
            if (_winBannerRoot != null)
            {
                _winBannerRoot.SetActive(true);
                if (_winBannerText != null) _winBannerText.text = text;
                StartCoroutine(AutoDismissBanner(duration));
            }
        }

        private IEnumerator AutoDismissBanner(float duration)
        {
            yield return new WaitForSeconds(duration);
            HideWinBanner();
        }

        public void HideWinBanner()
        {
            if (_winBannerRoot != null) _winBannerRoot.SetActive(false);
        }

        public void ShowWinPopup(string title, int amountWon)
        {
            if (_winPopupRoot != null)
            {
                _winPopupRoot.SetActive(true);
                if (_winPopupTitleText != null) _winPopupTitleText.text = title;
                if (_winPopupAmountText != null) _winPopupAmountText.text = $"+{amountWon:N0} CREDITS";
            }
        }

        public void HideWinPopup()
        {
            if (_winPopupRoot != null) _winPopupRoot.SetActive(false);
        }

        public void ShowPaytableModal()
        {
            if (_paytableModalRoot != null) _paytableModalRoot.SetActive(true);
        }

        public void HidePaytableModal()
        {
            if (_paytableModalRoot != null) _paytableModalRoot.SetActive(false);
        }

        public void TogglePaytableModal()
        {
            if (_paytableModalRoot != null)
            {
                bool active = _paytableModalRoot.activeSelf;
                _paytableModalRoot.SetActive(!active);
            }
        }
    }
}
