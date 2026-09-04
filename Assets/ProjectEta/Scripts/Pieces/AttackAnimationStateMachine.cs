using UnityEngine; // Mathf를 사용하기 위한 네임스페이스

namespace ProjectEta.Pieces // 기물 관련 타입을 모아두는 네임스페이스
{
    public class AttackAnimationStateMachine // 30일차: 근접 공격 연출의 단계 전이만 담당하는 순수 로직(Unity 좌표·코루틴과 분리해 단위 테스트 가능)
    {
        private readonly AttackAnimationTimings _timings; // 단계별 지속 시간
        private float _elapsedInPhase; // 현재 단계에 머문 시간

        public AttackAnimationPhase CurrentPhase { get; private set; } = AttackAnimationPhase.Idle; // 현재 단계
        public bool IsComplete => CurrentPhase == AttackAnimationPhase.Complete; // 연출이 모두 끝났는지 여부

        public AttackAnimationStateMachine(AttackAnimationTimings timings = null) // 타이밍을 지정하지 않으면 기본 임시값 사용
        {
            _timings = timings ?? new AttackAnimationTimings(); // 기본 타이밍 구성
        }

        public void Start() // 연출을 Idle에서 첫 단계로 진입시키는 메서드
        {
            CurrentPhase = AttackAnimationPhase.Rising; // 상승 단계부터 시작
            _elapsedInPhase = 0f; // 경과 시간 초기화
        }

        public void Advance(float deltaTime) // 매 프레임 호출해 경과 시간을 누적하고 필요하면 다음 단계(들)로 전이하는 메서드
        {
            if (CurrentPhase == AttackAnimationPhase.Idle || CurrentPhase == AttackAnimationPhase.Complete) // 아직 시작 전이거나 이미 끝났으면
            {
                return; // 더 진행할 것이 없음
            }

            _elapsedInPhase += deltaTime; // 현재 단계 경과 시간 누적

            while (CurrentPhase != AttackAnimationPhase.Complete) // 큰 델타타임(프레임 드랍)으로 한 번에 여러 단계를 지나칠 수 있으므로 반복 확인
            {
                float phaseDuration = GetPhaseDuration(CurrentPhase); // 현재 단계의 목표 지속 시간

                if (_elapsedInPhase < phaseDuration) // 아직 단계 지속 시간이 남았으면
                {
                    break; // 더 이상 전이하지 않고 대기
                }

                _elapsedInPhase -= phaseDuration; // 초과분은 다음 단계로 이월
                CurrentPhase = GetNextPhase(CurrentPhase); // 다음 단계로 전이
            }
        }

        public float GetPhaseProgress01() // 현재 단계 안에서의 진행률(0~1)을 반환하는 메서드
        {
            float duration = GetPhaseDuration(CurrentPhase); // 현재 단계의 목표 지속 시간
            return duration <= 0f ? 1f : Mathf.Clamp01(_elapsedInPhase / duration); // 0으로 나누는 상황을 방지하며 0~1로 보정
        }

        private float GetPhaseDuration(AttackAnimationPhase phase) // 단계별 지속 시간을 조회하는 메서드
        {
            switch (phase) // 단계에 따라 분기
            {
                case AttackAnimationPhase.Rising: return _timings.RisingSeconds; // 상승 지속 시간
                case AttackAnimationPhase.Approaching: return _timings.ApproachingSeconds; // 접근 지속 시간
                case AttackAnimationPhase.Striking: return _timings.StrikingSeconds; // 타격 정지 지속 시간
                case AttackAnimationPhase.Recovering: return _timings.RecoveringSeconds; // 복귀 지속 시간
                default: return 0f; // Idle/Complete는 지속 시간 없음
            }
        }

        private static AttackAnimationPhase GetNextPhase(AttackAnimationPhase phase) // 현재 단계 다음에 올 단계를 반환하는 메서드
        {
            switch (phase) // 현재 단계에 따라 분기
            {
                case AttackAnimationPhase.Rising: return AttackAnimationPhase.Approaching; // 상승 다음은 접근
                case AttackAnimationPhase.Approaching: return AttackAnimationPhase.Striking; // 접근 다음은 타격
                case AttackAnimationPhase.Striking: return AttackAnimationPhase.Recovering; // 타격 다음은 복귀
                case AttackAnimationPhase.Recovering: return AttackAnimationPhase.Complete; // 복귀 다음은 완료
                default: return AttackAnimationPhase.Complete; // 그 외는 완료로 처리
            }
        }
    }
}
