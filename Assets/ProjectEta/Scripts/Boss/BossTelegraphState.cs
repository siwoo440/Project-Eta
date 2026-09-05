using System.Collections.Generic; // IReadOnlyList<T>와 List<T>를 사용하기 위한 네임스페이스
using UnityEngine; // Vector2Int를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceRuntimeState를 사용하기 위한 네임스페이스

namespace ProjectEta.Boss // 2x2 이상 보스 전투 관련 타입을 모아두는 네임스페이스
{
    public sealed class BossTelegraphState // 다음 EnemyTurn에 실행할 보스 위험 공격의 고정 스냅샷
    {
        private readonly List<Vector2Int> _targetCells; // 예고 순간 계산된 실제 위험 칸 복사본

        public PieceRuntimeState Boss { get; } // 공격을 준비한 보스 런타임 상태
        public BossPatternType PatternType { get; } // 주변 강타 또는 King 직선 종류
        public string DisplayName { get; } // UI와 로그에 표시할 패턴 이름
        public IReadOnlyList<Vector2Int> TargetCells => _targetCells; // UI 표시와 실제 공격이 함께 읽는 동일 위험 칸 목록
        public int PlannedTurn { get; } // 언제 계획했는지 추적하기 위한 턴 번호

        public BossTelegraphState(PieceRuntimeState boss, BossPatternType patternType, string displayName, IReadOnlyList<Vector2Int> targetCells, int plannedTurn) // 예고 상태 생성자
        {
            Boss = boss; // 공격 주체 저장
            PatternType = patternType; // 패턴 종류 저장
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? patternType.ToString() : displayName; // 표시 이름이 비면 타입 이름 사용
            _targetCells = targetCells != null ? new List<Vector2Int>(targetCells) : new List<Vector2Int>(); // 공격 실행 전 보드가 바뀌어도 예고 칸은 고정되도록 복사
            PlannedTurn = plannedTurn; // 계획 턴 저장
        }
    }
}
