namespace ProjectEta.Pieces // 기물 관련 타입을 모아두는 네임스페이스
{
    public class AttackAnimationTimings // 30일차: 공격 연출 단계별 지속 시간(모두 테스트용 임시값, 추후 밸런스 시트에서 조정)
    {
        public float RisingSeconds { get; set; } = 0.12f; // 상승 단계 지속 시간
        public float ApproachingSeconds { get; set; } = 0.13f; // 접근 단계 지속 시간
        public float StrikingSeconds { get; set; } = 0.08f; // 타격 정지 단계 지속 시간
        public float RecoveringSeconds { get; set; } = 0.15f; // 복귀 단계 지속 시간
    }
}
