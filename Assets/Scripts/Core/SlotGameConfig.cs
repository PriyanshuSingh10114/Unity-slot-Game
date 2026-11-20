using System;
using UnityEngine;

namespace SlotGame.Core
{
    /// <summary>
    /// Master ScriptableObject configuration for slot machine gameplay, reels, bets, and timing.
    /// </summary>
    [CreateAssetMenu(fileName = "SlotGameConfig", menuName = "SlotGame/Game Config")]
    public class SlotGameConfig : ScriptableObject
    {
        [Header("Balance & Betting")]
        [SerializeField] private int _startingBalance = 1000;
        [SerializeField] private int[] _betOptions = new int[] { 10, 25, 50, 100 };
        [SerializeField] private int _defaultBetIndex = 0;

        [Header("Symbols")]
        [SerializeField] private SlotSymbolData[] _symbols;

        [Header("Reel Motion & Animation")]
        [Tooltip("Minimum duration in seconds before the first reel begins deceleration.")]
        [SerializeField] private float _minSpinDuration = 1.2f;

        [Tooltip("Staggered delay between consecutive reel stops.")]
        [SerializeField] private float _reelStopStaggerDelay = 0.35f;

        [Tooltip("Spin speed in UI pixels per second during continuous rotation.")]
        [SerializeField] private float _spinSpeed = 1600f;

        [Tooltip("Duration of acceleration phase when spin begins.")]
        [SerializeField] private float _accelerationDuration = 0.3f;

        [Tooltip("Duration of deceleration and bounce easing.")]
        [SerializeField] private float _decelerationDuration = 0.45f;

        [Tooltip("Height in pixels of each symbol slot on the reel strip.")]
        [SerializeField] private float _symbolHeight = 130f;

        [Tooltip("Number of visible symbols per reel window (e.g. 1 center payline or 3 vertical rows).")]
        [SerializeField] private int _visibleSymbolCount = 3;

        [Tooltip("Overshoot bounce distance in pixels during reel landing.")]
        [SerializeField] private float _landingOvershoot = 20f;

        public int StartingBalance
        {
            get { return _startingBalance; }
        }

        public int[] BetOptions
        {
            get { return _betOptions != null ? _betOptions : new int[] { 10, 25, 50, 100 }; }
        }

        public int DefaultBetIndex
        {
            get { return Mathf.Clamp(_defaultBetIndex, 0, BetOptions.Length - 1); }
        }

        public SlotSymbolData[] Symbols
        {
            get { return _symbols; }
        }

        public float MinSpinDuration
        {
            get { return _minSpinDuration; }
        }

        public float ReelStopStaggerDelay
        {
            get { return _reelStopStaggerDelay; }
        }

        public float SpinSpeed
        {
            get { return _spinSpeed; }
        }

        public float AccelerationDuration
        {
            get { return _accelerationDuration; }
        }

        public float DecelerationDuration
        {
            get { return _decelerationDuration; }
        }

        public float SymbolHeight
        {
            get { return _symbolHeight; }
        }

        public int VisibleSymbolCount
        {
            get { return _visibleSymbolCount; }
        }

        public float LandingOvershoot
        {
            get { return _landingOvershoot; }
        }
    }
}
