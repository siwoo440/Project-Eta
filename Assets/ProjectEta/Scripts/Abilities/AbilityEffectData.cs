using System;
using UnityEngine;
using ProjectEta.Pieces;

namespace ProjectEta.Abilities
{
    [Serializable]
    public sealed class AbilityEffectData
    {
        [SerializeField] private AbilityEffectType _effectType;
        [SerializeField] private int _amount;
        [SerializeField] private int _durationTurns = 1;
        [SerializeField] private int _radius = 1;
        [SerializeField] private StatusEffectDefinition _statusEffect;
        [SerializeField] private PieceDefinition _summonPiece;
        [SerializeField] private Vector2Int _vector;
        [SerializeField] private string _auraGroupId;
        [SerializeField] private bool _includeSelf;
        [SerializeField] private int _maxActiveSummons; // 93일차: 소환자별 동시 유지 가능한 임시 소환물 수(0이면 제한 없음)

        public AbilityEffectType EffectType => _effectType;
        public int Amount => _amount;
        public int DurationTurns => _durationTurns < 0 ? 0 : _durationTurns;
        public int Radius => _radius < 0 ? 0 : _radius;
        public StatusEffectDefinition StatusEffect => _statusEffect;
        public PieceDefinition SummonPiece => _summonPiece;
        public Vector2Int Vector => _vector;
        public string AuraGroupId =>
            string.IsNullOrWhiteSpace(_auraGroupId) ? "default_aura" : _auraGroupId;
        public bool IncludeSelf => _includeSelf;
        public int MaxActiveSummons => _maxActiveSummons < 0 ? 0 : _maxActiveSummons;

        public AbilityEffectData() { }

        public AbilityEffectData(
            AbilityEffectType effectType,
            int amount = 0,
            int durationTurns = 1,
            int radius = 1,
            StatusEffectDefinition statusEffect = null,
            PieceDefinition summonPiece = null,
            string auraGroupId = "",
            bool includeSelf = false,
            Vector2Int? vector = null,
            int maxActiveSummons = 0)
        {
            _effectType = effectType;
            _amount = amount;
            _durationTurns = durationTurns;
            _radius = radius;
            _statusEffect = statusEffect;
            _summonPiece = summonPiece;
            _auraGroupId = auraGroupId;
            _includeSelf = includeSelf;
            _vector = vector ?? Vector2Int.zero;
            _maxActiveSummons = maxActiveSummons;
        }
    }
}
