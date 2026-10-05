#if UNITY_EDITOR
using System.Text; // 검증 로그 작성
using UnityEditor; // MenuItem·AssetDatabase 사용
using UnityEngine; // Debug 사용
using ProjectEta.Fusion; // Atlas·트리 검증 사용
using ProjectEta.Meta; // 영구 메타 진행 사용
using ProjectEta.Pieces; // PieceDatabase 사용

namespace ProjectEta.EditorTools
{
    public static class Day89FusionAtlasValidationMenu
    {
        [MenuItem("Project Eta/Day 89/Validate Fusion Atlas And Full Tree")]
        public static void Validate()
        {
            PieceDatabase pieces =
                AssetDatabase.LoadAssetAtPath<PieceDatabase>("Assets/ProjectEta/Data/PieceDatabase.asset");
            FusionRecipeDatabase recipes =
                AssetDatabase.LoadAssetAtPath<FusionRecipeDatabase>("Assets/ProjectEta/Data/FusionRecipeDatabase.asset");

            FusionTreeValidationReport report = FusionTreeValidator.Validate(pieces, recipes);
            FusionRecipeIndex index = FusionAtlasService.RebuildIndex(recipes);
            MetaProgressState meta = MetaProgressService.Current;

            int permanentHidden = 0;

            if (recipes != null)
            {
                for (int i = 0; i < recipes.Recipes.Count; i++)
                {
                    FusionRecipe recipe = recipes.Recipes[i];

                    if (recipe != null &&
                        recipe.IsHiddenRecipe &&
                        meta.IsFusionRecipeDiscovered(recipe.RecipeId))
                    {
                        permanentHidden++;
                    }
                }
            }

            int tacticianFiveStarPaths =
                index.GetReachableFiveStarPaths("tactician").Count;

            var builder = new StringBuilder();
            builder.AppendLine("[Day89] Fusion Atlas·전체 성장 트리 검증");
            builder.AppendLine(report.BuildSummary());
            builder.AppendLine($"영구 발견 숨김 Recipe: {permanentHidden}/7");
            builder.AppendLine($"전술가 기준 5성 도달 경로: {tacticianFiveStarPaths}");
            builder.AppendLine($"Meta save version: {MetaProgressSaveData.CurrentVersion}");

            AppendIssues(builder, "제작법 누락", report.MissingCraftRecipePieceIds);
            AppendIssues(builder, "상위재료 누락", report.MissingUpgradeMaterialPieceIds);
            AppendIssues(builder, "도달불가", report.UnreachablePieceIds);
            AppendIssues(builder, "참조오류", report.InvalidReferenceRecipeIds);

            if (report.IsComplete &&
                MetaProgressSaveData.CurrentVersion >= 3 &&
                tacticianFiveStarPaths > 0)
            {
                Debug.Log(builder.ToString());
            }
            else
            {
                Debug.LogError(builder.ToString());
            }
        }

        private static void AppendIssues(
            StringBuilder builder,
            string label,
            System.Collections.Generic.IReadOnlyList<string> values)
        {
            if (values == null || values.Count == 0) return;

            builder.Append(label);
            builder.Append(": ");

            for (int i = 0; i < values.Count; i++)
            {
                if (i > 0) builder.Append(", ");
                builder.Append(values[i]);
            }

            builder.AppendLine();
        }
    }
}
#endif
