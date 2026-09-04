namespace ProjectEta.Pieces // 기물 이동 규칙 데이터 타입을 모아두는 네임스페이스
{
    public enum MovementConditionType // Conditional 이동 규칙에서 사용할 조건 종류
    {
        None, // 별도 조건이 없는 기본값
        Pawn // 프로젝트 η의 전진·공격 분리 폰 규칙
    }
}
