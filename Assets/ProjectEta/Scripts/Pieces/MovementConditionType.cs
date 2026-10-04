namespace ProjectEta.Pieces
{
    public enum MovementConditionType
    {
        None = 0, // 별도 조건 없음
        Pawn = 1, // 폰 전진·공격 분리
        ChameleonCycle = 2, // 카멜레온 순환 이동
        Spearman = 3, // 창병 직교 이동·2칸 공격
        Shooter = 4, // 사수 전방 대각선 슬라이드
        ShieldGuard = 5, // 방패병 좌우·후방 이동
        FlagBearer = 6, // 깃발병 전방·좌우 이동
        Pursuer = 7, // 추격병 전방·후방 대각선 이동
        Scout = 8 // 척후병 전방 대각선·좌우 이동
    }
}
