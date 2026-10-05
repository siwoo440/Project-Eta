using System; // Serializable 사용
using UnityEngine; // SerializeField·Vector2Int 사용
using ProjectEta.Pieces; // 상태 이상·소환 기물 참조 사용

namespace ProjectEta.Abilities
{
    [Serializable]
    public sealed class AbilityEffectData
    {
        [SerializeField] private AbilityEffectType _effectType; // 실행할 공통 효과 종류
        [SerializeField] private int _amount; // 회복량·피해 증감량 등 공통 정수 값
        [SerializeField] private int _durationTurns = 1; // 상태·봉쇄 등 지속 턴
        [SerializeField] private int _radius = 1; // 오라·Redirect 등 탐색 반경
        [SerializeField] private StatusEffectDefinition _statusEffect; // ApplyStatus가 사용할 상태 정의
        [SerializeField] private PieceDefinition _summonPiece; // Summon이 생성할 기물 정의
        [SerializeField] private Vector2Int _vector; // 이동 범위 보정·위치 효과용 보조 벡터

        public AbilityEffectType EffectType => _effectType;
        public int Amount => _amount;
        public int DurationTurns => _durationTurns < 0 ? 0 : _durationTurns;
        public int Radius => _radius < 0 ? 0 : _radius;
        public StatusEffectDefinition StatusEffect => _statusEffect;
        public PieceDefinition SummonPiece => _summonPiece;
        public Vector2Int Vector => _vector;

        public AbilityEffectData() { }

        public AbilityEffectData(
            AbilityEffectType effectType,
            int amount = 0,
            int durationTurns = 1,
            int radius = 1)
        {
            _effectType = effectType;
            _amount = amount;
            _durationTurns = durationTurns;
            _radius = radius;
        }
    }
}
