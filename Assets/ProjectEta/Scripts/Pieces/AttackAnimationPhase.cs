namespace ProjectEta.Pieces // 기물 관련 타입을 모아두는 네임스페이스
{
    public enum AttackAnimationPhase // 30일차: 근접 공격 연출이 거치는 단계
    {
        Idle, // 아직 시작하지 않음
        Rising, // 현재 칸에서 살짝 떠오르는 단계
        Approaching, // 목표 쪽으로 다가가는 단계
        Striking, // 다가간 위치에서 짧게 멈춰 타격하는 단계
        Recovering, // 원래 위치로 복귀하는 단계(비치명 결과 전용)
        Complete // 연출이 모두 끝난 상태
    }
}
