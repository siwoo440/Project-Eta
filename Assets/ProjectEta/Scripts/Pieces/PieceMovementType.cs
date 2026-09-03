namespace ProjectEta.Pieces // 기물 관련 타입을 모아두는 네임스페이스
{
    public enum PieceMovementType // 기물의 이동 규칙 종류를 나타내는 열거형
    {
        King, // 킹형 이동(8방향 1칸)
        Pawn, // 폰형 이동(전진)
        Knight, // 나이트형 이동(도약)
        Bishop, // 비숍형 이동(대각선 슬라이드)
        Rook, // 룩형 이동(직선 슬라이드)
        Queen, // 퀸형 이동(직선+대각선 슬라이드)
        Archbishop, // 비숍+나이트 합성 이동
        Chancellor, // 룩+나이트 합성 이동
        Amazon, // 퀸+나이트 합성 이동
        Custom // 그 외 커스텀 이동 규칙
    }
}
