namespace ProjectEta.Pieces // 기물 관련 타입을 모아두는 네임스페이스
{
    public enum StatusStackMode // 27일차: 동일 상태를 다시 걸었을 때의 처리 방식
    {
        StacksAdd, // 지속 턴을 갱신하며 중첩 수를 최대치까지 누적 (예: 독)
        RefreshDuration // 중첩 없이 지속 턴만 갱신 (예: 화상)
    }
}
