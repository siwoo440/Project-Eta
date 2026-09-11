using System.Collections.Generic; // IReadOnlyList<T>·List<T>·HashSet<T> 사용

namespace ProjectEta.Fusion
{
    public enum FusionRecipeContentIssueType
    {
        NullRecipe = 0, // 비어 있는 Recipe 항목
        DuplicateRecipeId = 1, // 중복 RecipeId
        MissingMaterial = 2, // 재료 누락
        MissingResult = 3, // 결과 누락
        RuleViolation = 4, // 합성 규칙 위반
        DuplicateMaterialPair = 5 // 같은 재료 조합 중복 등록
    }

    public sealed class FusionRecipeContentIssue
    {
        public FusionRecipeContentIssueType IssueType { get; } // 문제 종류
        public string RecipeId { get; } // 관련 Recipe ID
        public FusionBlockReason BlockReason { get; } // 합성 규칙 차단 사유

        public FusionRecipeContentIssue(FusionRecipeContentIssueType issueType, string recipeId, FusionBlockReason blockReason)
        {
            IssueType = issueType; // 문제 종류 저장
            RecipeId = recipeId ?? string.Empty; // Recipe ID 저장
            BlockReason = blockReason; // 규칙 차단 사유 저장
        }

        public override string ToString()
        {
            return $"{IssueType} / {RecipeId} / {BlockReason}"; // 테스트 로그용 문제 설명 반환
        }
    }

    public static class FusionRecipeContentValidator
    {
        public static IReadOnlyList<FusionRecipeContentIssue> Validate(FusionRecipeDatabase database)
        {
            return Validate(database != null ? database.Recipes : null); // Database Recipe 목록 검증 위임
        }

        public static IReadOnlyList<FusionRecipeContentIssue> Validate(IReadOnlyList<FusionRecipe> recipes)
        {
            var issues = new List<FusionRecipeContentIssue>(); // 검증 문제 목록 생성
            if (recipes == null) return issues; // Recipe 목록 누락 시 빈 결과 반환
            var recipeIds = new HashSet<string>(); // RecipeId 중복 검사 집합
            var materialPairs = new HashSet<string>(); // 재료 조합 중복 검사 집합

            for (int i = 0; i < recipes.Count; i++)
            {
                FusionRecipe recipe = recipes[i]; // 현재 Recipe 조회

                if (recipe == null)
                {
                    issues.Add(new FusionRecipeContentIssue(FusionRecipeContentIssueType.NullRecipe, $"index_{i}", FusionBlockReason.NoRecipe)); // 빈 Recipe 문제 등록
                    continue; // 다음 Recipe 검사
                }

                string recipeId = recipe.RecipeId; // Recipe 고유 ID 조회
                if (!recipeIds.Add(recipeId)) issues.Add(new FusionRecipeContentIssue(FusionRecipeContentIssueType.DuplicateRecipeId, recipeId, FusionBlockReason.None)); // 중복 ID 문제 등록

                if (recipe.MaterialA == null || recipe.MaterialB == null)
                {
                    issues.Add(new FusionRecipeContentIssue(FusionRecipeContentIssueType.MissingMaterial, recipeId, FusionBlockReason.MaterialNotFusable)); // 재료 누락 문제 등록
                    continue; // 규칙 검증 생략
                }

                if (recipe.Result == null)
                {
                    issues.Add(new FusionRecipeContentIssue(FusionRecipeContentIssueType.MissingResult, recipeId, FusionBlockReason.NoRecipe)); // 결과 누락 문제 등록
                    continue; // 규칙 검증 생략
                }

                string materialPairKey = CreateMaterialPairKey(recipe); // 순서 무관 재료 조합 키 생성
                if (!materialPairs.Add(materialPairKey)) issues.Add(new FusionRecipeContentIssue(FusionRecipeContentIssueType.DuplicateMaterialPair, recipeId, FusionBlockReason.None)); // 결정적 합성 중복 조합 문제 등록

                FusionBlockReason blockReason = FusionRuleValidator.ValidateRecipe(recipe); // 기존 합성 규칙 검증
                if (blockReason != FusionBlockReason.None) issues.Add(new FusionRecipeContentIssue(FusionRecipeContentIssueType.RuleViolation, recipeId, blockReason)); // 규칙 위반 문제 등록
            }

            return issues; // 전체 Fusion Recipe 검증 결과 반환
        }

        private static string CreateMaterialPairKey(FusionRecipe recipe)
        {
            string first = string.IsNullOrEmpty(recipe.MaterialA.PieceId) ? recipe.MaterialA.name : recipe.MaterialA.PieceId; // 재료 A 식별자 조회
            string second = string.IsNullOrEmpty(recipe.MaterialB.PieceId) ? recipe.MaterialB.name : recipe.MaterialB.PieceId; // 재료 B 식별자 조회
            return string.CompareOrdinal(first, second) <= 0 ? $"{first}|{second}" : $"{second}|{first}"; // 재료 순서 무관 조합 키 반환
        }
    }
}
