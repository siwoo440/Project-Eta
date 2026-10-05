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
        Scout = 8, // 척후병 전방 대각선·좌우 이동
        Grenadier = 9, // 척탄병 직교 이동·거리 2~3 공격
        Pikeman = 10, // 장창병 직교 이동·거리 2~3 직선 공격
        Crossbowman = 11, // 석궁병 제한 이동·직선 장거리 공격
        Gryphon = 12, // 그리폰 대각 진입 후 바깥 직선 이동
        Artillery = 13, // 포병대 성채 이동·스크린 포격
        Vanguard = 14, // 돌격대장 전방 최대 3칸·좌우 1칸
        Sniper = 15, // 저격수 제한 이동·8방향 장거리 공격
        GrandGryphon = 16, // 그랜드 그리폰 정·역그리폰 복합 이동
        IllusionistCycle = 17, // 환술사 기병→사제→성채 순환
        Deadeye = 18, // 대저격수 직교 이동·8칸 장거리 공격
        PhantomGeneralCycle = 19 // 환영장군 5단계 순환 이동
    }
}
