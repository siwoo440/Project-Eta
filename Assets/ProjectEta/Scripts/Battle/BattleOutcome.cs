namespace ProjectEta.Battle // 전투 관련 타입을 모아두는 네임스페이스
{
    public enum BattleOutcome // 13일차: 전투가 어떻게 끝났는지 구분하는 열거형
    {
        None, // 아직 전투가 끝나지 않음
        Victory, // 적 전멸로 승리
        Defeat // 킹 HP 0 또는 턴 제한 초과로 패배
    }
}
