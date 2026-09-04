namespace ProjectEta.Pieces // 기물 이동 규칙 데이터 타입을 모아두는 네임스페이스
{
    public enum MovementRuleKind // PieceDefinition이 조합할 수 있는 기본 이동 규칙의 종류
    {
        Step, // 지정 방향으로 제한된 칸 수만큼 한 칸씩 진행하는 규칙
        Slide, // 지정 방향으로 장애물이나 보드 끝까지 연속 이동하는 규칙
        Leap, // 중간 칸을 무시하고 지정 상대 좌표로 바로 도약하는 규칙
        Conditional // 진영이나 상황에 따라 별도 계산이 필요한 조건부 규칙
    }
}
