#if UNITY_EDITOR
using System.Text; // 로그 문자열 생성
using UnityEditor; // 메뉴와 에셋 로드
using UnityEngine; // Debug 사용
using ProjectEta.Fusion; // 합성 분석 사용
using ProjectEta.Pieces; // 기물 로스터 사용

namespace ProjectEta.EditorTools
{
    public static class Day88UpperFusionValidationMenu
    {
        [MenuItem("Project Eta/Day 88/Validate Full Piece And Fusion Data")]
        public static void Validate()
        {
            PieceDatabase pieces = AssetDatabase.LoadAssetAtPath<PieceDatabase>("Assets/ProjectEta/Data/PieceDatabase.asset");
            FusionRecipeDatabase recipes = AssetDatabase.LoadAssetAtPath<FusionRecipeDatabase>("Assets/ProjectEta/Data/FusionRecipeDatabase.asset");

            PieceRosterValidationReport roster = PieceRosterValidator.Validate(pieces);
            FusionProgressionReport progression = FusionProgressionAnalyzer.Analyze(pieces, recipes);

            int hidden = 0;
            if (recipes != null)
            {
                foreach (FusionRecipe recipe in recipes.Recipes)
                {
                    if (recipe != null && recipe.IsHiddenRecipe) hidden++;
                }
            }

            var builder = new StringBuilder();
            builder.AppendLine("[Day88] 81종·70레시피 완성 검증");
            builder.AppendLine($"PieceDatabase: {(pieces != null ? pieces.Definitions.Count : 0)} / 81");
            builder.AppendLine($"FusionRecipeDatabase: {(recipes != null ? recipes.Recipes.Count : 0)} / 70");
            builder.AppendLine(progression.BuildGradeSummary());
            builder.AppendLine($"공개: {(recipes != null ? recipes.Recipes.Count - hidden : 0)} / 63");
            builder.AppendLine($"숨김: {hidden} / 7");
            builder.AppendLine($"로스터 미등록: {roster.MissingCount}");
            builder.AppendLine($"로스터 구조 문제: {roster.SchemaIssueCount}");
            builder.AppendLine($"합성 데이터 문제: {progression.ContentIssueCount}");
            builder.AppendLine($"도달 불가 등급: {(progression.FirstUnreachableGrade.HasValue ? progression.FirstUnreachableGrade.Value.ToString() : "없음")}");

            bool valid =
                pieces != null &&
                recipes != null &&
                pieces.Definitions.Count == 81 &&
                recipes.Recipes.Count == 70 &&
                hidden == 7 &&
                roster.HasCompleteRoster &&
                roster.SchemaIssueCount == 0 &&
                progression.ContentIssueCount == 0 &&
                progression.HasCompleteGradeCoverage;

            if (valid) Debug.Log(builder.ToString());
            else Debug.LogError(builder.ToString());
        }
    }
}
#endif
