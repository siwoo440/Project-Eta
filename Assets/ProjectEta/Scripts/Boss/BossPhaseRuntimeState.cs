using UnityEngine; // Mathf를 사용하기 위한 네임스페이스

namespace ProjectEta.Boss // 2x2 이상 보스 전투 관련 타입을 모아두는 네임스페이스
{
    public enum BossPhase // 최종 보스의 현재 전투 페이즈
    {
        Phase1 = 1, // 38일차의 기본 이동·직접 공격을 사용하는 첫 페이즈
        Phase2 = 2 // 텔레그래프 범위 공격과 1회 증원을 사용하는 두 번째 페이즈
    }

    public enum BossPatternType // 39일차 Phase 2에서 번갈아 사용하는 텔레그래프 공격 종류
    {
        SlamRing = 0, // 2x2 보스 몸체 주변 한 칸 전체를 공격하는 주변 강타
        KingLane = 1 // 플레이어 King 방향으로 2칸 폭의 직선 범위를 공격하는 압박 패턴
    }

    public static class BossPhaseRules // 페이즈 전환 조건처럼 데이터와 무관하게 검증할 수 있는 순수 규칙 모음
    {
        public static bool ShouldEnterPhase2(int currentHp, int maxHp) // 현재 HP가 최대 HP의 절반 이하인지 확인하는 메서드
        {
            if (maxHp <= 0) return false; // 최대 HP 데이터가 없으면 페이즈 판정을 하지 않음
            if (currentHp <= 0) return false; // 이미 사망한 보스는 새 페이즈로 전환하지 않음
            return currentHp * 2 <= maxHp; // 부동소수점 오차 없이 정확히 50% 이하에서 Phase 2 진입
        }
    }

    public sealed class BossPhaseRuntimeState // 보스 한 기의 페이즈·다음 패턴·대기 중 텔레그래프를 런타임에서 보관하는 상태
    {
        private BossPhase _phase = BossPhase.Phase1; // 새 보스는 항상 Phase 1에서 시작
        private int _nextPatternIndex; // SlamRing과 KingLane을 번갈아 선택하기 위한 인덱스
        private bool _reinforcementCalled; // Phase 2 진입 증원이 이미 처리됐는지 여부
        private BossTelegraphState _pendingTelegraph; // 다음 EnemyTurn에 실행할 현재 예고 공격

        public BossPhase Phase => _phase; // 외부에서 현재 페이즈를 읽기 위한 프로퍼티
        public bool ReinforcementCalled => _reinforcementCalled; // 증원 1회 처리 여부
        public BossTelegraphState PendingTelegraph => _pendingTelegraph; // 현재 예고 중인 공격 상태

        public bool TryEnterPhase2(int currentHp, int maxHp) // HP 조건을 검사해 Phase 1에서 Phase 2로 한 번만 전환하는 메서드
        {
            if (_phase == BossPhase.Phase2) return false; // 이미 Phase 2면 중복 전환 금지
            if (!BossPhaseRules.ShouldEnterPhase2(currentHp, maxHp)) return false; // 아직 절반 HP 조건을 만족하지 않으면 유지
            _phase = BossPhase.Phase2; // 실제 페이즈 상태 변경
            return true; // 이번 호출에서 새로 전환됐음을 반환
        }

        public bool TryMarkReinforcementCalled() // Phase 2 진입 증원을 한 번만 실행하기 위한 원샷 플래그 소비 메서드
        {
            if (_reinforcementCalled) return false; // 이미 호출했으면 다시 실행하지 않음
            _reinforcementCalled = true; // 첫 호출을 처리 완료로 기록
            return true; // 이번 호출이 최초였음을 반환
        }

        public BossPatternType ConsumeNextPatternType() // Phase 2 패턴을 주변 강타→King 직선→주변 강타 순서로 번갈아 반환하는 메서드
        {
            BossPatternType type = _nextPatternIndex % 2 == 0 ? BossPatternType.SlamRing : BossPatternType.KingLane; // 짝수는 주변 강타, 홀수는 King 직선
            _nextPatternIndex++; // 다음 EnemyTurn 계획을 위해 인덱스 증가
            return type; // 이번에 사용할 패턴 반환
        }

        public void SetPendingTelegraph(BossTelegraphState telegraph) // 다음 EnemyTurn 실행 예정 텔레그래프를 저장하는 메서드
        {
            _pendingTelegraph = telegraph; // 예고 자체로 피해를 주지 않고 상태만 보관
        }

        public void ClearPendingTelegraph() // 공격 실행 또는 보스 사망 후 남은 예고 상태를 제거하는 메서드
        {
            _pendingTelegraph = null; // 다음 계획을 받을 수 있도록 비움
        }
    }
}
