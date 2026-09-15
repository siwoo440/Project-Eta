using ProjectEta.Pieces; // 기물 등급 사용

namespace ProjectEta.Cards // 카드 상태 네임스페이스
{ // 네임스페이스 범위 시작
    public static class CardOwnershipRules // 등급별 동일 카드 보유 상한 규칙
    { // 규칙 클래스 범위 시작
        public const int DefaultOwnedLimit = 3; // 1~3성 동일 카드 보유 상한
        public const int FourStarOwnedLimit = 2; // 4성 동일 카드 보유 상한
        public const int FiveStarOwnedLimit = 1; // 5성 동일 카드 보유 상한

        public static int GetOwnedLimit(PieceGrade grade) // 등급별 동일 카드 보유 상한 조회
        { // 메서드 범위 시작
            if (grade == PieceGrade.FiveStar) // 5성 등급 확인
            { // 5성 분기 시작
                return FiveStarOwnedLimit; // 5성 보유 상한 반환
            } // 5성 분기 종료

            if (grade == PieceGrade.FourStar) // 4성 등급 확인
            { // 4성 분기 시작
                return FourStarOwnedLimit; // 4성 보유 상한 반환
            } // 4성 분기 종료

            return DefaultOwnedLimit; // 1~3성 보유 상한 반환
        } // 메서드 범위 종료
    } // 규칙 클래스 범위 종료
} // 네임스페이스 범위 종료
