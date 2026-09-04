namespace ProjectEta.AI // 적 AI 관련 타입을 모아두는 네임스페이스
{
    public enum AIActionType // AI가 한 턴에 선택할 수 있는 기본 행동 종류
    {
        Move = 0, // 빈 칸으로 이동하는 행동
        Attack = 1 // 플레이어 기물을 공격하는 행동
    }
}
