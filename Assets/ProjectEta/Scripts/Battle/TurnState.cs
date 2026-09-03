namespace ProjectEta.Battle // 전투 턴 관련 타입을 모아두는 네임스페이스
{
    public enum TurnState // 현재 전투가 어느 턴 단계인지 나타내는 열거형
    {
        PlayerTurn, // 플레이어가 기물을 이동·공격할 수 있는 일반 턴
        EnemyTurn, // 적이 행동하며 플레이어 입력이 잠기는 턴
        DeploymentTurn, // 17일차: 5턴마다 손패 카드 1장을 배치할 수 있는 전용 배치 턴
        BattleEnded // 승리·패배 등으로 전투가 종료된 상태
    }
}
