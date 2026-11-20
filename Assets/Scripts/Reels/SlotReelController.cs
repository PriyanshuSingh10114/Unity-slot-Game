using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SlotGame.Core;

namespace SlotGame.Reels
{
    /// <summary>
    /// Reusable controller for a single slot reel.
    /// Manages continuous vertical spinning, infinite symbol recycling, staggered deceleration,
    /// and precise symbol alignment at the center payline with smooth overshoot/bounce easing.
    /// </summary>
    public class SlotReelController : MonoBehaviour
    {
        [Header("Reel Configuration")]
        [SerializeField] private int _reelIndex;
        [SerializeField] private RectTransform _reelStripContainer;
        [SerializeField] private Image[] _symbolViews;

        [Header("Motion Parameters")]
        [SerializeField] private float _symbolHeight = 130f;
        [SerializeField] private float _spinSpeed = 1600f;
        [SerializeField] private float _accelerationTime = 0.35f;
        [SerializeField] private float _decelerationTime = 0.5f;
        [SerializeField] private float _landingOvershoot = 22f;

        private SlotSymbolData[] _availableSymbols;
        private int _targetSymbolId;
        private bool _isSpinning;
        private Coroutine _spinCoroutine;
        private float _currentSpeed;

        public event Action<int> OnReelStopped;

        public int ReelIndex
        {
            get { return _reelIndex; }
        }

        public bool IsSpinning
        {
            get { return _isSpinning; }
        }

        public int CurrentCenterSymbolId { get; private set; }

        public void Initialize(int reelIndex, SlotSymbolData[] symbols, float symbolHeight, float spinSpeed)
        {
            _reelIndex = reelIndex;
            _availableSymbols = symbols;
            if (symbolHeight > 0) _symbolHeight = symbolHeight;
            if (spinSpeed > 0) _spinSpeed = spinSpeed;

            // Initialize symbol views positioning
            if (_symbolViews != null && _symbolViews.Length > 0 && _availableSymbols != null && _availableSymbols.Length > 0)
            {
                int count = _symbolViews.Length;
                int centerIndex = count / 2;
                for (int i = 0; i < count; i++)
                {
                    int offsetFromCenter = i - centerIndex;
                    RectTransform rt = _symbolViews[i].rectTransform;
                    rt.anchoredPosition = new Vector2(0, offsetFromCenter * _symbolHeight);

                    int symIdx = (i + reelIndex) % _availableSymbols.Length;
                    _symbolViews[i].sprite = _availableSymbols[symIdx].SymbolSprite;
                    if (i == centerIndex)
                    {
                        CurrentCenterSymbolId = _availableSymbols[symIdx].SymbolId;
                    }
                }
            }
        }

        /// <summary>
        /// Starts the continuous spinning motion of the reel.
        /// </summary>
        public void StartSpin()
        {
            if (_spinCoroutine != null)
            {
                StopCoroutine(_spinCoroutine);
            }
            _isSpinning = true;
            _spinCoroutine = StartCoroutine(SpinRoutine());
        }

        /// <summary>
        /// Signals the reel to decelerate and land precisely on the specified target symbol.
        /// </summary>
        public void StopAtSymbol(int targetSymbolId)
        {
            _targetSymbolId = targetSymbolId;
        }

        private IEnumerator SpinRoutine()
        {
            float elapsed = 0f;
            _currentSpeed = 0f;

            // Phase 1: Acceleration
            while (elapsed < _accelerationTime)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _accelerationTime);
                _currentSpeed = Mathf.Lerp(0f, _spinSpeed, t * t);
                ScrollSymbols(_currentSpeed * Time.deltaTime);
                yield return null;
            }

            _currentSpeed = _spinSpeed;

            // Phase 2: Steady state spin until target is assigned
            _targetSymbolId = -1;
            while (_targetSymbolId < 0)
            {
                ScrollSymbols(_currentSpeed * Time.deltaTime);
                yield return null;
            }

            // Phase 3: Deceleration and target symbol alignment
            yield return StartCoroutine(DecelerateAndAlignRoutine(_targetSymbolId));

            _isSpinning = false;
            _spinCoroutine = null;
            if (OnReelStopped != null)
            {
                OnReelStopped(_reelIndex);
            }
        }

        private void ScrollSymbols(float deltaY)
        {
            if (_symbolViews == null || _symbolViews.Length == 0) return;

            int count = _symbolViews.Length;
            float totalSpan = count * _symbolHeight;
            float halfSpan = totalSpan * 0.5f;

            for (int i = 0; i < count; i++)
            {
                RectTransform rt = _symbolViews[i].rectTransform;
                Vector2 pos = rt.anchoredPosition;
                pos.y -= deltaY;

                if (pos.y < -halfSpan)
                {
                    pos.y += totalSpan;
                    if (_availableSymbols != null && _availableSymbols.Length > 0)
                    {
                        int randomSym = UnityEngine.Random.Range(0, _availableSymbols.Length);
                        _symbolViews[i].sprite = _availableSymbols[randomSym].SymbolSprite;
                    }
                }

                rt.anchoredPosition = pos;
            }
        }

        private IEnumerator DecelerateAndAlignRoutine(int targetSymbolId)
        {
            int count = _symbolViews.Length;
            int centerViewIndex = count / 2;

            Sprite targetSprite = null;
            if (_availableSymbols != null)
            {
                for (int s = 0; s < _availableSymbols.Length; s++)
                {
                    if (_availableSymbols[s].SymbolId == targetSymbolId)
                    {
                        targetSprite = _availableSymbols[s].SymbolSprite;
                        break;
                    }
                }
            }

            float decelElapsed = 0f;
            float initialDecelSpeed = _currentSpeed;

            while (decelElapsed < _decelerationTime)
            {
                decelElapsed += Time.deltaTime;
                float t = Mathf.Clamp01(decelElapsed / _decelerationTime);
                float speedFactor = (1f - t) * (1f - t);
                _currentSpeed = initialDecelSpeed * speedFactor;
                ScrollSymbols(_currentSpeed * Time.deltaTime);
                yield return null;
            }

            if (centerViewIndex >= 0 && centerViewIndex < count)
            {
                if (targetSprite != null)
                {
                    _symbolViews[centerViewIndex].sprite = targetSprite;
                }
                CurrentCenterSymbolId = targetSymbolId;

                if (_availableSymbols != null && _availableSymbols.Length > 0)
                {
                    for (int i = 0; i < count; i++)
                    {
                        if (i != centerViewIndex)
                        {
                            int offset = i - centerViewIndex;
                            int adjIdx = (targetSymbolId + offset + _availableSymbols.Length * 10) % _availableSymbols.Length;
                            _symbolViews[i].sprite = _availableSymbols[adjIdx].SymbolSprite;
                        }
                    }
                }
            }

            float bounceElapsed = 0f;
            float bounceDownDuration = 0.12f;
            while (bounceElapsed < bounceDownDuration)
            {
                bounceElapsed += Time.deltaTime;
                float t = Mathf.Clamp01(bounceElapsed / bounceDownDuration);
                float yOffset = Mathf.Sin(t * Mathf.PI * 0.5f) * (-_landingOvershoot);
                SnapReelViewsWithOffset(yOffset);
                yield return null;
            }

            float reboundElapsed = 0f;
            float reboundDuration = 0.18f;
            while (reboundElapsed < reboundDuration)
            {
                reboundElapsed += Time.deltaTime;
                float t = Mathf.Clamp01(reboundElapsed / reboundDuration);
                float ease = Mathf.Sin(t * Mathf.PI * 0.5f);
                float yOffset = Mathf.Lerp(-_landingOvershoot, 0f, ease);
                SnapReelViewsWithOffset(yOffset);
                yield return null;
            }

            SnapReelViewsWithOffset(0f);
        }

        private void SnapReelViewsWithOffset(float globalYOffset)
        {
            if (_symbolViews == null) return;
            int count = _symbolViews.Length;
            int centerIndex = count / 2;

            for (int i = 0; i < count; i++)
            {
                int offset = i - centerIndex;
                RectTransform rt = _symbolViews[i].rectTransform;
                rt.anchoredPosition = new Vector2(0f, (offset * _symbolHeight) + globalYOffset);
            }
        }

        public void ForceSetSymbol(int symbolId)
        {
            if (_spinCoroutine != null)
            {
                StopCoroutine(_spinCoroutine);
                _spinCoroutine = null;
            }
            _isSpinning = false;
            CurrentCenterSymbolId = symbolId;

            int count = _symbolViews != null ? _symbolViews.Length : 0;
            int centerIndex = count / 2;

            Sprite centerSprite = null;
            if (_availableSymbols != null)
            {
                for (int s = 0; s < _availableSymbols.Length; s++)
                {
                    if (_availableSymbols[s].SymbolId == symbolId)
                    {
                        centerSprite = _availableSymbols[s].SymbolSprite;
                        break;
                    }
                }
            }

            if (_symbolViews != null)
            {
                for (int i = 0; i < count; i++)
                {
                    int offset = i - centerIndex;
                    _symbolViews[i].rectTransform.anchoredPosition = new Vector2(0f, offset * _symbolHeight);
                    if (i == centerIndex && centerSprite != null)
                    {
                        _symbolViews[i].sprite = centerSprite;
                    }
                    else if (_availableSymbols != null && _availableSymbols.Length > 0)
                    {
                        int adjIdx = (symbolId + offset + _availableSymbols.Length * 10) % _availableSymbols.Length;
                        _symbolViews[i].sprite = _availableSymbols[adjIdx].SymbolSprite;
                    }
                }
            }
        }
    }
}
