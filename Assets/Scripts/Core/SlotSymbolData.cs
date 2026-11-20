using System;
using UnityEngine;

namespace SlotGame.Core
{
    /// <summary>
    /// ScriptableObject defining metadata, visual icon, and payout multiplier for a slot symbol.
    /// </summary>
    [CreateAssetMenu(fileName = "NewSymbolData", menuName = "SlotGame/Symbol Data")]
    public class SlotSymbolData : ScriptableObject
    {
        [Tooltip("Unique integer identifier for the symbol (0-indexed).")]
        [SerializeField] private int _symbolId;

        [Tooltip("Human-readable name of the symbol.")]
        [SerializeField] private string _symbolName = "Symbol";

        [Tooltip("Winning payout multiplier when 3 identical symbols land.")]
        [SerializeField] private int _payoutMultiplier = 2;

        [Tooltip("Visual 2D sprite representing the symbol on the reel.")]
        [SerializeField] private Sprite _symbolSprite;

        [Tooltip("Probability weight for RNG selection. Equal weights give uniform distribution.")]
        [SerializeField] private int _weight = 1;

        public int SymbolId
        {
            get { return _symbolId; }
        }

        public string SymbolName
        {
            get { return _symbolName; }
        }

        public int PayoutMultiplier
        {
            get { return _payoutMultiplier; }
        }

        public Sprite SymbolSprite
        {
            get { return _symbolSprite; }
        }

        public int Weight
        {
            get { return Mathf.Max(1, _weight); }
        }

        public void Initialize(int id, string name, int multiplier, Sprite sprite, int weight = 1)
        {
            _symbolId = id;
            _symbolName = name;
            _payoutMultiplier = multiplier;
            _symbolSprite = sprite;
            _weight = weight;
        }
    }
}
