using ProjectEta.Cards; // 카드 보유 상한 사용
using ProjectEta.Pieces; // 기물 데이터 사용

namespace ProjectEta.Fusion
{
    public static class FusionRuleValidator
    {
        public const int FourStarOwnedLimit = CardOwnershipRules.FourStarOwnedLimit; // 4성 보유 상한
        public const int FiveStarOwnedLimit = CardOwnershipRules.FiveStarOwnedLimit; // 5성 보유 상한

        public static bool IsFusableMaterial(PieceDefinition material)
        {
            if (material == null) return false; // 빈 재료 차단

            // PieceMovementType의 기본값이 King이므로 MovementType만 보고 King을 판정하면
            // 테스트·런타임에서 생성한 Basic/Fusion 임시 기물까지 King으로 오인한다.
            if (material.Category == PieceCategory.Special &&
                material.MovementType == PieceMovementType.King)
            {
                return false; // 실제 Special King 계열만 합성 재료에서 제외
            }

            return material.Category == PieceCategory.Basic ||
                   material.Category == PieceCategory.Fusion ||
                   material.Category == PieceCategory.Special; // 일반·합성·특수형 플레이어 기물 허용
        }

        public static int GetOwnedLimit(PieceGrade grade)
        {
            return CardOwnershipRules.GetOwnedLimit(grade); // 등급별 보유 상한 반환
        }

        public static bool HasOwnedLimit(PieceGrade grade)
        {
            return grade == PieceGrade.FourStar || grade == PieceGrade.FiveStar; // 배치 제한 등급 확인
        }

        public static bool IsGradeStepValid(FusionRecipe recipe)
        {
            if (recipe == null || recipe.Result == null || recipe.MaterialA == null || recipe.MaterialB == null) return false; // 불완전 데이터 차단
            if (recipe.IgnoresGradeStepRule) return true; // 명시적 예외 허용

            int materialGrade = (int)recipe.MaterialA.Grade > (int)recipe.MaterialB.Grade
                ? (int)recipe.MaterialA.Grade
                : (int)recipe.MaterialB.Grade; // 두 재료 중 높은 등급 사용

            return (int)recipe.Result.Grade == materialGrade + 1; // 결과는 정확히 한 단계 상승
        }

        public static FusionBlockReason ValidateRecipe(FusionRecipe recipe)
        {
            if (recipe == null || recipe.Result == null) return FusionBlockReason.NoRecipe; // 레시피·결과 누락
            if (!IsFusableMaterial(recipe.MaterialA) || !IsFusableMaterial(recipe.MaterialB)) return FusionBlockReason.MaterialNotFusable; // 재료 분류 위반
            if (!IsGradeStepValid(recipe)) return FusionBlockReason.GradeStepViolation; // 등급 상승 위반
            return FusionBlockReason.None; // 정상 레시피
        }

        public static FusionBlockReason ValidateOwnedLimit(PieceDefinition result, int currentOwnedCount)
        {
            if (result == null) return FusionBlockReason.NoRecipe; // 결과 누락
            int limit = GetOwnedLimit(result.Grade); // 결과 등급 상한 조회
            if (limit == int.MaxValue) return FusionBlockReason.None; // 상한 없음
            return currentOwnedCount >= limit ? FusionBlockReason.OwnedLimitReached : FusionBlockReason.None; // 상한 판정
        }

        public static string DescribeBlockReason(FusionBlockReason reason)
        {
            switch (reason)
            {
                case FusionBlockReason.NotEnoughMaterials: return "재료를 선택하세요";
                case FusionBlockReason.NoRecipe: return "합성 가능한 조합이 아닙니다";
                case FusionBlockReason.MaterialNotFusable: return "합성 재료로 쓸 수 없는 기물입니다";
                case FusionBlockReason.GradeStepViolation: return "등급은 한 번에 한 단계만 올릴 수 있습니다";
                case FusionBlockReason.MaterialsMissingInHand: return "손패에 재료가 부족합니다";
                case FusionBlockReason.OwnedLimitReached: return "해당 등급의 보유 수량 제한에 도달했습니다";
                case FusionBlockReason.NotDeploymentTurn: return "배치 턴에만 합성할 수 있습니다";
                default: return "";
            }
        }
    }
}
