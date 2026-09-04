namespace ProjectEta.Pieces // 기물 이동 규칙 데이터 타입을 모아두는 네임스페이스
{
    public enum MovementConditionType // Conditional 이동 규칙에서 사용할 조건 종류
    {
        None = 0, // 별도 조건이 없는 기본값
        Pawn = 1, // 프로젝트 η의 전진·공격 분리 폰 규칙
        ChameleonCycle = 2 // Knight → Bishop → Rook → Queen 순환을 런타임 상태로 선택하는 카멜레온 규칙
    }
}
