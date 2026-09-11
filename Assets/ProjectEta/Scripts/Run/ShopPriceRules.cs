using ProjectEta.Pieces; // PieceDefinition·PieceGrade 사용

namespace ProjectEta.Run
{
    public static class ShopPriceRules
    {
        public const int HealAmount = 1; // 상점 회복량

        public static int GetCardPurchasePrice(PieceDefinition card, int phase, int stage)
        {
            if (card == null) return 0; // 빈 카드 가격 없음
            return GetCardPurchasePrice(card.Grade, phase, stage); // 카드 등급 가격 계산 위임
        }

        public static int GetCardPurchasePrice(PieceGrade grade, int phase, int stage)
        {
            int basePrice = GetGradeBasePrice(grade); // 등급 기본 가격 조회
            int safePhase = NormalizePhase(phase); // 페이즈 범위 보정
            int safeStage = NormalizeStage(stage); // 스테이지 범위 보정
            int phaseSurcharge = (safePhase - 1) * 5; // 후반 페이즈 가격 가산
            int stageSurcharge = ((safeStage - 1) / 3) * 3; // 스테이지 구간 가격 가산
            return basePrice + phaseSurcharge + stageSurcharge; // 최종 구매 가격 반환
        }

        public static int GetCardRemovePrice(int phase)
        {
            int safePhase = NormalizePhase(phase); // 페이즈 범위 보정
            return 35 + (safePhase - 1) * 5; // 후반 제거 비용 증가
        }

        public static int GetHealPrice(int phase)
        {
            int safePhase = NormalizePhase(phase); // 페이즈 범위 보정
            return 20 + (safePhase - 1) * 4; // 후반 회복 비용 증가
        }

        public static int GetUpgradePrice(int phase, int stage)
        {
            int safePhase = NormalizePhase(phase); // 페이즈 범위 보정
            int safeStage = NormalizeStage(stage); // 스테이지 범위 보정
            int phaseSurcharge = (safePhase - 1) * 8; // 후반 페이즈 강화 가산
            int stageSurcharge = ((safeStage - 1) / 5) * 5; // 후반 스테이지 강화 가산
            return 45 + phaseSurcharge + stageSurcharge; // 최종 강화 가격 반환
        }

        private static int GetGradeBasePrice(PieceGrade grade)
        {
            switch (grade)
            {
                case PieceGrade.TwoStar:
                    return 40; // 2성 기본 가격
                case PieceGrade.ThreeStar:
                    return 60; // 3성 기본 가격
                case PieceGrade.FourStar:
                    return 90; // 4성 예비 가격
                case PieceGrade.FiveStar:
                    return 120; // 5성 예비 가격
                default:
                    return 25; // 1성·잘못된 등급 기본 가격
            }
        }

        private static int NormalizePhase(int phase)
        {
            if (phase < RunPhaseProgressService.FirstPhase) return RunPhaseProgressService.FirstPhase; // 최소 페이즈 보정
            if (phase > RunPhaseProgressService.TotalPhases) return RunPhaseProgressService.TotalPhases; // 최대 페이즈 보정
            return phase; // 정상 페이즈 반환
        }

        private static int NormalizeStage(int stage)
        {
            if (stage < RoundState.FirstRound) return RoundState.FirstRound; // 최소 스테이지 보정
            if (stage > RoundState.FinalRound) return RoundState.FinalRound; // 최대 스테이지 보정
            return stage; // 정상 스테이지 반환
        }
    }
}
