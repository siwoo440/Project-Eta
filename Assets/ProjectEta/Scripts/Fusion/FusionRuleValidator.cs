using ProjectEta.Pieces; // PieceDefinition, PieceGrade, PieceCategory를 사용하기 위한 네임스페이스

namespace ProjectEta.Fusion // 합성 관련 타입을 모아두는 네임스페이스
{
    public static class FusionRuleValidator // 22일차: 기획서 5.7 합성 등급·수량 규칙을 한 곳에서 판정하는 정적 검증기
    {
        public const int FourStarOwnedLimit = 2; // 4성 기물은 동일 기물 기준 최대 2개까지 보유 가능(기획서 "제한을 둘 수 있다"의 기본안)
        public const int FiveStarOwnedLimit = 1; // 5성 기물은 동일 최상위 기물 1개로 제한(기획서 기본안)

        public static bool IsFusableMaterial(PieceDefinition material) // 해당 기물이 합성 재료로 사용 가능한 분류인지 판정하는 메서드
        {
            if (material == null) return false; // 재료가 없으면 사용 불가
            return material.Category == PieceCategory.Basic || material.Category == PieceCategory.Fusion; // 기본·합성 기물만 재료로 허용(King은 Special, 적/보스는 Monster·Boss로 제외)
        }

        public static int GetOwnedLimit(PieceGrade grade) // 등급별 동일 기물 보유 상한을 반환하는 메서드
        {
            if (grade == PieceGrade.FiveStar) return FiveStarOwnedLimit; // 5성은 1개 제한
            if (grade == PieceGrade.FourStar) return FourStarOwnedLimit; // 4성은 2개 제한
            return int.MaxValue; // 1~3성은 별도 상한 없음
        }

        public static bool HasOwnedLimit(PieceGrade grade) // 해당 등급에 수량 제한이 걸려 있는지 확인하는 메서드
        {
            return GetOwnedLimit(grade) != int.MaxValue; // 상한이 무제한이 아니면 제한이 있는 등급
        }

        public static bool IsGradeStepValid(FusionRecipe recipe) // 합성 결과가 "한 번에 한 등급" 상승 규칙을 지키는지 판정하는 메서드
        {
            if (recipe == null || recipe.Result == null || recipe.MaterialA == null || recipe.MaterialB == null) return false; // 데이터가 불완전하면 규칙 위반으로 처리
            if (recipe.IgnoresGradeStepRule) return true; // 동일 카드 특수 레시피 등 데이터에서 명시적으로 예외 처리한 레시피는 통과

            int materialGrade = (int)recipe.MaterialA.Grade > (int)recipe.MaterialB.Grade ? (int)recipe.MaterialA.Grade : (int)recipe.MaterialB.Grade; // 재료 두 장 중 더 높은 등급을 기준으로 사용
            return (int)recipe.Result.Grade == materialGrade + 1; // 결과는 기준 등급보다 정확히 한 단계만 높아야 함
        }

        public static FusionBlockReason ValidateRecipe(FusionRecipe recipe) // 레시피 자체가 등급·재료 규칙을 만족하는지 판정하는 메서드
        {
            if (recipe == null || recipe.Result == null) return FusionBlockReason.NoRecipe; // 레시피나 결과가 없으면 조합 없음으로 처리
            if (!IsFusableMaterial(recipe.MaterialA) || !IsFusableMaterial(recipe.MaterialB)) return FusionBlockReason.MaterialNotFusable; // 재료 분류가 합성 대상이 아니면 차단
            if (!IsGradeStepValid(recipe)) return FusionBlockReason.GradeStepViolation; // 등급이 2단계 이상 뛰면 차단
            return FusionBlockReason.None; // 모든 레시피 규칙을 통과
        }

        public static FusionBlockReason ValidateOwnedLimit(PieceDefinition result, int currentOwnedCount) // 결과 기물의 보유 수량 제한을 판정하는 메서드
        {
            if (result == null) return FusionBlockReason.NoRecipe; // 결과가 없으면 조합 없음으로 처리
            int limit = GetOwnedLimit(result.Grade); // 결과 등급의 보유 상한 조회
            if (limit == int.MaxValue) return FusionBlockReason.None; // 상한이 없는 등급이면 즉시 통과
            return currentOwnedCount >= limit ? FusionBlockReason.OwnedLimitReached : FusionBlockReason.None; // 이미 상한을 채웠으면 차단
        }

        public static string DescribeBlockReason(FusionBlockReason reason) // 차단 사유를 합성 패널에 그대로 띄울 한글 문구로 변환하는 메서드
        {
            switch (reason) // 사유별 안내 문구 분기
            {
                case FusionBlockReason.NotEnoughMaterials: return "재료를 선택하세요"; // 재료가 덜 모인 경우
                case FusionBlockReason.NoRecipe: return "합성 가능한 조합이 아닙니다"; // 매칭 레시피가 없는 경우
                case FusionBlockReason.MaterialNotFusable: return "합성 재료로 쓸 수 없는 기물입니다"; // King 등 합성 제외 기물
                case FusionBlockReason.GradeStepViolation: return "등급은 한 번에 한 단계만 올릴 수 있습니다"; // 등급 점프 위반
                case FusionBlockReason.MaterialsMissingInHand: return "손패에 재료가 부족합니다"; // 동일 카드 2장 미보유 포함
                case FusionBlockReason.OwnedLimitReached: return "해당 등급의 보유 수량 제한에 도달했습니다"; // 4·5성 상한 도달
                case FusionBlockReason.NotDeploymentTurn: return "배치 턴에만 합성할 수 있습니다"; // 턴 조건 위반
                default: return ""; // 차단 사유가 없으면 빈 문구
            }
        }
    }
}
