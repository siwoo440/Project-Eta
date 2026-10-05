#if UNITY_EDITOR
using System.Text; // 로그 문자열 생성
using UnityEditor; // MenuItem·AssetDatabase 사용
using UnityEngine; // Debug 사용
using ProjectEta.Fusion; // 합성 검증 사용
using ProjectEta.Pieces; // 기물 DB 사용

namespace ProjectEta.EditorTools
{
    public static class Day87FusionValidationMenu
    {
        [MenuItem("Project Eta/Day 87/Validate Fusion Expansion")]
        public static void Validate()
        {
            PieceDatabase pieces = AssetDatabase.LoadAssetAtPath<PieceDatabase>("Assets/ProjectEta/Data/PieceDatabase.asset");
            FusionRecipeDatabase recipes = AssetDatabase.LoadAssetAtPath<FusionRecipeDatabase>("Assets/ProjectEta/Data/FusionRecipeDatabase.asset");

            FusionProgressionReport progression = FusionProgressionAnalyzer.Analyze(pieces, recipes);
            int hidden = 0;
            int twoStar = 0;
            int threeStar = 0;

            if (recipes != null)
            {
                foreach (FusionRecipe recipe in recipes.Recipes)
                {
                    if (recipe == null) continue;
                    if (recipe.IsHiddenRecipe) hidden++;
                    if (recipe.Result != null && recipe.Result.Grade == PieceGrade.TwoStar) twoStar++;
                    if (recipe.Result != null && recipe.Result.Grade == PieceGrade.ThreeStar) threeStar++;
                }
            }

            var builder = new StringBuilder();
            builder.AppendLine("[Day87] 1~3성 합성 확장 검증");
            builder.AppendLine(progression.BuildGradeSummary());
            builder.AppendLine($"PieceDatabase: {(pieces != null ? pieces.Definitions.Count : 0)} / 55");
            builder.AppendLine($"FusionRecipe: {(recipes != null ? recipes.Recipes.Count : 0)} / 41");
            builder.AppendLine($"1→2: {twoStar} / 21");
            builder.AppendLine($"2→3: {threeStar} / 20");
            builder.AppendLine($"공개: {(recipes != null ? recipes.Recipes.Count - hidden : 0)} / 37");
            builder.AppendLine($"숨김: {hidden} / 4");
            builder.AppendLine($"데이터 문제: {progression.ContentIssueCount}");
            builder.AppendLine($"다음 단절: {(progression.FirstUnreachableGrade.HasValue ? ((int)progression.FirstUnreachableGrade.Value + "성") : "없음")}");

            bool valid = pieces != null &&
                         recipes != null &&
                         pieces.Definitions.Count == 55 &&
                         recipes.Recipes.Count == 41 &&
                         twoStar == 21 &&
                         threeStar == 20 &&
                         hidden == 4 &&
                         progression.ContentIssueCount == 0 &&
                         progression.FirstUnreachableGrade == PieceGrade.FourStar;

            if (valid) Debug.Log(builder.ToString());
            else Debug.LogError(builder.ToString());
        }
    }
}
#endif
