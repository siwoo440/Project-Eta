using System; // StringComparer 사용
using System.Collections.Generic; // List·HashSet·IReadOnlyList 사용
using ProjectEta.Pieces; // PieceDatabase·PieceDefinition 사용
using ProjectEta.Run; // 통합 콘텐츠 참조 검증 사용

namespace ProjectEta.Fusion
{
    public sealed class FusionTreeValidationReport
    {
        public const int ExpectedPlayerPieceCount = 81;
        public const int ExpectedRecipeCount = 70;
        public const int ExpectedPublicRecipeCount = 63;
        public const int ExpectedHiddenRecipeCount = 7;

        public int PlayerPieceCount { get; internal set; }
        public int RecipeCount { get; internal set; }
        public int PublicRecipeCount { get; internal set; }
        public int HiddenRecipeCount { get; internal set; }
        public int ContentIssueCount { get; internal set; }

        public List<string> MissingCraftRecipePieceIds { get; } = new List<string>();
        public List<string> MissingUpgradeMaterialPieceIds { get; } = new List<string>();
        public List<string> UnreachablePieceIds { get; } = new List<string>();
        public List<string> InvalidReferenceRecipeIds { get; } = new List<string>();

        public bool IsComplete =>
            PlayerPieceCount == ExpectedPlayerPieceCount &&
            RecipeCount == ExpectedRecipeCount &&
            PublicRecipeCount == ExpectedPublicRecipeCount &&
            HiddenRecipeCount == ExpectedHiddenRecipeCount &&
            ContentIssueCount == 0 &&
            MissingCraftRecipePieceIds.Count == 0 &&
            MissingUpgradeMaterialPieceIds.Count == 0 &&
            UnreachablePieceIds.Count == 0 &&
            InvalidReferenceRecipeIds.Count == 0;

        public string BuildSummary()
        {
            return $"기물 {PlayerPieceCount}/{ExpectedPlayerPieceCount} · Recipe {RecipeCount}/{ExpectedRecipeCount} · 공개 {PublicRecipeCount}/{ExpectedPublicRecipeCount} · 숨김 {HiddenRecipeCount}/{ExpectedHiddenRecipeCount} · 제작법 누락 {MissingCraftRecipePieceIds.Count} · 상위재료 누락 {MissingUpgradeMaterialPieceIds.Count} · 도달불가 {UnreachablePieceIds.Count} · 참조오류 {InvalidReferenceRecipeIds.Count} · 콘텐츠오류 {ContentIssueCount}";
        }
    }

    public static class FusionTreeValidator
    {
        public static FusionTreeValidationReport Validate(
            PieceDatabase pieceDatabase,
            FusionRecipeDatabase recipeDatabase)
        {
            IReadOnlyList<PieceDefinition> pieces = pieceDatabase != null ? pieceDatabase.Definitions : null;
            IReadOnlyList<FusionRecipe> recipes = recipeDatabase != null ? recipeDatabase.Recipes : null;
            return Validate(pieces, recipes);
        }

        public static FusionTreeValidationReport Validate(
            IReadOnlyList<PieceDefinition> pieces,
            IReadOnlyList<FusionRecipe> recipes)
        {
            var report = new FusionTreeValidationReport();
            var pieceIds = new HashSet<string>(StringComparer.Ordinal);
            var playerPieces = new List<PieceDefinition>();

            if (pieces != null)
            {
                for (int i = 0; i < pieces.Count; i++)
                {
                    PieceDefinition piece = pieces[i];
                    if (!IsPlayerPiece(piece)) continue;

                    playerPieces.Add(piece);
                    report.PlayerPieceCount++;

                    if (!string.IsNullOrWhiteSpace(piece.PieceId))
                    {
                        pieceIds.Add(piece.PieceId);
                    }
                }
            }

            var resultPieceIds = new HashSet<string>(StringComparer.Ordinal);
            var materialPieceIds = new HashSet<string>(StringComparer.Ordinal);
            var validReachabilityRecipes = new List<FusionRecipe>();
            var invalidReferenceIds = new HashSet<string>(StringComparer.Ordinal);

            if (recipes != null)
            {
                report.RecipeCount = recipes.Count;

                for (int i = 0; i < recipes.Count; i++)
                {
                    FusionRecipe recipe = recipes[i];
                    if (recipe == null) continue;

                    if (recipe.IsHiddenRecipe) report.HiddenRecipeCount++;
                    else report.PublicRecipeCount++;

                    bool referencesValid =
                        IsRegistered(recipe.MaterialA, pieceIds) &&
                        IsRegistered(recipe.MaterialB, pieceIds) &&
                        IsRegistered(recipe.Result, pieceIds);

                    if (!referencesValid)
                    {
                        if (invalidReferenceIds.Add(recipe.RecipeId))
                        {
                            report.InvalidReferenceRecipeIds.Add(recipe.RecipeId);
                        }

                        continue;
                    }

                    resultPieceIds.Add(recipe.Result.PieceId);
                    materialPieceIds.Add(recipe.MaterialA.PieceId);
                    materialPieceIds.Add(recipe.MaterialB.PieceId);

                    if (FusionRuleValidator.ValidateRecipe(recipe) == FusionBlockReason.None)
                    {
                        validReachabilityRecipes.Add(recipe);
                    }
                }

                report.ContentIssueCount = FusionRecipeContentValidator.Validate(recipes).Count;
            }

            for (int i = 0; i < playerPieces.Count; i++)
            {
                PieceDefinition piece = playerPieces[i];
                if (piece == null || string.IsNullOrWhiteSpace(piece.PieceId)) continue;

                if ((int)piece.Grade >= (int)PieceGrade.TwoStar &&
                    !resultPieceIds.Contains(piece.PieceId))
                {
                    report.MissingCraftRecipePieceIds.Add(piece.PieceId);
                }

                if ((int)piece.Grade <= (int)PieceGrade.FourStar &&
                    FusionRuleValidator.IsFusableMaterial(piece) &&
                    !materialPieceIds.Contains(piece.PieceId))
                {
                    report.MissingUpgradeMaterialPieceIds.Add(piece.PieceId);
                }
            }

            HashSet<string> reachablePieceIds = BuildReachablePieceIds(playerPieces, validReachabilityRecipes);

            for (int i = 0; i < playerPieces.Count; i++)
            {
                PieceDefinition piece = playerPieces[i];
                if (piece == null || string.IsNullOrWhiteSpace(piece.PieceId)) continue;
                if ((int)piece.Grade < (int)PieceGrade.TwoStar) continue;

                if (!reachablePieceIds.Contains(piece.PieceId))
                {
                    report.UnreachablePieceIds.Add(piece.PieceId);
                }
            }

            report.MissingCraftRecipePieceIds.Sort(StringComparer.Ordinal);
            report.MissingUpgradeMaterialPieceIds.Sort(StringComparer.Ordinal);
            report.UnreachablePieceIds.Sort(StringComparer.Ordinal);
            report.InvalidReferenceRecipeIds.Sort(StringComparer.Ordinal);
            return report;
        }

        private static HashSet<string> BuildReachablePieceIds(
            IReadOnlyList<PieceDefinition> pieces,
            IReadOnlyList<FusionRecipe> recipes)
        {
            var reachable = new HashSet<string>(StringComparer.Ordinal);

            if (pieces != null)
            {
                for (int i = 0; i < pieces.Count; i++)
                {
                    PieceDefinition piece = pieces[i];
                    if (!IsPlayerPiece(piece) ||
                        piece.Grade != PieceGrade.OneStar ||
                        !FusionRuleValidator.IsFusableMaterial(piece) ||
                        string.IsNullOrWhiteSpace(piece.PieceId))
                    {
                        continue;
                    }

                    reachable.Add(piece.PieceId);
                }
            }

            bool changed;

            do
            {
                changed = false;

                if (recipes == null) break;

                for (int i = 0; i < recipes.Count; i++)
                {
                    FusionRecipe recipe = recipes[i];
                    if (recipe == null || recipe.Result == null ||
                        recipe.MaterialA == null || recipe.MaterialB == null) continue;

                    if (!reachable.Contains(recipe.MaterialA.PieceId) ||
                        !reachable.Contains(recipe.MaterialB.PieceId))
                    {
                        continue;
                    }

                    if (reachable.Add(recipe.Result.PieceId)) changed = true;
                }
            }
            while (changed);

            return reachable;
        }

        private static bool IsRegistered(PieceDefinition definition, HashSet<string> pieceIds)
        {
            return definition != null &&
                   !string.IsNullOrWhiteSpace(definition.PieceId) &&
                   pieceIds.Contains(definition.PieceId);
        }

        private static bool IsPlayerPiece(PieceDefinition definition)
        {
            if (definition == null) return false;
            return definition.Category != PieceCategory.Monster &&
                   definition.Category != PieceCategory.Boss;
        }
    }
}
